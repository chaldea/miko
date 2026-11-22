namespace Miko.Components.Editor;

/// <summary>Zero-based line indices. A missing original (-1) denotes an addition, a missing modified line a deletion.</summary>
internal readonly record struct DiffRow(int Original, int Modified);

internal static class EditorDiff
{
    // Bound Myers' search history. For distant versions, match remaining equal lines in order.
    private const int MaxDistance = 256;

    public static List<DiffRow> Compare(EditorDocument original, EditorDocument modified)
    {
        int oldCount = original.Text.Length == 0 ? 0 : original.LineCount;
        int newCount = modified.Text.Length == 0 ? 0 : modified.LineCount;
        bool Equal(int a, int b) => original.GetLine(a).Span.SequenceEqual(modified.GetLine(b).Span);
        var rows = new List<DiffRow>(Math.Max(oldCount, newCount));
        int start = 0;
        while (start < oldCount && start < newCount && Equal(start, start))
        {
            rows.Add(new(start, start));
            start++;
        }
        int oldEnd = oldCount, newEnd = newCount;
        while (oldEnd > start && newEnd > start && Equal(oldEnd - 1, newEnd - 1))
        {
            oldEnd--;
            newEnd--;
        }

        if (oldEnd == start || newEnd == start)
        {
            for (int i = start; i < oldEnd; i++) rows.Add(new(i, -1));
            for (int i = start; i < newEnd; i++) rows.Add(new(-1, i));
        }
        else if (!TryMyers(rows, start, oldEnd - start, newEnd - start, Equal))
        {
            AppendOrderedMatches(rows, original, modified, start, oldEnd, newEnd);
        }
        for (int i = oldEnd; i < oldCount; i++) rows.Add(new(i, newEnd + (i - oldEnd)));
        return rows;
    }

    private static bool TryMyers(List<DiffRow> rows, int start, int oldLength, int newLength, Func<int, int, bool> equal)
    {
        if (Math.Abs(oldLength - newLength) > MaxDistance) return false;
        int limit = (int)Math.Min(MaxDistance, (long)oldLength + newLength);
        int center = limit + 1;
        int[] frontier = new int[2 * limit + 3];
        var history = new List<int[]>();
        for (int distance = 0; distance <= limit; distance++)
        {
            // On diagonal k = x - y, retain the furthest reachable original line x.
            for (int k = -distance; k <= distance; k += 2)
            {
                int index = center + k;
                int x = IsInsertion(frontier, index, k, distance) ? frontier[index + 1] : frontier[index - 1] + 1;
                int y = x - k;
                while (x < oldLength && y < newLength && equal(start + x, start + y))
                {
                    x++;
                    y++;
                }
                frontier[index] = x;
                if (x == oldLength && y == newLength)
                {
                    Backtrack(rows, start, x, y, center, history);
                    return true;
                }
            }
            history.Add((int[])frontier.Clone());
        }
        return false;
    }

    private static bool IsInsertion(int[] frontier, int index, int diagonal, int distance)
        => diagonal == -distance || (diagonal != distance && frontier[index - 1] < frontier[index + 1]);

    private static void Backtrack(List<DiffRow> rows, int start, int x, int y, int center, List<int[]> history)
    {
        int first = rows.Count;
        for (int distance = history.Count; distance > 0; distance--)
        {
            var previous = history[distance - 1];
            int diagonal = x - y;
            bool insert = IsInsertion(previous, center + diagonal, diagonal, distance);
            int prevDiagonal = diagonal + (insert ? 1 : -1);
            int prevX = previous[center + prevDiagonal];
            int prevY = prevX - prevDiagonal;
            while (x > prevX && y > prevY) rows.Add(new(start + --x, start + --y));
            if (insert) rows.Add(new(-1, start + --y));
            else rows.Add(new(start + --x, -1));
        }
        rows.Reverse(first, rows.Count - first);
    }

    private static void AppendOrderedMatches(List<DiffRow> rows, EditorDocument original, EditorDocument modified,
        int start, int oldEnd, int newEnd)
    {
        // Index slices into the existing snapshot. Link duplicate occurrences without copying line text.
        var heads = new Dictionary<ReadOnlyMemory<char>, int>(LineComparer.Instance);
        var next = new int[newEnd - start];
        for (int i = newEnd - 1; i >= start; i--)
        {
            var line = modified.GetLine(i);
            next[i - start] = heads.GetValueOrDefault(line, -1);
            heads[line] = i;
        }
        int current = start;
        for (int i = start; i < oldEnd; i++)
        {
            var line = original.GetLine(i);
            int match = heads.GetValueOrDefault(line, -1);
            while (match >= 0 && match < current) match = next[match - start];
            if (match < 0)
            {
                heads.Remove(line);
                rows.Add(new(i, -1));
                continue;
            }
            heads[line] = next[match - start];
            while (current < match) rows.Add(new(-1, current++));
            rows.Add(new(i, current++));
        }
        while (current < newEnd) rows.Add(new(-1, current++));
    }

    private sealed class LineComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly LineComparer Instance = new();
        public bool Equals(ReadOnlyMemory<char> a, ReadOnlyMemory<char> b) => a.Span.SequenceEqual(b.Span);
        public int GetHashCode(ReadOnlyMemory<char> line) => string.GetHashCode(line.Span, StringComparison.Ordinal);
    }
}
