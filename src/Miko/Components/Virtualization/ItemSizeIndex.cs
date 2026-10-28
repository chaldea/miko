namespace Miko.Components.Virtualization;

/// <summary>
/// The measured size of every item of a virtualized list, with prefix sums for placing items that are
/// not rendered (ISSUE-151).
///
/// <para>An item that has been rendered has its measured size; every other item counts as
/// <see cref="Estimate"/> — the mean of the measured sizes, or the configured item size before anything
/// has been measured. Two Fenwick trees (the sum of the measured sizes, and how many items are
/// measured) give the offset of any item in O(log n) however many items there are, and changing the
/// estimate costs nothing: a prefix is <c>measuredSum + (unmeasuredCount × estimate)</c>.</para>
///
/// <para>Sums are kept in <c>double</c>: a float accumulating a hundred thousand row heights drifts by
/// whole pixels.</para>
/// </summary>
internal sealed class ItemSizeIndex
{
    private float[] _sizes = Array.Empty<float>();   // NaN = not measured
    private double[] _sumTree = new double[1];        // 1-based Fenwick trees over _sizes
    private int[] _countTree = new int[1];
    private int _count;
    private double _measuredSum;
    private int _measuredCount;

    public ItemSizeIndex(float defaultSize) => DefaultSize = defaultSize;

    /// <summary>The size assumed for items while nothing has been measured.</summary>
    public float DefaultSize { get; set; }

    /// <summary>The number of items.</summary>
    public int Count => _count;

    /// <summary>The size counted for an item that has not been measured.</summary>
    public float Estimate => _measuredCount > 0 ? (float)(_measuredSum / _measuredCount) : DefaultSize;

    /// <summary>The measured size of item <paramref name="index"/>, or <c>null</c>.</summary>
    public float? MeasuredSize(int index)
        => (uint)index < (uint)_count && !float.IsNaN(_sizes[index]) ? _sizes[index] : null;

    /// <summary>The size counted for item <paramref name="index"/>: measured, or the estimate.</summary>
    public float SizeOf(int index) => MeasuredSize(index) ?? Estimate;

    /// <summary>
    /// Changes the number of items. Items past the old count start unmeasured; items past the new count
    /// are forgotten.
    /// </summary>
    public void Resize(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == _count) return;

        if (count < _count)
        {
            for (int i = count; i < _count; i++) Clear(i);
            _count = count;
            return;
        }

        if (count > _sizes.Length) Grow(count);
        _count = count;
    }

    /// <summary>Records the measured size of item <paramref name="index"/>.</summary>
    public void Set(int index, float size)
    {
        if ((uint)index >= (uint)_count) return;
        size = Math.Max(0f, size);
        var old = _sizes[index];
        if (old == size) return;

        if (float.IsNaN(old))
        {
            Update(index, size, 1);
            _measuredCount++;
        }
        else
        {
            Update(index, size - old, 0);
        }
        _measuredSum += size - (float.IsNaN(old) ? 0f : old);
        _sizes[index] = size;
    }

    /// <summary>Forgets the measured size of item <paramref name="index"/>.</summary>
    public void Clear(int index)
    {
        if ((uint)index >= (uint)_count) return;
        var old = _sizes[index];
        if (float.IsNaN(old)) return;
        Update(index, -old, -1);
        _measuredCount--;
        _measuredSum -= old;
        _sizes[index] = float.NaN;
    }

    /// <summary>
    /// Replaces every size at once — <paramref name="sizes"/>[i] is item i's measured size or NaN — in
    /// O(n). Used when the items were re-keyed and their sizes moved to other indices.
    /// </summary>
    public void Reset(ReadOnlySpan<float> sizes)
    {
        if (sizes.Length > _sizes.Length) Grow(sizes.Length);
        _count = sizes.Length;
        sizes.CopyTo(_sizes);
        _sizes.AsSpan(_count).Fill(float.NaN);
        Rebuild();
    }

    /// <summary>The offset of item <paramref name="index"/>: the total size of items 0 … index−1.</summary>
    public double Prefix(int index)
    {
        index = Math.Clamp(index, 0, _count);
        double sum = 0;
        int measured = 0;
        for (int i = index; i > 0; i -= i & -i)
        {
            sum += _sumTree[i];
            measured += _countTree[i];
        }
        return sum + (index - measured) * (double)Estimate;
    }

    /// <summary>The total size of all items.</summary>
    public double Total => _measuredSum + (_count - _measuredCount) * (double)Estimate;

    /// <summary>
    /// The largest <c>k</c> in [0, Count] with <c>Prefix(k) ≤ offset</c> — the number of items that end
    /// at or before <paramref name="offset"/> when sizes are positive. O(log n).
    /// </summary>
    public int CountWithin(double offset)
    {
        if (offset < 0 || _count == 0) return 0;

        double estimate = Estimate;
        int pos = 0;
        double acc = 0;
        for (int step = HighestPowerOfTwo(_count); step > 0; step >>= 1)
        {
            int next = pos + step;
            if (next > _count) continue;
            double add = _sumTree[next] + (step - _countTree[next]) * estimate;
            if (acc + add <= offset)
            {
                pos = next;
                acc += add;
            }
        }
        return pos;
    }

    private void Update(int index, double sizeDelta, int countDelta)
    {
        for (int i = index + 1; i < _sumTree.Length && i <= _sizes.Length; i += i & -i)
        {
            _sumTree[i] += sizeDelta;
            _countTree[i] += countDelta;
        }
    }

    private void Grow(int minCapacity)
    {
        int capacity = Math.Max(minCapacity, Math.Max(16, _sizes.Length * 2));
        var sizes = new float[capacity];
        _sizes.AsSpan(0, _count).CopyTo(sizes);
        sizes.AsSpan(_count).Fill(float.NaN);
        _sizes = sizes;
        Rebuild();
    }

    /// <summary>Rebuilds both trees and the totals from <see cref="_sizes"/> in O(capacity).</summary>
    private void Rebuild()
    {
        int n = _sizes.Length;
        if (_sumTree.Length != n + 1)
        {
            _sumTree = new double[n + 1];
            _countTree = new int[n + 1];
        }
        else
        {
            Array.Clear(_sumTree);
            Array.Clear(_countTree);
        }

        _measuredSum = 0;
        _measuredCount = 0;
        for (int i = 0; i < n; i++)
        {
            var size = _sizes[i];
            if (float.IsNaN(size)) continue;
            if (i >= _count)
            {
                _sizes[i] = float.NaN;
                continue;
            }
            _sumTree[i + 1] += size;
            _countTree[i + 1] += 1;
            _measuredSum += size;
            _measuredCount++;
        }

        // Linear-time construction: push each node's total to its parent.
        for (int i = 1; i <= n; i++)
        {
            int parent = i + (i & -i);
            if (parent <= n)
            {
                _sumTree[parent] += _sumTree[i];
                _countTree[parent] += _countTree[i];
            }
        }
    }

    private static int HighestPowerOfTwo(int n)
    {
        int p = 1;
        while (p <= n >> 1) p <<= 1;
        return p;
    }
}
