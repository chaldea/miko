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
            // After the routed build, the page's current root (the latest generation, so nothing
            // older is pinned). Reading the property never re-renders the page.
            if (_pageBuilt && _bodyElement is null) return _page?.CurrentRoot;
            if (ResolveBody() is null) return null;
            _bodyElement = _bodyElement!.ResolveSuperseded();
            return _bodyElement;
        }
        internal set => _bodyElement = value;
    }

    private Element? _bodyElement;
    private ComponentBase? _page;
    private bool _pageBuilt;

    /// <summary>
    /// 交给布局一个「按需构建页面」的入口（由 <see cref="Routing.RouteView"/> 在构建布局前调用）。
    ///
    /// <para>页面在布局<b>第一次要内容</b>时才构建——Razor 布局走到 <c>@Body</c>，手写布局读
    /// <see cref="BodyElement"/>——两条路径共享同一次构建。这一刻布局的渲染仍在调用栈上，
    /// 页面因此能拿到布局级联下来的值。先建页面再建布局时，页面永远拿不到布局里
    /// <c>IonApp</c> 级联的弹层上下文，声明的弹层全部内联渲染（ISSUE-153 问题 1）。</para>
    /// </summary>
    internal void SetBody(ComponentBase page)
    {
        _bodyElement = null;
        _page = page;
        _pageBuilt = false;
        Body = builder => builder.AttachElement(ResolveBody()!);
    }

    private Element? ResolveBody()
    {
        if (_bodyElement is null && _page is not null)
        {
            if (!_pageBuilt)
            {
                _bodyElement = _page.Build();
                _pageBuilt = true;
            }
            else
            {
                // The layout re-renders itself (StateHasChanged after its own handler, e.g. a
                // language switch, ISSUE-157). Its page re-renders with it, as a nested component
                // does with its parent, and the new layout tree takes the page's current root —
                // which moves out of the old tree, so discarding that tree leaves the page alone.
                // Before, Body was gone by now and the page vanished from the layout.
                _page.RenderNow();
                return _page.CurrentRoot;
            }
        }
        return _bodyElement;
    }

    /// <summary>
    /// 主动放开对页面内容的引用（由 <see cref="Routing.RouteView"/> 在装配完布局后调用）。
    ///
    /// <para>放开的是<b>元素</b>：<see cref="BodyElement"/> 直接持有第 0 代内容根，留着它就等于
    /// 攥住第 0 代，它的 <c>SupersededBy</c> <b>前向</b>链会让此后每一代——连同每个元素约
    /// 6&#160;KB 的 <c>ComputedStyle</c>——全部可达（ISSUE-142 复审：反复点 <c>IonSegmentButton</c>
    /// 每次泄漏约 730&#160;KB，600 次后 21&#160;MB → 457&#160;MB，强制压缩回收也放不掉）。</para>
    ///
    /// <para>页面<b>组件</b>则保留：它只指向自己当前这一代根（<see cref="ComponentBase.CurrentRoot"/>），
    /// 不攥任何旧代；而且它本就经由内容树上的拆解回调可达，多一个引用不延长任何生命周期。
    /// 留着它，布局之后自己重渲染时 <see cref="Body"/> 仍能交出页面（ISSUE-157）。</para>
    /// </summary>
    internal void ReleaseBody()
    {
        _bodyElement = null;
    }
}
