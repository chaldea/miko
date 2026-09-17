namespace Miko.Tests;

/// <summary>
/// 把所有会开启 <see cref="Miko.Diagnostics.FrameTimingDiagnostics"/> 的测试收进同一个
/// xUnit collection，使它们相互串行（ISSUE-136）。
///
/// <para>探针的计数器是 <c>[ThreadStatic]</c> 的，但 <c>Begin</c>/<c>End</c> 之间被测量的
/// 是「本线程上发生的全部构建/样式/布局/绘制」。xUnit 并行运行时，同一线程会被复用来跑
/// 别的测试类，那些测试自己的 Build/Layout 就会计进当前测量窗口，令
/// <c>BuildCount</c>/<c>LayoutCount</c> 这类精确断言随机失败。</para>
///
/// <para>与 <see cref="GlobalFontStateCollection"/> 同样的处置：归入一个禁用并行的
/// collection。新增测试若调用 <c>FrameTimingDiagnostics.Begin()</c>，请一并加上
/// <c>[Collection(FrameTimingCollection.Name)]</c>。</para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class FrameTimingCollection
{
    public const string Name = "FrameTiming";
}
