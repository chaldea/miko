using Miko.Rendering;
using Shouldly;
using Xunit;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-143：窗口帧缓冲的包装表面（<see cref="GRBackendRenderTarget"/> + <see cref="SKSurface"/>）
/// 必须按 (尺寸, FBO) 复用，而不是每帧重建。
///
/// <para>只测复用判定这一个纯函数：构造真实的 <c>GRContext</c> 需要活的 GL 上下文，单元测试里
/// 没有。而复用判定正是本类唯一的决策——三个分量任一漏掉都是真实缺陷：漏尺寸会按旧尺寸绘制，
/// 漏 FBO 会把内容画进离屏帧缓冲导致窗口黑屏（ISSUE-067 那类现象）。</para>
/// </summary>
public class WindowRenderTargetTests
{
    [Fact]
    public void Should_reuse_the_surface_when_size_and_framebuffer_are_unchanged()
    {
        // 稳态帧：尺寸与 FBO 都没变，必须复用。这正是 ISSUE-143 省下的那笔开销——
        // 每帧重建在固定尺寸下实测多占 6.7 MB 私有内存、2.7 MB 托管分配（1500 帧）。
        WindowRenderTarget.NeedsRebuild(800, 600, 0, 800, 600, 0).ShouldBeFalse();
    }

    [Theory]
    [InlineData(801, 600, 0u)] // 宽度变化
    [InlineData(800, 601, 0u)] // 高度变化
    [InlineData(800, 600, 3u)] // FBO 绑定变化（模拟器先渲染离屏再合成）
    public void Should_rebuild_when_any_key_component_changes(int width, int height, uint framebuffer)
    {
        WindowRenderTarget.NeedsRebuild(800, 600, 0, width, height, framebuffer).ShouldBeTrue();
    }

    [Fact]
    public void Should_rebuild_after_returning_to_a_previously_seen_size()
    {
        // 只缓存"上一帧"那一张，不做多槽缓存：缩放时尺寸单调变化，多槽只会囤住一堆
        // 再也用不到的渲染目标。回到旧尺寸时重建是正确且廉价的。
        WindowRenderTarget.NeedsRebuild(1000, 700, 0, 800, 600, 0).ShouldBeTrue();
    }
}
