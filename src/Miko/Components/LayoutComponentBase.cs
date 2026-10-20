using Miko.Core;
using Miko.Routing;

namespace Miko.Components;

public abstract class LayoutComponentBase : ComponentBase
{
    public new NavigationManager? NavigationManager
    {
        get => base.NavigationManager;
        internal set => base.NavigationManager = value;
    }

    public RenderFragment? Body { get; internal set; }

    /// <summary>
    /// 当前在场的页面内容根元素，供手写布局（非 Razor）把它放进自己的结构里
    /// （见 <c>examples/Bootstrap/MikoApp1/MainLayout.cs</c>）。
    ///
    /// <para><b>读取时沿 <c>SupersededBy</c> 链前推，绝不返回赋值时的那一个实例。</b>
    /// 本属性由 <see cref="Routing.RouteView"/> 在导航时赋值<b>一次</b>，此后页面每次
    /// <c>StateHasChanged</c> 都产出全新的根元素实例。字段若一直攥着第 0 代，它的
    /// <c>SupersededBy</c> <b>前向</b>链就让此后每一代——连同每个元素约 6&#160;KB 的
    /// <c>ComputedStyle</c>——全部可达：实测 Anime 示例来回点 <c>IonSegmentButton</c>
    /// 每次点击泄漏约 730&#160;KB，600 次后托管堆 21&#160;MB → 457&#160;MB，
    /// 强制压缩回收也放不掉（ISSUE-142 复审）。</para>
    ///
    /// <para>前推的是<b>字段本身</b>而不只是返回值：<c>ResolveSuperseded</c> 的路径压缩只改写
    /// 链头那一格，字段不松手则被跳过的各代仍从它可达——与
    /// <c>MikoInteractionController.ResolveAndReanchorPointerDownTarget</c> 同一套处理。</para>
    /// </summary>
    public Element? BodyElement
    {
        get
        {
            if (ResolveBody() is null) return null;
            _bodyElement = _bodyElement!.ResolveSuperseded();
            return _bodyElement;
        }
        internal set => _bodyElement = value;
    }

    private Element? _bodyElement;
    private Func<Element>? _buildBody;

    /// <summary>
    /// 交给布局一个「按需构建页面」的入口（由 <see cref="Routing.RouteView"/> 在构建布局前调用）。
    ///
    /// <para>页面在布局<b>第一次要内容</b>时才构建——Razor 布局走到 <c>@Body</c>，手写布局读
    /// <see cref="BodyElement"/>——两条路径共享同一次构建。这一刻布局的渲染仍在调用栈上，
    /// 页面因此能拿到布局级联下来的值。先建页面再建布局时，页面永远拿不到布局里
    /// <c>IonApp</c> 级联的弹层上下文，声明的弹层全部内联渲染（ISSUE-153 问题 1）。</para>
    /// </summary>
    internal void SetBody(Func<Element> buildBody)
    {
        _bodyElement = null;
        _buildBody = buildBody;
        Body = builder => builder.AttachElement(ResolveBody()!);
    }

    private Element? ResolveBody()
    {
        if (_bodyElement is null && _buildBody is not null)
        {
            _bodyElement = _buildBody();
            _buildBody = null;
        }
        return _bodyElement;
    }

    /// <summary>
    /// 主动放开对页面内容的引用（由 <see cref="Routing.RouteView"/> 在装配完布局后调用）。
    ///
    /// <para>三个字段都要放：<see cref="BodyElement"/> 直接持有第 0 代内容根，按需构建的入口
    /// 捕获着页面组件，<see cref="Body"/> 闭包又经布局自身读回它们，留着任何一个都等于攥住第 0 代。
    /// 它的 <c>SupersededBy</c> <b>前向</b>链会让此后每一代——连同每个元素约 6&#160;KB 的
    /// <c>ComputedStyle</c>——全部可达（ISSUE-142 复审：反复点 <c>IonSegmentButton</c>
    /// 每次泄漏约 730&#160;KB，600 次后 21&#160;MB → 457&#160;MB，强制压缩回收也放不掉）。</para>
    ///
    /// <para>放开是安全的：两者都只在布局的 <c>Build()</c> 期间被读到一次
    /// ——Razor 布局写 <c>@Body</c>，手写布局读 <see cref="BodyElement"/>——
    /// 此后页面内容由已经建成的元素树自己持有，重渲染走的是 <c>StateHasChanged</c>
    /// 就地替换，不会再回头问布局要内容。</para>
    /// </summary>
    internal void ReleaseBody()
    {
        _bodyElement = null;
        _buildBody = null;
        Body = null;
    }
}
