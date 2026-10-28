using Miko.Components.Virtualization;
using Shouldly;

namespace Miko.Tests.Components.Virtualization;

/// <summary>
/// ISSUE-151: the size index behind <see cref="Virtualize{TItem}"/> places unrendered items from prefix
/// sums. It is checked against a plain array doing the same arithmetic the slow way, over random
/// operations — resizes, measurements, forgotten measurements and resets.
/// </summary>
public class ItemSizeIndexTests
{
    [Fact]
    public void Should_UseTheDefaultSize_UntilSomethingIsMeasured()
    {
        var index = new ItemSizeIndex(40f);
        index.Resize(10);

        index.Estimate.ShouldBe(40f);
        index.Prefix(3).ShouldBe(120d);
        index.Total.ShouldBe(400d);
        index.CountWithin(119).ShouldBe(2);
        index.CountWithin(120).ShouldBe(3);
    }

    [Fact]
    public void Should_CountUnmeasuredItemsAsTheMeanOfTheMeasuredOnes()
    {
        var index = new ItemSizeIndex(40f);
        index.Resize(10);
        index.Set(0, 100f);
        index.Set(1, 60f);

        index.Estimate.ShouldBe(80f);
        index.Prefix(2).ShouldBe(160d);
        index.Prefix(4).ShouldBe(320d);
        index.Total.ShouldBe(160d + 8 * 80d);
    }

    [Fact]
    public void Should_MatchANaiveModel_OverRandomOperations()
    {
        var random = new Random(151);
        var index = new ItemSizeIndex(50f);
        var model = new List<float>(); // NaN = unmeasured

        for (int step = 0; step < 4000; step++)
        {
            switch (random.Next(10))
            {
                case 0:
                {
                    int count = random.Next(0, 300);
                    index.Resize(count);
                    while (model.Count < count) model.Add(float.NaN);
                    if (model.Count > count) model.RemoveRange(count, model.Count - count);
                    break;
                }
                case 1 when model.Count > 0:
                {
                    int i = random.Next(model.Count);
                    index.Clear(i);
                    model[i] = float.NaN;
                    break;
                }
                case 2:
                {
                    var sizes = Enumerable.Range(0, random.Next(0, 300))
                        .Select(_ => random.Next(3) == 0 ? float.NaN : random.Next(0, 200))
                        .ToArray();
                    index.Reset(sizes);
                    model = sizes.ToList();
                    break;
                }
                default:
                    if (model.Count == 0) break;
                    int at = random.Next(model.Count);
                    float size = random.Next(0, 200);
                    index.Set(at, size);
                    model[at] = size;
                    break;
            }

            AssertMatches(index, model, random);
        }
    }

    private static void AssertMatches(ItemSizeIndex index, List<float> model, Random random)
    {
        index.Count.ShouldBe(model.Count);
        var measured = model.Where(s => !float.IsNaN(s)).ToList();
        double estimate = measured.Count > 0 ? measured.Average(s => (double)s) : 50d;
        index.Estimate.ShouldBe((float)estimate, 0.01f);

        double Prefix(int k)
        {
            double sum = 0;
            for (int i = 0; i < k; i++) sum += float.IsNaN(model[i]) ? (float)estimate : model[i];
            return sum;
        }

        index.Total.ShouldBe(Prefix(model.Count), 0.05);
        for (int probe = 0; probe < 5; probe++)
        {
            int k = random.Next(model.Count + 1);
            index.Prefix(k).ShouldBe(Prefix(k), 0.05);

            // CountWithin is the largest k with Prefix(k) <= offset.
            double offset = random.NextDouble() * (Prefix(model.Count) + 100) - 20;
            int expected = 0;
            for (int j = 1; j <= model.Count; j++)
            {
                if (Prefix(j) <= offset + 1e-6) expected = j;
            }
            int actual = index.CountWithin(offset);
            // Equal prefixes (zero-size items) make several answers valid; the prefix must agree.
            Prefix(actual).ShouldBe(Prefix(expected), 0.05);
            if (actual < model.Count) (Prefix(actual + 1) > offset - 1e-6).ShouldBeTrue();
        }
    }
}
