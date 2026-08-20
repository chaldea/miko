using Miko.Animation;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Rendering;

/// <summary>
/// 渲染引擎
/// </summary>
public class RenderEngine
{
    /// <summary>
    /// 增量渲染脏区域数量阈值。脏区域超过该数量时，多次全树遍历的成本会超过一次全量渲染
    /// （见基准报告 §2 拐点 30–50），此时应回退到全量渲染。
    /// </summary>
    public int MaxIncrementalDirtyRegions { get; set; } = 30;

    private SKCanvas? _canvas;
    private Painter? _painter;

    /// <summary>
    /// 当前渲染所用的 GPU 上下文。GPU 宿主在拥有 <see cref="GRContext"/> 时设置，
    /// 供视频帧源把解码 GPU 资源零拷贝包装为 <see cref="SKImage"/>。
    /// 离屏/软件渲染下为 null，视频帧源应回退到 CPU 光栅图像。
    /// </summary>
    public GRContext? GraphicsContext { get; set; }

    /// <summary>
    /// 语法高亮器（<c>&lt;code language="..."&gt;</c> 的 token 着色）。默认内置
    /// <see cref="Highlight.SyntaxHighlighter"/>；DI 场景下由
    /// <see cref="Platform.MikoInteractionController"/> 在初始化时解析容器中的
    /// <see cref="Highlight.ISyntaxHighlighter"/> 并覆盖此默认值（应用可重新注册接口
    /// 以替换高亮实现，见 ISSUE-098）。
    /// </summary>
    public Highlight.ISyntaxHighlighter SyntaxHighlighter { get; set; } = new Highlight.SyntaxHighlighter();
    private List<RectF>? _dirtyRegions;
    private readonly List<(LayoutBox box, SelectElement select, float scrollOffsetX, float scrollOffsetY)> _pendingDropdowns = new();

    /// <summary>
    /// 本帧遇到的 <c>position: fixed</c> 盒子，延迟到整棵树绘制完后由 <see cref="FlushFixed"/>
    /// 在画布根状态下统一绘制（见那里的说明）。
    /// </summary>
    private readonly List<LayoutBox> _pendingFixed = new();
    private float _currentScrollOffsetX;
    private float _currentScrollOffsetY;

    public Action<SKCanvas>? OverlayCallback { get; set; }

    /// <summary>
    /// 设置画布
    /// </summary>
    public void SetCanvas(SKCanvas canvas)
    {
        _canvas = canvas;
        _painter = new Painter(canvas);
    }

    /// <summary>
    /// 全量渲染
    /// </summary>
    public void Render(LayoutBox layoutRoot)
    {
        if (_canvas == null || _painter == null)
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");

        _dirtyRegions = null;
        _pendingDropdowns.Clear();
        _pendingFixed.Clear();
        _currentScrollOffsetX = 0;
        _currentScrollOffsetY = 0;
        RenderBox(layoutRoot, null, isStackingRoot: true);
        FlushFixed();
        FlushDropdowns();
        OverlayCallback?.Invoke(_canvas!);
    }

    /// <summary>
    /// 把一棵布局树作为一个"图层"渲染：整体偏移（像素）+ 整体不透明度（页面转场使用，ISSUE-108）。
    /// 不触发 <see cref="OverlayCallback"/>——多层绘制时由调用方在最后统一调用 <see cref="RenderOverlay"/>。
    /// </summary>
    public void RenderLayer(LayoutBox layoutRoot, float offsetX = 0, float offsetY = 0, float opacity = 1)
    {
        if (_canvas == null || _painter == null)
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");

        _dirtyRegions = null;
        _pendingDropdowns.Clear();
        _pendingFixed.Clear();
        _currentScrollOffsetX = 0;
        _currentScrollOffsetY = 0;

        bool hasOpacity = opacity < 1f;

        _painter.Save();
        if (hasOpacity)
            _painter.SaveLayerAlpha((byte)(Math.Clamp(opacity, 0f, 1f) * 255));
        if (offsetX != 0 || offsetY != 0)
            _painter.Translate(offsetX, offsetY);

        RenderBox(layoutRoot, null, isStackingRoot: true);
        // 转场图层的整体偏移与不透明度对覆盖层同样生效，故在本层的 Save 之内 flush：
        // 页面切换时 fixed 覆盖层应随页面一起滑动/淡出，而不是钉在屏幕上不动。
        FlushFixed();
        FlushDropdowns();

        if (hasOpacity) _painter.Restore();
        _painter.Restore();
    }

    /// <summary>触发 <see cref="OverlayCallback"/>（页面转场多层绘制结束后统一绘制一次覆盖层）。</summary>
    public void RenderOverlay()
    {
        if (_canvas == null)
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");
        OverlayCallback?.Invoke(_canvas);
    }

    public void RenderDirty(LayoutBox layoutRoot, List<RectF> dirtyRegions)
    {
        if (_canvas == null || _painter == null)
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");

        _dirtyRegions = dirtyRegions;
        _pendingDropdowns.Clear();
        _currentScrollOffsetX = 0;
        _currentScrollOffsetY = 0;

        foreach (var region in dirtyRegions)
        {
            _painter.Save();
            _painter.ClipRect(region);
            // fixed 覆盖层同样要限制在本脏区内重绘，故逐区收集并 flush（在该区的裁剪之内）。
            _pendingFixed.Clear();
            RenderBox(layoutRoot, null, isStackingRoot: true);
            FlushFixed();
            _painter.Restore();
        }

        FlushDropdowns();
        _dirtyRegions = null;
    }

    private void FlushDropdowns()
    {
        foreach (var (box, select, scrollX, scrollY) in _pendingDropdowns)
            RenderSelectDropdown(box, select, scrollX, scrollY);
        _pendingDropdowns.Clear();
    }

    /// <summary>
    /// 绘制本帧收集到的 <c>position: fixed</c> 盒子——CSS 的「固定定位相对视口」在绘制侧的另一半。
    /// <para>
    /// fixed 盒的包含块是视口（<see cref="Layout.LayoutEngine"/> 已按视口解析其坐标），因此它
    /// 既不该被任何祖先的 <c>overflow</c> 裁掉，也不该跟着祖先滚动。而正常递归绘制时它身处
    /// 祖先的画布状态里：<see cref="RenderChildrenWithOverflow"/> 已经压了 clip 并
    /// <c>Translate(-ScrollLeft, -ScrollTop)</c>，两者都会作用到它身上。z-index 提取路径也救不了
    /// 它——<see cref="CollectZOrderedDescendants"/> 在会裁剪的祖先处刻意停止下探。
    /// </para>
    /// <para>
    /// 所以改为 <see cref="RenderBox"/> 遇到 fixed 就地收集、跳过，等整棵树画完、所有祖先的
    /// <c>Save</c> 都已 <c>Restore</c> 回画布根状态后，在这里按 z-index 升序统一绘制。这与
    /// <c>&lt;select&gt;</c> 下拉的 <see cref="FlushDropdowns"/> 是同一套「顶层 pass」手法。
    /// 症状见 issues/ion-select.md 问题 4：ion-item（<c>overflow:hidden</c> + 48px）里的
    /// ion-select 打开覆盖层后，全屏 fixed 覆盖层被裁成 48px 高的一条。
    /// </para>
    /// </summary>
    private void FlushFixed()
    {
        if (_pendingFixed.Count == 0) return;

        // 收集顺序是深度优先前序（即文档序），OrderBy 稳定，故同 z-index 保持文档序。
        var ordered = _pendingFixed.OrderBy(b => b.ComputedStyle.ZIndex).ToList();
        _pendingFixed.Clear();

        // 祖先的滚动偏移不适用于 fixed（它相对视口固定），从零开始。
        float prevScrollX = _currentScrollOffsetX;
        float prevScrollY = _currentScrollOffsetY;
        _currentScrollOffsetX = 0;
        _currentScrollOffsetY = 0;

        foreach (var box in ordered)
        {
            // 每个 fixed 盒自成一个层叠上下文根，其内部的定位后代在它自己那一层里排序。
            RenderBox(box, null, isStackingRoot: true, isFixedRoot: true);
        }

        _currentScrollOffsetX = prevScrollX;
        _currentScrollOffsetY = prevScrollY;

        // fixed 子树里还可能嵌着别的 fixed 盒（被上面的递归收集起来），继续排空。
        FlushFixed();
    }

    /// <summary>
    /// 渲染盒子。
    /// </summary>
    /// <param name="box">要绘制的盒子。</param>
    /// <param name="inheritedDeferred">
    /// 由祖先层叠上下文提出、稍后统一按 z-index 绘制的后代集合；沿普通（不建立层叠上下文的）
    /// 祖先向下传递，使这些后代在正常递归中被跳过。
    /// </param>
    /// <param name="isStackingRoot">
    /// 本盒是否作为层叠上下文的根来收集并排序内部的定位后代。调用方在两种情况下传 true：
    /// 绘制整棵树的根，或绘制一个被提出的定位后代（它自成一层）。
    /// </param>
    /// <param name="isFixedRoot">
    /// 本盒是否是 <see cref="FlushFixed"/> 正在绘制的那个 fixed 盒。仅对「本盒自身」生效，
    /// 使它不再被自己重新收集；其子树里更深的 fixed 后代仍照常收集到下一轮 flush。
    /// </param>
    private void RenderBox(
        LayoutBox box,
        HashSet<LayoutBox>? inheritedDeferred = null,
        bool isStackingRoot = false,
        bool isFixedRoot = false)
    {
        if (_painter == null) return;

        if (!ShouldRender(box)) return;

        // position: fixed —— 不在祖先的画布状态里就地绘制（那会挨上祖先的 overflow 裁剪与滚动
        // 平移），改为收集起来，等回到画布根状态后由 FlushFixed 统一绘制。见 FlushFixed。
        if (!isFixedRoot && box.ComputedStyle.Position == Common.Position.Fixed)
        {
            _pendingFixed.Add(box);
            return;
        }

        // 建立层叠上下文的盒子自行收集内部后代，祖先的延迟集合到此为止。
        if (!isStackingRoot && EstablishesStackingContext(box)) isStackingRoot = true;
        if (isStackingRoot) inheritedDeferred = null;

        float opacity = box.ComputedStyle.Opacity;
        bool hasOpacity = opacity < 1f;
        if (hasOpacity)
        {
            byte alpha = (byte)(opacity * 255);
            _painter.SaveLayerAlpha(alpha);
        }

        bool hasTransform = box.ComputedStyle.Transform.Functions.Count > 0;
        if (hasTransform)
        {
            _painter.Save();
            ApplyTransform(box);
        }

        // visibility: hidden / collapse —— 本盒自身不绘制（背景/边框/轮廓/内容），
        // 但仍占据布局空间且继续递归子元素（子元素可用 visibility: visible 覆盖）。
        // 这与 display: none（完全从布局树移除）不同。
        bool isVisible = box.ComputedStyle.Visibility == Visibility.Visible;

        if (isVisible)
        {
            // 1. 绘制盒阴影（在背景之前）
            RenderBoxShadow(box);

            // 2. 绘制背景
            RenderBackground(box);

            // 3. 绘制边框
            RenderBorder(box);

            // 3b. 绘制轮廓（在边框之外，不占布局空间）
            RenderOutline(box);

            // 4. 绘制内容。overflow != visible 且带圆角时，内容（如图片位图）需裁剪到圆角内
            //    ——否则方形位图会溢出圆角（如 ion-avatar 的 <img> border-radius:50% overflow:hidden）。
            //    背景/边框已由 ResolveBorderRadii 自行成形，故裁剪只包住内容绘制。
            if (ClipsContentToRoundedBox(box, out var tl, out var tr, out var brr, out var bl))
            {
                _painter.SaveClipRounded(box.BoxModel.PaddingBox, tl, tr, brr, bl);
                RenderContent(box);
                _painter.Restore();
            }
            else
            {
                RenderContent(box);
            }
        }

        // 4. 递归绘制子元素
        // SelectElement 的子元素（Option）不参与正常树渲染，由 overlay pass 统一绘制下拉层
        if (box.Element is SelectElement)
        {
            if (hasTransform) _painter.Restore();
            if (hasOpacity) _painter.Restore();
            return;
        }

        // 本盒是层叠上下文的根（或整棵树的根）时，把它内部所有「带 z-index 的定位后代」提出来，
        // 在本层末尾按 z-index 统一绘制——这才是 CSS 的绘制顺序：z-index 跨越普通祖先比较，
        // 只被建立层叠上下文的祖先关住（见 CollectZOrderedDescendants）。
        List<LayoutBox>? zOrdered = null;
        var deferred = inheritedDeferred;
        if (isStackingRoot)
        {
            zOrdered = CollectZOrderedDescendants(box);
            if (zOrdered != null) deferred = new HashSet<LayoutBox>(zOrdered);
        }

        // Negative z-index descendants paint behind normal-flow content, but above this box's
        // background. They must not be deferred until after normal children.
        if (zOrdered != null)
        {
            foreach (var descendant in zOrdered)
            {
                if (descendant.ComputedStyle.ZIndex >= 0) break;
                RenderBox(descendant, null, isStackingRoot: true);
            }
        }

        // 处理 overflow 裁剪和滚动
        bool hasOverflow = box.ComputedStyle.OverflowX != Overflow.Visible ||
                           box.ComputedStyle.OverflowY != Overflow.Visible;

        if (hasOverflow && box.Children.Count > 0)
        {
            RenderChildrenWithOverflow(box, deferred);
        }
        else
        {
            foreach (var child in OrderedChildren(box, deferred))
            {
                RenderBox(child, deferred);
            }
        }

        // 提取出的定位后代：按 z-index 升序绘制在本层内容之上（负 z-index 的 CSS 细分层级
        // ——背景之下、正常流之上——尚未区分，负值仍绘制在本层内容之后，只是彼此有序）。
        if (zOrdered != null)
        {
            foreach (var descendant in zOrdered)
            {
                if (descendant.ComputedStyle.ZIndex < 0) continue;
                RenderBox(descendant, null, isStackingRoot: true);
            }
        }

        // 5. 绘制滚动条（在裁剪区域之外）
        RenderScrollbars(box);

        if (hasTransform) _painter.Restore();
        if (hasOpacity) _painter.Restore();
    }

    /// <summary>
    /// 某个盒子是否建立层叠上下文（CSS "stacking context"）。
    /// <para>
    /// 这里只实现与本引擎相关的两条触发条件：<c>position</c> 非 static 且显式声明了
    /// <c>z-index</c>（非 auto）；以及 <c>opacity &lt; 1</c>（它同样建立层叠上下文，且本引擎
    /// 会为其 SaveLayer，后代无论如何都被关在那一层里）。transform / filter / will-change 等
    /// 其余触发条件尚未纳入。
    /// </para>
    /// </summary>
    private static bool EstablishesStackingContext(LayoutBox box)
    {
        var style = box.ComputedStyle;
        // opacity < 1 与 transform 都建立层叠上下文，且本引擎都会为其 SaveLayer / Save+变换，
        // 后代无论如何都被关在那一层里，提取出去绘制会丢掉这些效果。
        if (style.Opacity < 1f) return true;
        if (style.Transform.Functions.Count > 0) return true;
        return style.Position != Common.Position.Static && style.HasZIndex;
    }

    /// <summary>
    /// 该盒子是否作为「带 z-index 的定位后代」参与祖先层叠上下文的排序——即被
    /// <see cref="CollectZOrderedDescendants"/> 提出来单独按 z-index 绘制。
    /// </summary>
    private static bool IsZOrderedPositioned(LayoutBox box)
        => box.ComputedStyle.Position != Common.Position.Static && box.ComputedStyle.HasZIndex;

    /// <summary>
    /// 收集 <paramref name="root"/> 这个层叠上下文里所有「带 z-index 的定位后代」，按
    /// z-index 稳定排序（同值保持文档序）后返回；没有则返回 null（快速路径，零分配）。
    /// <para>
    /// 关键在于「穿透」：CSS 中只有建立了层叠上下文的祖先才会把后代的 z-index 关进自己的层级，
    /// 普通祖先（含 <c>position:relative</c> 但 <c>z-index:auto</c> 的盒子）是透明的，其后代的
    /// z-index 直接与更外层的兄弟比较。修复前本引擎只对「兄弟」排序，于是
    /// <c>ion-content</c>（relative、z-index:auto）里 <c>z-index:1000</c> 的 fab 永远只能和
    /// content 内部的兄弟比，而 content 自己（视同 0）排在 <c>ion-header</c>（z-index:10）之下，
    /// fab 越到 header 上的那一半就被 header 盖住了（issues/ion-fab.md 问题 3）。
    /// </para>
    /// <para>
    /// 被提出的后代由 <see cref="RenderBox"/> 在其所属层叠上下文的末尾统一绘制；沿途祖先的
    /// 裁剪 / 变换 / 透明度不会重复施加，因此本收集在遇到「会裁剪的祖先」时停止下探：那些后代
    /// 必须留在原地随祖先的裁剪一起绘制，否则会漏出裁剪框（滚动容器里的定位子元素）。
    /// </para>
    /// </summary>
    private static List<LayoutBox>? CollectZOrderedDescendants(LayoutBox root)
    {
        List<LayoutBox>? found = null;
        Collect(root, ref found);
        if (found == null || found.Count == 0) return null;

        // 稳定排序：文档序由收集顺序（深度优先前序）天然给出，OrderBy 在 .NET 中是稳定的。
        return found.OrderBy(b => b.ComputedStyle.ZIndex).ToList();

        static void Collect(LayoutBox box, ref List<LayoutBox>? found)
        {
            foreach (var child in box.Children)
            {
                // fixed 后代由 FlushFixed 的顶层 pass 绘制，不能再被这里提取——否则会画两遍，
                // 且提取出的那一遍仍活在祖先的裁剪/滚动状态里。
                if (child.ComputedStyle.Position == Common.Position.Fixed) continue;

                if (IsZOrderedPositioned(child))
                {
                    (found ??= new List<LayoutBox>()).Add(child);
                    // 该后代自身作为一个整体被排序绘制，其子树在它自己的层里处理，不再下探。
                    continue;
                }

                // 建立层叠上下文的后代把它自己的后代关在内部，不再穿透。
                if (EstablishesStackingContext(child)) continue;

                // 会裁剪的祖先：其后代必须留在原地绘制以承受裁剪，不提取。
                if (Clips(child)) continue;

                Collect(child, ref found);
            }
        }

        static bool Clips(LayoutBox box)
            => box.ComputedStyle.OverflowX != Overflow.Visible
            || box.ComputedStyle.OverflowY != Overflow.Visible;
    }

    /// <summary>
    /// 返回正常递归时要绘制的子元素——即跳过那些已被 <paramref name="deferred"/> 提出、
    /// 稍后按 z-index 统一绘制的后代。<paramref name="deferred"/> 为 null 时原样返回子列表。
    /// </summary>
    private static IEnumerable<LayoutBox> OrderedChildren(LayoutBox box, HashSet<LayoutBox>? deferred)
    {
        if (deferred == null) return box.Children;
        return box.Children.Where(c => !deferred.Contains(c));
    }

    private void ApplyTransform(LayoutBox box)
    {
        if (_painter == null) return;

        var borderBox = box.BoxModel.BorderBox;
        var origin = box.ComputedStyle.TransformOrigin;

        float originX = origin.X.Unit == LengthUnit.Percent
            ? borderBox.X + borderBox.Width * origin.X.Value / 100f
            : borderBox.X + origin.X.Value;
        float originY = origin.Y.Unit == LengthUnit.Percent
            ? borderBox.Y + borderBox.Height * origin.Y.Value / 100f
            : borderBox.Y + origin.Y.Value;

        _painter.Translate(originX, originY);

        foreach (var fn in box.ComputedStyle.Transform.Functions)
        {
            switch (fn)
            {
                case TransformFunction.Translate t:
                    float tx = t.X.Unit == LengthUnit.Percent
                        ? borderBox.Width * t.X.Value / 100f : t.X.Value;
                    float ty = t.Y.Unit == LengthUnit.Percent
                        ? borderBox.Height * t.Y.Value / 100f : t.Y.Value;
                    _painter.Translate(tx, ty);
                    break;
                case TransformFunction.TranslateX t:
                    float txVal = t.X.Unit == LengthUnit.Percent
                        ? borderBox.Width * t.X.Value / 100f : t.X.Value;
                    _painter.Translate(txVal, 0);
                    break;
                case TransformFunction.TranslateY t:
                    float tyVal = t.Y.Unit == LengthUnit.Percent
                        ? borderBox.Height * t.Y.Value / 100f : t.Y.Value;
                    _painter.Translate(0, tyVal);
                    break;
                case TransformFunction.Rotate r:
                    _painter.Rotate(r.Degrees);
                    break;
                case TransformFunction.Scale s:
                    _painter.Scale(s.X, s.Y);
                    break;
                case TransformFunction.ScaleX s:
                    _painter.Scale(s.X, 1f);
                    break;
                case TransformFunction.ScaleY s:
                    _painter.Scale(1f, s.Y);
                    break;
                case TransformFunction.SkewX s:
                    _painter.Skew(s.Degrees, 0);
                    break;
                case TransformFunction.SkewY s:
                    _painter.Skew(0, s.Degrees);
                    break;
                case TransformFunction.Skew s:
                    _painter.Skew(s.DegreesX, s.DegreesY);
                    break;
                case TransformFunction.Matrix m:
                    var matrix = new SKMatrix(
                        m.A, m.C, m.Tx,
                        m.B, m.D, m.Ty,
                        0, 0, 1);
                    _painter.Concat(matrix);
                    break;
            }
        }

        _painter.Translate(-originX, -originY);
    }

    /// <summary>
    /// 带溢出裁剪的子元素渲染
    /// </summary>
    private void RenderChildrenWithOverflow(LayoutBox box, HashSet<LayoutBox>? deferred = null)
    {
        if (_painter == null) return;

        var paddingBox = box.BoxModel.PaddingBox;
        float clipWidth = paddingBox.Width;
        float clipHeight = paddingBox.Height;

        if (box.ShowsVerticalScrollbar)
        {
            clipWidth -= box.VerticalScrollbarThickness;
        }
        if (box.ShowsHorizontalScrollbar)
        {
            clipHeight -= box.HorizontalScrollbarThickness;
        }

        var clipRect = new RectF(paddingBox.X, paddingBox.Y, clipWidth, clipHeight);

        float prevScrollX = _currentScrollOffsetX;
        float prevScrollY = _currentScrollOffsetY;
        _currentScrollOffsetX += box.ScrollLeft;
        _currentScrollOffsetY += box.ScrollTop;

        // 带圆角的裁剪盒（overflow != visible + border-radius）需按圆角路径裁剪子元素，
        // 否则子内容会溢出圆角（如圆形 ion-avatar 内的方形子块）。
        var (tl, tr, brr, bl) = ResolveBorderRadii(box);
        if (tl > 0 || tr > 0 || brr > 0 || bl > 0)
        {
            _painter.SaveClipRounded(clipRect, tl, tr, brr, bl);
        }
        else
        {
            _painter.Save();
            _painter.ClipRect(clipRect);
        }
        _painter.Translate(-box.ScrollLeft, -box.ScrollTop);

        // 裁剪盒的后代不会被提取（CollectZOrderedDescendants 在会裁剪的祖先处停止下探），
        // 故这里一般 deferred 为空；仍传递以保持与非裁剪分支同一语义。
        foreach (var child in OrderedChildren(box, deferred))
        {
            RenderBox(child, deferred);
        }

        _painter.Restore();

        _currentScrollOffsetX = prevScrollX;
        _currentScrollOffsetY = prevScrollY;
    }

    /// <summary>
    /// 渲染滚动条
    /// </summary>
    private void RenderScrollbars(LayoutBox box)
    {
        if (_painter == null) return;

        var paddingBox = box.BoxModel.PaddingBox;
        bool hasVScrollbar = box.ShowsVerticalScrollbar;
        bool hasHScrollbar = box.ShowsHorizontalScrollbar;

        if (hasVScrollbar)
        {
            float thickness = box.VerticalScrollbarThickness;
            float trackX = paddingBox.Right - thickness;
            float trackHeight = paddingBox.Height - box.HorizontalScrollbarThickness;
            var trackRect = new RectF(trackX, paddingBox.Y, thickness, trackHeight);

            _painter.DrawVerticalScrollbar(
                trackRect,
                box.ScrollTop,
                box.ScrollableContentHeight,
                trackHeight);
        }

        if (hasHScrollbar)
        {
            float thickness = box.HorizontalScrollbarThickness;
            float trackY = paddingBox.Bottom - thickness;
            float trackWidth = paddingBox.Width - box.VerticalScrollbarThickness;
            var trackRect = new RectF(paddingBox.X, trackY, trackWidth, thickness);

            _painter.DrawHorizontalScrollbar(
                trackRect,
                box.ScrollLeft,
                box.ScrollableContentWidth,
                trackWidth);
        }
    }

    /// <summary>
    /// 检查是否应该渲染
    /// </summary>
    private bool ShouldRender(LayoutBox box)
    {
        // 检查是否在脏区域内
        if (_dirtyRegions != null && _dirtyRegions.Count > 0)
        {
            return _dirtyRegions.Any(r => r.IntersectsWith(box.BoxModel.BorderBox));
        }

        return true;
    }

    /// <summary>
    /// 盒的可视矩形列表：跨行的非替换 inline 盒有逐行片段（ISSUE-126），
    /// 背景/边框/阴影/轮廓应逐片段绘制（浏览器的 inline box fragment 行为）；
    /// 其余盒子只有单个边框盒。
    /// </summary>
    private static IReadOnlyList<RectF> PaintRects(LayoutBox box)
        => box.InlineFragments is { Count: > 0 } frags
            ? frags
            : new[] { box.BoxModel.BorderBox };

    /// <summary>
    /// 渲染盒阴影
    /// </summary>
    private void RenderBoxShadow(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        // BoxShadow 未被 ComputedStyle 遮蔽，仍是基类的 StyleProperty<List<BoxShadow>>?。
        var boxShadow = style.BoxShadow.RefValueOrNull();
        if (boxShadow == null || boxShadow.Count == 0) return;

        var (tl, tr, br, bl) = ResolveBorderRadii(box);
        foreach (var rect in PaintRects(box))
        {
            _painter.DrawBoxShadow(boxShadow, rect, tl, tr, br, bl);
        }
    }

    /// <summary>
    /// 将四角圆角（Length，可能含 rem/em/percent 单位）解析为像素值。
    /// 百分比按 CSS 语义相对边框盒尺寸解析（水平角对宽度、垂直角对高度），
    /// 由于绘制层每角仅接受单一半径，故取较小边作为百分比基准（使 50% 在正方形上得到圆形）。
    /// </summary>
    private static (float TopLeft, float TopRight, float BottomRight, float BottomLeft)
        ResolveBorderRadii(LayoutBox box)
    {
        var style = box.ComputedStyle;
        var borderBox = box.BoxModel.BorderBox;
        float percentBase = Math.Min(borderBox.Width, borderBox.Height);
        float fontSize = style.FontSize.ToPixels(0);

        return (
            style.BorderTopLeftRadius.ToPixels(percentBase, fontSize),
            style.BorderTopRightRadius.ToPixels(percentBase, fontSize),
            style.BorderBottomRightRadius.ToPixels(percentBase, fontSize),
            style.BorderBottomLeftRadius.ToPixels(percentBase, fontSize)
        );
    }

    /// <summary>
    /// 判断盒子是否需要把内容裁剪成圆角形状：overflow 非 visible（即会裁剪溢出）且存在非零圆角。
    /// 满足时输出四角的像素半径。用于让图片/子内容跟随圆角裁剪（浏览器行为）。
    /// </summary>
    private static bool ClipsContentToRoundedBox(
        LayoutBox box, out float topLeft, out float topRight, out float bottomRight, out float bottomLeft)
    {
        topLeft = topRight = bottomRight = bottomLeft = 0f;

        var style = box.ComputedStyle;
        if (style.OverflowX == Overflow.Visible && style.OverflowY == Overflow.Visible)
            return false;

        (topLeft, topRight, bottomRight, bottomLeft) = ResolveBorderRadii(box);
        return topLeft > 0 || topRight > 0 || bottomRight > 0 || bottomLeft > 0;
    }

    /// <summary>
    /// 渲染背景
    /// </summary>
    private void RenderBackground(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        if (style.BackgroundColor.A > 0)
        {
            var (tl, tr, br, bl) = ResolveBorderRadii(box);
            foreach (var rect in PaintRects(box))
            {
                _painter.DrawBackground(rect, style.BackgroundColor, tl, tr, br, bl);
            }
        }

        if (style.BackgroundImage?.Bitmap != null)
        {
            RenderBackgroundImage(style, box.BoxModel.PaddingBox);
        }
    }

    private void RenderBackgroundImage(ComputedStyle style, RectF area)
    {
        var bgImage = style.BackgroundImage!;
        var bitmap = bgImage.Bitmap!;
        var imgWidth = bgImage.OriginalWidth;
        var imgHeight = bgImage.OriginalHeight;

        float drawWidth, drawHeight;
        switch (style.BackgroundSize.Mode)
        {
            case BackgroundSizeMode.Cover:
                var coverScale = Math.Max(area.Width / imgWidth, area.Height / imgHeight);
                drawWidth = imgWidth * coverScale;
                drawHeight = imgHeight * coverScale;
                break;
            case BackgroundSizeMode.Contain:
                var containScale = Math.Min(area.Width / imgWidth, area.Height / imgHeight);
                drawWidth = imgWidth * containScale;
                drawHeight = imgHeight * containScale;
                break;
            case BackgroundSizeMode.Explicit:
                drawWidth = style.BackgroundSize.ResolveWidth(area.Width, imgWidth);
                drawHeight = style.BackgroundSize.ResolveHeight(area.Height, imgHeight);
                break;
            default:
                drawWidth = imgWidth;
                drawHeight = imgHeight;
                break;
        }

        float startX = area.X, startY = area.Y;
        switch (style.BackgroundPosition)
        {
            case BackgroundPosition.CenterTop:
            case BackgroundPosition.Center:
            case BackgroundPosition.CenterBottom:
                startX = area.X + (area.Width - drawWidth) / 2;
                break;
            case BackgroundPosition.RightTop:
            case BackgroundPosition.RightCenter:
            case BackgroundPosition.RightBottom:
                startX = area.X + area.Width - drawWidth;
                break;
        }
        switch (style.BackgroundPosition)
        {
            case BackgroundPosition.LeftCenter:
            case BackgroundPosition.Center:
            case BackgroundPosition.RightCenter:
                startY = area.Y + (area.Height - drawHeight) / 2;
                break;
            case BackgroundPosition.LeftBottom:
            case BackgroundPosition.CenterBottom:
            case BackgroundPosition.RightBottom:
                startY = area.Y + area.Height - drawHeight;
                break;
        }

        var renderBitmap = (drawWidth != imgWidth || drawHeight != imgHeight)
            ? bgImage.RenderAtSize((int)drawWidth, (int)drawHeight) ?? bitmap
            : bitmap;

        // Template icons (Ionicons SVG masks) are tinted with the element's color — CSS fill: currentColor.
        Color? tint = bgImage.IsTemplate ? style.Color : (Color?)null;

        _painter!.SaveClip(area);

        switch (style.BackgroundRepeat)
        {
            case BackgroundRepeat.Repeat:
                TileImage(renderBitmap, area, startX, startY, drawWidth, drawHeight, true, true, tint);
                break;
            case BackgroundRepeat.RepeatX:
                TileImage(renderBitmap, area, startX, startY, drawWidth, drawHeight, true, false, tint);
                break;
            case BackgroundRepeat.RepeatY:
                TileImage(renderBitmap, area, startX, startY, drawWidth, drawHeight, false, true, tint);
                break;
            case BackgroundRepeat.NoRepeat:
                _painter.DrawImage(renderBitmap, new RectF(startX, startY, drawWidth, drawHeight), tint);
                break;
        }

        _painter.Restore();
    }

    private void TileImage(SKBitmap bitmap, RectF area, float startX, float startY, float tileW, float tileH, bool repeatX, bool repeatY, Color? tint = null)
    {
        float originX = startX;
        if (repeatX)
        {
            while (originX > area.X) originX -= tileW;
        }
        float originY = startY;
        if (repeatY)
        {
            while (originY > area.Y) originY -= tileH;
        }

        float endX = repeatX ? area.Right : originX + tileW;
        float endY = repeatY ? area.Bottom : originY + tileH;

        for (float y = originY; y < endY; y += tileH)
        {
            for (float x = originX; x < endX; x += tileW)
            {
                _painter!.DrawImage(bitmap, new RectF(x, y, tileW, tileH), tint);
            }
        }
    }

    /// <summary>
    /// 渲染边框
    /// </summary>
    private void RenderBorder(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;

        bool hasVisibleBorder =
            style.ComputedBorderTop.IsVisible ||
            style.ComputedBorderRight.IsVisible ||
            style.ComputedBorderBottom.IsVisible ||
            style.ComputedBorderLeft.IsVisible;

        if (!hasVisibleBorder) return;

        var (tl, tr, br, bl) = ResolveBorderRadii(box);
        foreach (var rect in PaintRects(box))
        {
            _painter.DrawBorderSides(
                rect,
                style.ComputedBorderTop,
                style.ComputedBorderRight,
                style.ComputedBorderBottom,
                style.ComputedBorderLeft,
                tl, tr, br, bl
            );
        }
    }

    /// <summary>
    /// 渲染轮廓（CSS outline）。轮廓绘制在边框盒之外，遵循 outline-offset，不影响布局。
    /// </summary>
    private void RenderOutline(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        if (!style.HasVisibleOutline) return;

        float width = style.OutlineWidth.ToPixels(0, style.FontSize.Value);
        float offset = style.OutlineOffset.ToPixels(0, style.FontSize.Value);

        foreach (var rect in PaintRects(box))
        {
            _painter.DrawOutline(
                rect,
                width,
                style.OutlineColor,
                style.OutlineStyle,
                offset,
                style.BorderTopLeftRadius.Value,
                style.BorderTopRightRadius.Value,
                style.BorderBottomRightRadius.Value,
                style.BorderBottomLeftRadius.Value
            );
        }
    }

    /// <summary>
    /// 渲染内容
    /// </summary>
    private void RenderContent(LayoutBox box)
    {
        if (_painter == null) return;

        var element = box.Element;

        // 渲染输入框
        if (element is InputElement inputElement)
        {
            RenderInputElement(box, inputElement);
            return;
        }

        // 渲染多行文本框
        if (element is TextAreaElement textAreaElement)
        {
            RenderTextAreaElement(box, textAreaElement);
            return;
        }

        // 渲染下拉选择框
        if (element is SelectElement selectElement)
        {
            RenderSelectElement(box, selectElement);
            return;
        }

        // 渲染文本节点（TextNode）。文本自 ISSUE-086 起以有序文本节点子盒形式存在，
        // 由各自的 TextNode 盒在布局中定位，此处按其内容盒绘制。
        if (element is Miko.Core.DomElements.TextNode)
        {
            RenderTextNode(box);
        }

        // 渲染图片
        if (element is ImageElement imageElement)
        {
            RenderImage(box, imageElement);
        }

        // 渲染视频帧
        if (element is Miko.Core.DomElements.VideoElement videoElement)
        {
            RenderVideoFrame(box, videoElement);
        }
    }

    /// <summary>
    /// 绘制文本节点（<see cref="Core.DomElements.TextNode"/>）。
    ///
    /// 文本节点的内容盒已由 <see cref="Layout.LayoutAlgorithms.TextLayout"/> 定位并测出实际文本尺寸。
    /// 水平对齐（text-align）以父元素内容盒为参照：单行时在父内容宽度内对齐；文本超宽需换行时按父
    /// 内容盒多行绘制。垂直方向遵循 CSS 行盒模型（半行距上下均分），因此在纯文本场景下文本垂直居中于
    /// 其行盒（保持 ISSUE-070 行为）。坐标做像素对齐以保持抗锯齿清晰度（见 ISSUE-085 抗锯齿修复）。
    /// </summary>
    private void RenderTextNode(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        // 应用 text-transform（与布局测量一致）。
        var text = Utils.TextTransformer.Apply(box.Element.TextContent, style.TextTransform);
        if (string.IsNullOrEmpty(text)) return;

        var content = box.BoxModel.Content;

        // letter-spacing 与长单词断行（word-break / overflow-wrap）。
        float letterSpacing = style.LetterSpacing.ToPixels(0, style.FontSize.Value);
        bool breakLongWords = Utils.TextWrapper.ShouldBreakLongWords(style.WordBreak, style.OverflowWrap);

        // text-align 的对齐参照：
        // - 父元素为 flex/grid 容器时：文本节点是 flex/grid 项目，已由 justify-content /
        //   align-content 等容器对齐属性定位；CSS 中 text-align 只作用于块容器的行内内容，
        //   不适用于 flex/grid 项目。必须按文本节点自身内容盒绘制，否则绘制期会把
        //   flex 居中的文本重新锚定回父内容盒边缘（见 ISSUE-099 问题4：flex 按钮文本居左）。
        // - 当文本节点是父元素唯一的在流内容、且父处于常规流（block/inline-block 等）时，
        //   以父内容盒作为对齐容器，使 center/right 生效（覆盖纯文本元素与 button 的居中场景）。
        // - 否则（存在交错的兄弟元素，如 text1 <span/> text3），文本已由布局定位到其行内位置，
        //   按自身内容盒左对齐绘制，不再二次对齐（避免破坏交错顺序）。
        var parent = box.Element.Parent;
        bool parentIsNormalFlow = parent?.LayoutBox?.Type
            is not (Common.LayoutType.Flex or Common.LayoutType.InlineFlex or Common.LayoutType.Grid);
        bool textNodeIsSoleInlineContent = parent != null && parentIsNormalFlow && IsSoleInFlowInlineChild(box);

        float alignX;
        float alignWidth;
        if (textNodeIsSoleInlineContent && parent!.LayoutBox != null)
        {
            var parentContent = parent.LayoutBox.BoxModel.Content;
            alignX = parentContent.X;
            alignWidth = parentContent.Width;
        }
        else
        {
            alignX = content.X;
            alignWidth = content.Width;
        }

        // 是否需要多行绘制：文本宽度超过对齐容器宽度，或包含换行符。
        bool shouldWrap = Utils.TextWrapper.ShouldWrap(style.WhiteSpace);
        // pre 统一走多行路径：保留的显式换行符产生多行，且多行路径会先经 ProcessText
        // 完成 \r\n 归一与 Tab 展开（单行 pre 文本走该路径的绘制结果与单行路径一致）。
        bool needsMultiline = style.WhiteSpace == Common.WhiteSpace.Pre;
        if (shouldWrap && alignWidth > 0)
        {
            var processedText = Utils.TextWrapper.ProcessText(text, style.WhiteSpace);
            var (singleLineWidth, _) = Utils.TextMeasurer.MeasureText(
                processedText, style.FontFamily, style.FontSize.Value, style.FontWeight);
            needsMultiline = singleLineWidth > alignWidth + 0.5f || processedText.Contains('\n');
        }

        // 语法高亮（ISSUE-098）：父元素为 <code> 且高亮生效时按 token 着色绘制。
        // DOM 保持单一文本节点，测量/布局不变，仅绘制阶段分段着色；
        // DrawHighlightedText 自行按显式换行符分行，兼容单行与多行（不做软换行）。
        if (box.Element.Parent is CodeElement { IsHighlightActive: true } codeElement)
        {
            var processed = Utils.TextWrapper.ProcessText(text, style.WhiteSpace);
            var tokens = string.IsNullOrEmpty(processed)
                ? null
                : codeElement.GetHighlightTokens(processed, SyntaxHighlighter);
            if (tokens != null)
            {
                float hlLineHeight = Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(style);
                var hlRect = new RectF(MathF.Round(alignX), MathF.Round(content.Y), alignWidth, content.Height);
                _painter.DrawHighlightedText(
                    processed, tokens, SyntaxHighlighter.Theme, hlRect, style.Color, style.FontFamily,
                    style.FontSize.Value, style.FontWeight, style.TextAlign, hlLineHeight, letterSpacing);
                return;
            }
        }

        // 行内断行片段（ISSUE-110）：文本节点已由行内格式化上下文切分为行片段并完成
        // 定位（含 text-align 的行级对齐），逐片段单行绘制即可，不再按内容盒二次换行/对齐。
        // 片段坐标相对内容盒原点（内容盒为全部片段的并集）。
        if (box.Element is Miko.Core.DomElements.TextNode fragNode
            && fragNode.LayoutFragments is { Count: > 0 } fragments)
        {
            foreach (var frag in fragments)
            {
                var fragRect = new RectF(
                    MathF.Round(content.X + frag.X),
                    MathF.Round(content.Y + frag.Y),
                    frag.Width,
                    frag.Height);
                _painter.DrawTextLine(
                    frag.Text, fragRect, style.Color, style.FontFamily, style.FontSize.Value,
                    style.FontWeight, letterSpacing);

                if (style.TextDecoration != Common.TextDecoration.None)
                {
                    _painter.DrawTextDecoration(frag.Text, fragRect, style.Color, style.FontFamily,
                        style.FontSize.Value, style.FontWeight, Common.TextAlign.Left,
                        style.TextDecoration, VerticalAlign.Middle);
                }
            }
            return;
        }

        if (needsMultiline)
        {
            float lineHeight = Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(style);
            // 多行文本在对齐容器宽度内换行与水平对齐，从文本节点顶部开始向下排列。
            var wrapRect = new RectF(MathF.Round(alignX), MathF.Round(content.Y), alignWidth, content.Height);
            _painter.DrawMultilineText(
                text, wrapRect, style.Color, style.FontFamily, style.FontSize.Value, style.FontWeight,
                style.TextAlign, lineHeight, style.WhiteSpace, VerticalAlign.Top, breakLongWords, letterSpacing);

            if (style.TextDecoration != Common.TextDecoration.None)
            {
                _painter.DrawTextDecoration(text, wrapRect, style.Color, style.FontFamily,
                    style.FontSize.Value, style.FontWeight, style.TextAlign, style.TextDecoration, VerticalAlign.Top);
            }
            return;
        }

        // text-overflow: ellipsis 仅在单行不换行（white-space: nowrap）且父容器裁剪溢出
        // （overflow != visible）时生效，与 CSS 一致。
        var textOverflow = ResolveTextOverflow(box, style);

        // 单行：在对齐容器宽度内按 text-align 水平对齐，垂直居中于文本节点行盒。
        var textRect = new RectF(MathF.Round(alignX), MathF.Round(content.Y), alignWidth, content.Height);
        _painter.DrawText(
            text, textRect, style.Color, style.FontFamily, style.FontSize.Value, style.FontWeight,
            style.TextAlign, VerticalAlign.Middle, letterSpacing, textOverflow);

        if (style.TextDecoration != Common.TextDecoration.None)
        {
            _painter.DrawTextDecoration(text, textRect, style.Color, style.FontFamily,
                style.FontSize.Value, style.FontWeight, style.TextAlign, style.TextDecoration, VerticalAlign.Middle);
        }
    }

    /// <summary>
    /// 解析文本节点生效的 <c>text-overflow</c>。CSS 中 <c>text-overflow</c> 设在裁剪溢出的
    /// 块容器上（非文本节点），且仅在单行不换行时生效。因此这里从父元素读取 text-overflow，
    /// 并要求父元素 white-space: nowrap 且水平方向裁剪溢出（overflow-x != visible）。
    /// 不满足条件时返回 <see cref="TextOverflow.Clip"/>（不省略）。
    /// </summary>
    private static TextOverflow ResolveTextOverflow(LayoutBox box, ComputedStyle textStyle)
    {
        // 文本节点不换行时 white-space 由继承得到，与父元素一致；直接检查文本节点即可。
        if (textStyle.WhiteSpace != WhiteSpace.Nowrap && textStyle.WhiteSpace != WhiteSpace.Pre)
            return TextOverflow.Clip;

        var parentStyle = box.Element.Parent?.LayoutBox?.ComputedStyle;
        if (parentStyle == null) return TextOverflow.Clip;

        if (parentStyle.TextOverflow != TextOverflow.Ellipsis) return TextOverflow.Clip;

        // 需要水平裁剪（overflow-x 非 visible）才会触发省略号。
        if (parentStyle.OverflowX == Overflow.Visible) return TextOverflow.Clip;

        return TextOverflow.Ellipsis;
    }

    /// <summary>
    /// 判断一个文本节点盒是否为其父元素唯一的在流行内内容（即父元素只有这一个非脱流子节点）。
    /// 用于决定文本是否应以父内容盒作为 text-align 的对齐容器。
    /// </summary>
    private static bool IsSoleInFlowInlineChild(LayoutBox textBox)
    {
        var parentBox = textBox.Element.Parent?.LayoutBox;
        if (parentBox == null) return false;

        int inFlowCount = 0;
        foreach (var sibling in parentBox.Children)
        {
            var pos = sibling.ComputedStyle.Position;
            if (pos == Common.Position.Absolute || pos == Common.Position.Fixed) continue;
            inFlowCount++;
            if (inFlowCount > 1) return false;
        }
        return inFlowCount == 1;
    }

    /// <summary>
    /// 渲染图片元素。真实图已解码时填满内容盒（保持既有行为）；尚未就绪时回退到占位图
    /// （object-fit: contain，保持纵横比），无占位图则仅保留背景色。加载完成后引擎标脏，下一帧自动切换。
    /// </summary>
    private void RenderImage(LayoutBox box, ImageElement img)
    {
        if (_painter == null) return;

        var content = box.BoxModel.Content;
        if (content.Width <= 0 || content.Height <= 0) return;

        if (img.Bitmap != null)
        {
            _painter.DrawImage(img.Bitmap, content);
            return;
        }

        if (img.PlaceholderBitmap != null)
        {
            var dst = FitContain(img.PlaceholderBitmap.Width, img.PlaceholderBitmap.Height, content);
            _painter.DrawImage(img.PlaceholderBitmap, dst);
        }
    }

    /// <summary>
    /// 把视频当前帧合成进内容盒。帧是一张 GPU 图像，DrawImage 后即与其它元素进入同一
    /// Skia 命令流，自动获得 overflow 裁剪、圆角、opacity、transform 与兄弟覆盖等链路。
    /// 首帧前回退到 poster 占位图，无 poster 时仅保留背景色（已在 RenderBackground 绘制）。
    /// </summary>
    private void RenderVideoFrame(LayoutBox box, Miko.Core.DomElements.VideoElement video)
    {
        if (_painter == null) return;

        var content = box.BoxModel.Content;
        if (content.Width <= 0 || content.Height <= 0) return;

        var frame = video.Session?.FrameSource.AcquireCurrentFrame(GraphicsContext);
        if (frame != null)
        {
            try
            {
                // 视频默认 object-fit: contain（letterbox），保持纵横比不变形。
                var dst = FitContain(frame.Width, frame.Height, content);
                _painter.DrawImage(frame, dst);
            }
            finally
            {
                video.Session!.FrameSource.ReleaseCurrentFrame();
            }
            return;
        }

        // 首帧前：绘制 poster 占位图（若已解码）。
        if (video.PosterBitmap != null)
        {
            var dst = FitContain(video.PosterBitmap.Width, video.PosterBitmap.Height, content);
            _painter.DrawImage(video.PosterBitmap, dst);
        }
    }

    /// <summary>
    /// 计算 object-fit: contain 的目标矩形：在 <paramref name="content"/> 内按源纵横比
    /// 等比缩放并居中，产生上下或左右的 letterbox 空白（由背景色填充）。
    /// </summary>
    private static RectF FitContain(float srcW, float srcH, RectF content)
    {
        if (srcW <= 0 || srcH <= 0) return content;

        float scale = Math.Min(content.Width / srcW, content.Height / srcH);
        float w = srcW * scale;
        float h = srcH * scale;
        float x = content.X + (content.Width - w) / 2f;
        float y = content.Y + (content.Height - h) / 2f;
        return new RectF(x, y, w, h);
    }

    /// <summary>
    /// 渲染输入框元素
    /// </summary>
    private void RenderInputElement(LayoutBox box, InputElement inputElement)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var contentRect = box.BoxModel.Content;
        bool isFocused = inputElement.HasState(Miko.Core.ElementState.Focus);

        switch (inputElement.Type)
        {
            case InputType.Checkbox:
                _painter.DrawCheckbox(
                    contentRect,
                    inputElement.Checked,
                    style.BorderTopColor,
                    style.Color,
                    style.BackgroundColor
                );
                break;

            case InputType.Radio:
                _painter.DrawRadio(
                    contentRect,
                    inputElement.Checked,
                    style.BorderTopColor,
                    style.Color,
                    style.BackgroundColor
                );
                break;

            case InputType.Range:
                // 获取 range 伪元素样式
                Style? trackStyle = null;
                Style? progressStyle = null;
                Style? thumbStyle = null;

                if (inputElement.PseudoElementStyles != null)
                {
                    inputElement.PseudoElementStyles.TryGetValue(PseudoElementType.RangeTrack, out trackStyle);
                    inputElement.PseudoElementStyles.TryGetValue(PseudoElementType.RangeProgress, out progressStyle);
                    inputElement.PseudoElementStyles.TryGetValue(PseudoElementType.RangeThumb, out thumbStyle);
                }

                _painter.DrawRange(
                    contentRect,
                    inputElement.NumericValue,
                    inputElement.Min,
                    inputElement.Max,
                    trackStyle,
                    progressStyle,
                    thumbStyle,
                    style.FontSize.Value  // Pass fontSize for em/rem resolution
                );
                break;

            case InputType.Password:
                _painter.SaveClip(box.BoxModel.PaddingBox);
                if (!string.IsNullOrEmpty(inputElement.Value))
                {
                    _painter.DrawPasswordText(
                        inputElement.Value.Length,
                        contentRect,
                        style.Color,
                        style.FontSize.Value
                    );
                }
                else if (!string.IsNullOrEmpty(inputElement.Placeholder) && !isFocused)
                {
                    _painter.DrawText(
                        inputElement.Placeholder,
                        contentRect,
                        Color.Gray,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left,
                        VerticalAlign.Middle
                    );
                }
                if (isFocused)
                {
                    var maskedText = new string('●', (inputElement.Value ?? string.Empty).Length);
                    _painter.DrawTextCursor(contentRect, maskedText, inputElement.CursorPosition, style.FontFamily, style.FontSize.Value, style.FontWeight, style.ResolvedCaretColor);
                }
                _painter.Restore();
                break;

            case InputType.Text:
            default:
                // 单行输入：内容超出内容宽度后，按光标位置水平滚动，使光标始终可见；
                // 超出内容盒的文本被裁剪不显示（对齐浏览器 input 的行为）。
                _painter.SaveClip(box.BoxModel.PaddingBox);
                if (!string.IsNullOrEmpty(inputElement.Value))
                {
                    float scrollX = ComputeInputScrollOffset(
                        inputElement.Value, inputElement.CursorPosition, contentRect.Width,
                        style.FontFamily, style.FontSize.Value, style.FontWeight, isFocused);
                    var scrolledRect = new RectF(
                        contentRect.X - scrollX, contentRect.Y, contentRect.Width + scrollX, contentRect.Height);
                    _painter.DrawText(
                        inputElement.Value,
                        scrolledRect,
                        style.Color,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left,
                        VerticalAlign.Middle
                    );
                    if (isFocused)
                    {
                        _painter.DrawTextCursor(scrolledRect, inputElement.Value, inputElement.CursorPosition, style.FontFamily, style.FontSize.Value, style.FontWeight, style.ResolvedCaretColor);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(inputElement.Placeholder) && !isFocused)
                    {
                        _painter.DrawText(
                            inputElement.Placeholder,
                            contentRect,
                            Color.Gray,
                            style.FontFamily,
                            style.FontSize.Value,
                            style.FontWeight,
                            TextAlign.Left,
                            VerticalAlign.Middle
                        );
                    }
                    if (isFocused)
                    {
                        _painter.DrawTextCursor(contentRect, string.Empty, 0, style.FontFamily, style.FontSize.Value, style.FontWeight, style.ResolvedCaretColor);
                    }
                }
                _painter.Restore();
                break;
        }
    }

    /// <summary>
    /// 计算单行输入框的水平滚动偏移：当光标前文本宽度超过内容宽度时，向左滚动使光标落在内容盒右边缘，
    /// 保持光标可见；否则不滚动（从文本起始处显示，右侧超出部分被裁剪）。
    /// 仅在聚焦时滚动跟随光标；未聚焦时从起始处显示（offset=0）。
    /// </summary>
    private static float ComputeInputScrollOffset(
        string text, int cursorPosition, float contentWidth,
        string fontFamily, float fontSize, FontWeight fontWeight, bool isFocused)
    {
        if (!isFocused || contentWidth <= 0) return 0;

        int pos = Math.Clamp(cursorPosition, 0, text.Length);
        float caretX = Utils.TextMeasurer.MeasureTextWidth(
            text.Substring(0, pos), fontFamily, fontSize, fontWeight);

        return caretX > contentWidth ? caretX - contentWidth : 0;
    }

    /// <summary>
    /// 渲染多行文本框元素 (textarea)。
    ///
    /// 文本在内容盒内按行盒自顶向下多行绘制（white-space: pre-wrap），过长的行在内容宽度内换行。
    /// 无内容且未聚焦时绘制灰色占位符。聚焦时在光标所在行/列绘制文本光标。
    /// </summary>
    private void RenderTextAreaElement(LayoutBox box, TextAreaElement textArea)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var contentRect = box.BoxModel.Content;
        bool isFocused = textArea.HasState(Miko.Core.ElementState.Focus);
        float lineHeight = Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(style);

        // 裁剪到内容盒（padding box 内）：超出内容宽/高的软换行文本与光标不应溢出控件绘制。
        // textarea 采用软换行，任何超出内容宽度的内容都换行（breakLongWords: true），
        // 但换行后总高度仍可能超过可见高度，故仍需裁剪。
        _painter.SaveClip(box.BoxModel.PaddingBox);

        if (!string.IsNullOrEmpty(textArea.Value))
        {
            _painter.DrawMultilineText(
                textArea.Value,
                contentRect,
                style.Color,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight,
                TextAlign.Left,
                lineHeight,
                Common.WhiteSpace.PreWrap,
                VerticalAlign.Top,
                breakLongWords: true);
        }
        else if (!string.IsNullOrEmpty(textArea.Placeholder) && !isFocused)
        {
            _painter.DrawMultilineText(
                textArea.Placeholder,
                contentRect,
                Color.Gray,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight,
                TextAlign.Left,
                lineHeight,
                Common.WhiteSpace.PreWrap,
                VerticalAlign.Top,
                breakLongWords: true);
        }

        if (isFocused)
        {
            RenderTextAreaCursor(box, textArea, lineHeight);
        }

        _painter.Restore();
    }

    /// <summary>
    /// 绘制 textarea 的文本光标。textarea 采用软换行，因此光标所在的视觉行/列需按与绘制一致的
    /// 换行规则（含逐字符断行）重新计算：先取光标前文本，按内容宽度软换行后，光标定位在最后一段
    /// 视觉行的行尾。
    /// </summary>
    private void RenderTextAreaCursor(LayoutBox box, TextAreaElement textArea, float lineHeight)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var contentRect = box.BoxModel.Content;
        var value = textArea.Value ?? string.Empty;
        int pos = Math.Clamp(textArea.CursorPosition, 0, value.Length);
        var before = value.Substring(0, pos);

        // 与绘制一致：按内容宽度对“光标前文本”软换行（含长单词逐字符断行），
        // 视觉行号为换行结果的行数-1，当前列文本为最后一段视觉行。
        var processed = Utils.TextWrapper.ProcessText(before, Common.WhiteSpace.PreWrap);
        var lines = Utils.TextWrapper.WrapText(
            processed, style.FontFamily, style.FontSize.Value, style.FontWeight,
            contentRect.Width, Common.WhiteSpace.PreWrap, breakLongWords: true);

        int lineIndex = lines.Count > 0 ? lines.Count - 1 : 0;
        // 若光标前文本以显式换行结尾，WrapText 会为末尾空段补一行；此处直接取最后一段作为当前行。
        var currentLine = lines.Count > 0 ? lines[^1] : string.Empty;

        float lineTop = contentRect.Top + lineIndex * lineHeight;
        // 复用单行光标绘制：以当前行文本 + 光标位于该行末尾，定位在当前行的行盒内。
        var cursorRect = new RectF(contentRect.X, lineTop, contentRect.Width, lineHeight);
        _painter.DrawTextCursor(
            cursorRect, currentLine, currentLine.Length,
            style.FontFamily, style.FontSize.Value, style.FontWeight, style.ResolvedCaretColor);
    }

    /// <summary>
    /// 渲染下拉选择框元素
    /// </summary>
    private void RenderSelectElement(LayoutBox box, SelectElement selectElement)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var borderBox = box.BoxModel.BorderBox;

        _painter.DrawSelect(
            borderBox,
            selectElement.GetDisplayText(),
            selectElement.IsOpen,
            style.BorderTopColor,
            style.BackgroundColor,
            style.Color,
            Color.Gray,
            style.FontSize.Value
        );

        if (selectElement.IsOpen)
        {
            _pendingDropdowns.Add((box, selectElement, _currentScrollOffsetX, _currentScrollOffsetY));
        }
    }

    /// <summary>
    /// 渲染下拉选项列表
    /// </summary>
    private void RenderSelectDropdown(LayoutBox box, SelectElement selectElement, float scrollOffsetX, float scrollOffsetY)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var borderBox = box.BoxModel.BorderBox;

        float screenLeft = borderBox.Left - scrollOffsetX;
        float screenTop = borderBox.Bottom - scrollOffsetY;

        var options = new List<(string text, bool isSelected, bool isDisabled, bool isGroupLabel)>();
        var allOptions = selectElement.GetAllOptions();
        int optionIndex = 0;

        foreach (var child in selectElement.Children)
        {
            if (child is OptGroupElement optGroup)
            {
                options.Add((optGroup.Label ?? string.Empty, false, false, true));

                foreach (var groupChild in optGroup.Children)
                {
                    if (groupChild is OptionElement option)
                    {
                        bool isSelected = optionIndex == selectElement.SelectedIndex || option.Selected;
                        bool isDisabled = option.IsDisabled;
                        options.Add((option.TextContent ?? option.Value ?? string.Empty, isSelected, isDisabled, false));
                        optionIndex++;
                    }
                }
            }
            else if (child is OptionElement option)
            {
                bool isSelected = optionIndex == selectElement.SelectedIndex || option.Selected;
                bool isDisabled = option.IsDisabled;
                options.Add((option.TextContent ?? option.Value ?? string.Empty, isSelected, isDisabled, false));
                optionIndex++;
            }
        }

        float optionHeight = style.FontSize.Value + 8;
        float dropdownHeight = options.Count * optionHeight;
        var dropdownRect = new RectF(
            screenLeft,
            screenTop,
            borderBox.Width,
            dropdownHeight
        );

        _painter.DrawSelectDropdown(
            dropdownRect,
            options,
            Color.White,
            style.BorderTopColor,
            style.Color,
            new Color(0, 120, 215),
            Color.White,
            Color.Gray,
            Color.Gray,
            style.FontSize.Value
        );
    }
}
