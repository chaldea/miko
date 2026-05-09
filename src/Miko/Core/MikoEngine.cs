using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Common;
using Miko.Core;
using Miko.Events;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Core;

public class MikoEngine
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();
    private readonly DirtyRegionManager _dirtyManager = new();
    private readonly EventDispatcher _eventDispatcher = new();
    private List<StyleSheet> _styleSheets = new();
    private ILogger _logger = NullLogger.Instance;

    public void SetLogger(ILogger logger) => _logger = logger;

    private Element? _root;
    private LayoutBox? _currentLayout;
    private float _viewportWidth;
    private float _viewportHeight;

    public void Initialize(Element root, List<StyleSheet> styleSheets, SKCanvas canvas, float viewportWidth, float viewportHeight)
    {
        _root = root;
        _styleSheets = styleSheets;
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        EnsureParentReferences(root);
        _renderEngine.SetCanvas(canvas);

        _logger.LogInformation("Engine initialized with viewport {Width}x{Height}", viewportWidth, viewportHeight);
        _currentLayout = _layoutEngine.Layout(root, _styleSheets, viewportWidth, viewportHeight);
        _renderEngine.Render(_currentLayout);
    }

    public void Update(SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        _renderEngine.SetCanvas(canvas);

        if (_dirtyManager.HasDirtyRegions())
        {
            var dirtyRegions = _dirtyManager.GetDirtyRegions();
            _logger.LogDebug("Incremental update, {Count} dirty regions", dirtyRegions.Count);
            var oldLayout = _currentLayout;
            _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight);
            RestoreScrollState(oldLayout, _currentLayout);
            _renderEngine.RenderDirty(_currentLayout, dirtyRegions);
        }
    }

    public void Render(SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        _renderEngine.SetCanvas(canvas);
        var oldLayout = _currentLayout;
        _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight);
        RestoreScrollState(oldLayout, _currentLayout);
        _renderEngine.Render(_currentLayout);
        _dirtyManager.Clear();
    }

    /// <summary>
    /// 标记元素为脏（需要重绘）
    /// </summary>
    public void InvalidateElement(Element element)
    {
        _dirtyManager.MarkDirty(element);
    }

    /// <summary>
    /// 设置视口大小
    /// </summary>
    public void SetViewportSize(float width, float height)
    {
        if (Math.Abs(_viewportWidth - width) > 0.01f || Math.Abs(_viewportHeight - height) > 0.01f)
        {
            _viewportWidth = width;
            _viewportHeight = height;

            // 视口变化需要完整重新布局
            if (_root != null)
            {
                InvalidateElement(_root);
            }
        }
    }

    /// <summary>
    /// 添加样式表
    /// </summary>
    public void AddStyleSheet(StyleSheet styleSheet)
    {
        _styleSheets.Add(styleSheet);

        // 样式变化需要重新布局
        if (_root != null)
        {
            InvalidateElement(_root);
        }
    }

    /// <summary>
    /// 获取当前布局树
    /// </summary>
    public LayoutBox? GetCurrentLayout() => _currentLayout;

    /// <summary>
    /// 获取根元素
    /// </summary>
    public Element? GetRoot() => _root;

    /// <summary>
    /// 在指定坐标处进行命中测试，返回最深层的元素
    /// </summary>
    public Element? HitTest(float x, float y)
    {
        if (_currentLayout == null) return null;
        return HitTestBox(_currentLayout, x, y);
    }

    private Element? HitTestBox(LayoutBox box, float x, float y, float scrollOffsetX = 0, float scrollOffsetY = 0)
    {
        var rect = box.BoxModel.BorderBox;
        float adjustedLeft = rect.Left - scrollOffsetX;
        float adjustedRight = rect.Right - scrollOffsetX;
        float adjustedTop = rect.Top - scrollOffsetY;
        float adjustedBottom = rect.Bottom - scrollOffsetY;

        if (x < adjustedLeft || x > adjustedRight || y < adjustedTop || y > adjustedBottom)
            return null;

        float childScrollOffsetX = scrollOffsetX + box.ScrollLeft;
        float childScrollOffsetY = scrollOffsetY + box.ScrollTop;

        for (int i = box.Children.Count - 1; i >= 0; i--)
        {
            var child = box.Children[i];
            var childRect = child.BoxModel.BorderBox;
            float childScreenTop = childRect.Top - childScrollOffsetY;
            float childScreenBottom = childRect.Bottom - childScrollOffsetY;
            float childScreenLeft = childRect.Left - childScrollOffsetX;
            float childScreenRight = childRect.Right - childScrollOffsetX;

            bool isClipped = (box.ScrollTop > 0 || box.ScrollLeft > 0 ||
                              box.ComputedStyle.OverflowY != Overflow.Visible ||
                              box.ComputedStyle.OverflowX != Overflow.Visible) &&
                             (childScreenBottom < adjustedTop || childScreenTop > adjustedBottom ||
                              childScreenRight < adjustedLeft || childScreenLeft > adjustedRight);

            if (isClipped) continue;

            var hit = HitTestBox(child, x, y, childScrollOffsetX, childScrollOffsetY);
            if (hit != null) return hit;
        }

        return box.Element;
    }

    private static void EnsureParentReferences(Element element)
    {
        foreach (var child in element.Children)
        {
            child.SetParent(element);
            EnsureParentReferences(child);
        }
    }

    /// <summary>
    /// 将旧布局树的滚动状态恢复到新布局树
    /// </summary>
    private static void RestoreScrollState(LayoutBox? oldRoot, LayoutBox? newRoot)
    {
        if (oldRoot == null || newRoot == null) return;
        RestoreScrollStateRecursive(oldRoot, newRoot);
    }

    private static void RestoreScrollStateRecursive(LayoutBox oldBox, LayoutBox newBox)
    {
        if (oldBox.Element == newBox.Element)
        {
            newBox.ScrollTop = oldBox.ScrollTop;
            newBox.ScrollLeft = oldBox.ScrollLeft;
        }

        foreach (var newChild in newBox.Children)
        {
            foreach (var oldChild in oldBox.Children)
            {
                if (oldChild.Element == newChild.Element)
                {
                    RestoreScrollStateRecursive(oldChild, newChild);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 处理滚动事件，更新滚动位置
    /// </summary>
    public bool ScrollBy(float x, float y, float deltaX, float deltaY)
    {
        if (_currentLayout == null)
        {
            _logger.LogTrace("ScrollBy: no layout available");
            return false;
        }

        var targetElement = HitTest(x, y);
        if (targetElement == null)
        {
            _logger.LogTrace("ScrollBy: no element at ({X}, {Y})", x, y);
            return false;
        }

        _logger.LogTrace("ScrollBy: hit element <{Tag} id=\"{Id}\" class=\"{Class}\"> at ({X}, {Y}), delta=({DeltaX}, {DeltaY})",
            targetElement.TagName, targetElement.Id, targetElement.Class, x, y, deltaX, deltaY);

        var scrollableBox = FindScrollableBox(targetElement, deltaX, deltaY);
        if (scrollableBox == null)
        {
            _logger.LogTrace("ScrollBy: no scrollable ancestor found for <{Tag} id=\"{Id}\" class=\"{Class}\">",
                targetElement.TagName, targetElement.Id, targetElement.Class);
            return false;
        }

        _logger.LogTrace("ScrollBy: found scrollable <{Tag} id=\"{Id}\" class=\"{Class}\">, overflowY={OverflowY}, scrollableHeight={ScrollableH}, paddingBoxHeight={PaddingH}",
            scrollableBox.Element.TagName, scrollableBox.Element.Id, scrollableBox.Element.Class,
            scrollableBox.ComputedStyle.OverflowY, scrollableBox.ScrollableContentHeight, scrollableBox.BoxModel.PaddingBox.Height);

        float oldScrollLeft = scrollableBox.ScrollLeft;
        float oldScrollTop = scrollableBox.ScrollTop;

        // 更新垂直滚动
        if (Math.Abs(deltaY) > 0.01f && scrollableBox.HasVerticalScrollbar)
        {
            float maxScrollTop = scrollableBox.ScrollableContentHeight - scrollableBox.BoxModel.PaddingBox.Height
                + (scrollableBox.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            maxScrollTop = Math.Max(0, maxScrollTop);
            scrollableBox.ScrollTop = Math.Clamp(scrollableBox.ScrollTop + deltaY, 0, maxScrollTop);
            _logger.LogTrace("ScrollBy: vertical scroll {Old} -> {New} (max={Max})", oldScrollTop, scrollableBox.ScrollTop, maxScrollTop);
        }

        // 更新水平滚动
        if (Math.Abs(deltaX) > 0.01f && scrollableBox.HasHorizontalScrollbar)
        {
            float maxScrollLeft = scrollableBox.ScrollableContentWidth - scrollableBox.BoxModel.PaddingBox.Width
                + (scrollableBox.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            maxScrollLeft = Math.Max(0, maxScrollLeft);
            scrollableBox.ScrollLeft = Math.Clamp(scrollableBox.ScrollLeft + deltaX, 0, maxScrollLeft);
            _logger.LogTrace("ScrollBy: horizontal scroll {Old} -> {New} (max={Max})", oldScrollLeft, scrollableBox.ScrollLeft, maxScrollLeft);
        }

        bool scrolled = Math.Abs(scrollableBox.ScrollLeft - oldScrollLeft) > 0.01f ||
                        Math.Abs(scrollableBox.ScrollTop - oldScrollTop) > 0.01f;

        if (scrolled)
        {
            // 分发滚动事件
            var scrollArgs = new ScrollEventArgs
            {
                Target = scrollableBox.Element,
                DeltaX = scrollableBox.ScrollLeft - oldScrollLeft,
                DeltaY = scrollableBox.ScrollTop - oldScrollTop,
                ScrollLeft = scrollableBox.ScrollLeft,
                ScrollTop = scrollableBox.ScrollTop,
                Bubbles = true
            };
            _eventDispatcher.Dispatch(scrollableBox.Element, EventTypes.Scroll, scrollArgs);

            InvalidateElement(scrollableBox.Element);
            _logger.LogTrace("ScrollBy: scrolled, new position=({ScrollLeft}, {ScrollTop})", scrollableBox.ScrollLeft, scrollableBox.ScrollTop);
        }
        else
        {
            _logger.LogTrace("ScrollBy: no actual scroll change (already at boundary)");
        }

        return scrolled;
    }

    /// <summary>
    /// 从目标元素向上查找最近的可滚动容器
    /// </summary>
    private LayoutBox? FindScrollableBox(Element target, float deltaX, float deltaY)
    {
        if (_currentLayout == null) return null;

        var current = target;
        while (current != null)
        {
            var box = FindLayoutBoxForElement(_currentLayout, current);
            if (box != null)
            {
                bool canScrollY = Math.Abs(deltaY) > 0.01f &&
                    (box.ComputedStyle.OverflowY == Overflow.Auto || box.ComputedStyle.OverflowY == Overflow.Scroll) &&
                    box.ScrollableContentHeight > box.BoxModel.PaddingBox.Height;

                bool canScrollX = Math.Abs(deltaX) > 0.01f &&
                    (box.ComputedStyle.OverflowX == Overflow.Auto || box.ComputedStyle.OverflowX == Overflow.Scroll) &&
                    box.ScrollableContentWidth > box.BoxModel.PaddingBox.Width;

                if (canScrollY || canScrollX)
                    return box;
            }
            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// 在布局树中查找对应元素的 LayoutBox
    /// </summary>
    private static LayoutBox? FindLayoutBoxForElement(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;

        foreach (var child in box.Children)
        {
            var found = FindLayoutBoxForElement(child, element);
            if (found != null) return found;
        }

        return null;
    }
}
