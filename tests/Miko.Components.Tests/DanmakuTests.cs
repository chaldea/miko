using Miko.Components.Player.Danmaku;
using Shouldly;

namespace Miko.Components.Tests;

public class DanmakuTests
{
    [Fact]
    public void Should_Advance_Steady_State_Without_Per_Frame_Allocations()
    {
        var timeline = new DanmakuTimeline();
        timeline.SetItems(Enumerable.Range(0, 100).Select(i => new DanmakuItem(TimeSpan.FromSeconds(i * .1), "text")));
        Func<string, float> measure = static _ => 100;
        for (int i = 0; i < 100; i++) timeline.Advance(TimeSpan.FromSeconds(1), 640, 360, measure);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) timeline.Advance(TimeSpan.FromSeconds(1 + i * .001), 640, 360, measure);
        (GC.GetAllocatedBytesForCurrentThread() - before).ShouldBe(0);
    }

    [Fact]
    public void Should_Freeze_At_Same_Media_Time_And_Rebuild_After_Seek()
    {
        var timeline = new DanmakuTimeline();
        timeline.SetItems([new(TimeSpan.Zero, "first"), new(TimeSpan.FromSeconds(20), "later")]);
        timeline.Advance(TimeSpan.FromSeconds(2), 640, 360, _ => 100);
        float x = timeline.Visible.Single().X;
        timeline.Advance(TimeSpan.FromSeconds(2), 640, 360, _ => 100);
        timeline.Visible.Single().X.ShouldBe(x);
        timeline.Advance(TimeSpan.FromSeconds(21), 640, 360, _ => 100);
        timeline.Visible.Single().Item.Text.ShouldBe("later");
        timeline.Advance(TimeSpan.Zero, 640, 360, _ => 100);
        timeline.Visible.Single().Item.Text.ShouldBe("first");
        timeline.Visible.Single().X.ShouldBe(640);
    }

    [Fact]
    public void Should_Bound_Density_And_Avoid_Overlapping_Lanes()
    {
        var timeline = new DanmakuTimeline { Settings = new() { MaxVisible = 3 } };
        timeline.SetItems(Enumerable.Range(0, 10000).Select(i => new DanmakuItem(TimeSpan.Zero, i.ToString())));
        int measurements = 0;
        timeline.Advance(TimeSpan.FromSeconds(1), 320, 180, _ => { measurements++; return 100; });
        timeline.Visible.Count.ShouldBeLessThanOrEqualTo(3);
        timeline.Visible.Select(x => x.Y).Distinct().Count().ShouldBe(timeline.Visible.Count);
        measurements.ShouldBeLessThanOrEqualTo(2048);
    }

    [Fact]
    public void Should_Keep_Fixed_Comments_Centered_And_Clear_Disabled_State()
    {
        var timeline = new DanmakuTimeline();
        timeline.SetItems([new(TimeSpan.Zero, "top", DanmakuMode.Top), new(TimeSpan.Zero, "bottom", DanmakuMode.Bottom)]);
        timeline.Advance(TimeSpan.FromSeconds(1), 640, 360, _ => 100);
        timeline.Visible.ShouldAllBe(x => x.X == 270);
        timeline.Visible[0].Y.ShouldBeLessThan(timeline.Visible[1].Y);
        timeline.Settings = timeline.Settings with { Enabled = false };
        timeline.Advance(TimeSpan.FromSeconds(1), 640, 360, _ => 100);
        timeline.Visible.ShouldBeEmpty();
    }
}
