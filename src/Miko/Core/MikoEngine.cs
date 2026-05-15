using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Animation;
using Miko.Common;
using Miko.Events;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Core;

public class MikoEngine
{
    private readonly LayoutEngine _layoutEngine;
    private readonly RenderEngine _renderEngine;
    private readonly DirtyRegionManager _dirtyManager;
    private readonly EventDispatcher _eventDispatcher;
    private readonly AnimationManager _animationManager;
    private List<StyleSheet> _styleSheets = new();
    private ILogger _logger = NullLogger.Instance;

    public MikoEngine(
        LayoutEngine layoutEngine,
        RenderEngine renderEngine,
        DirtyRegionManager dirtyManager,
        EventDispatcher eventDispatcher,
        AnimationManager animationManager,
        ILogger<MikoEngine>? logger = null)
    {
        _layoutEngine = layoutEngine;
        _renderEngine = renderEngine;
        _dirtyManager = dirtyManager;
        _eventDispatcher = eventDispatcher;
        _animationManager = animationManager;
        if (logger != null) _logger = logger;
    }

    public MikoEngine() : this(new(), new(), new(), new(), new()) { }

    public void SetLogger(ILogger logger)
    {
        _logger = logger;
        _animationManager.SetLogger(logger);
    }

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

        _animationManager.Clear();
        EnsureParentReferences(root);
        _renderEngine.SetCanvas(canvas);

        _logger.LogInformation("Engine initialized with viewport {Width}x{Height}", viewportWidth, viewportHeight);
        _currentLayout = _layoutEngine.Layout(root, _styleSheets, viewportWidth, viewportHeight);
        _renderEngine.Render(_currentLayout);

        ScanAndStartAnimations(root);
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

    public AnimationManager AnimationManager => _animationManager;

    public void RegisterAnimation(KeyframeAnimation animation)
    {
        _logger.LogDebug("MikoEngine.RegisterAnimation: \"{Name}\"", animation.Name);
        _animationManager.RegisterAnimation(animation);
    }

    public void StartAnimation(Element element, string animationName)
    {
        _logger.LogDebug("MikoEngine.StartAnimation: \"{Name}\" on <{Tag} id=\"{Id}\">",
            animationName, element.TagName, element.Id ?? "");
        _animationManager.StartAnimation(element, animationName);
    }

    public void StartAnimation(Element element, KeyframeAnimation animation)
    {
        _logger.LogDebug("MikoEngine.StartAnimation: \"{Name}\" on <{Tag} id=\"{Id}\">",
            animation.Name, element.TagName, element.Id ?? "");
        _animationManager.StartAnimation(element, animation);
    }

    public void StopAnimation(Element element, string? animationName = null)
    {
        _logger.LogDebug("MikoEngine.StopAnimation: \"{Name}\" on <{Tag} id=\"{Id}\">",
            animationName ?? "(all)", element.TagName, element.Id ?? "");
        _animationManager.StopAnimation(element, animationName);
    }

    public void Tick(float deltaTime, SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        if (!_animationManager.HasActiveAnimations && !_dirtyManager.HasDirtyRegions())
        {
            _logger.LogTrace("Tick: no active animations or dirty regions, skipping");
            return;
        }

        _logger.LogTrace("Tick: deltaTime={DeltaTime}s, activeAnimations={HasAnim}, dirtyRegions={HasDirty}",
            deltaTime, _animationManager.HasActiveAnimations, _dirtyManager.HasDirtyRegions());
        _animationManager.Update(deltaTime);
        Render(canvas);
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

    private void ScanAndStartAnimations(Element element)
    {
        if (element.Style?.Animations != null)
        {
            foreach (var animation in element.Style.Animations)
            {
                _logger.LogDebug("ScanAndStartAnimations: found animation \"{Name}\" on <{Tag} id=\"{Id}\">",
                    animation.Name, element.TagName, element.Id ?? "");
                _animationManager.StartAnimation(element, animation);
            }
        }

        foreach (var child in element.Children)
        {
            ScanAndStartAnimations(child);
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

    public enum ScrollbarHitType { None, VerticalThumb, VerticalTrack, HorizontalThumb, HorizontalTrack }

    public record ScrollbarHitResult(LayoutBox Box, ScrollbarHitType HitType, float ThumbOffset);

    /// <summary>
    /// 检测鼠标是否点击在某个滚动条上
    /// </summary>
    public ScrollbarHitResult? HitTestScrollbar(float x, float y)
    {
        if (_currentLayout == null) return null;
        return HitTestScrollbarBox(_currentLayout, x, y, 0, 0);
    }

    private static ScrollbarHitResult? HitTestScrollbarBox(LayoutBox box, float x, float y, float scrollOffsetX, float scrollOffsetY)
    {
        var borderBox = box.BoxModel.BorderBox;
        float adjustedLeft = borderBox.Left - scrollOffsetX;
        float adjustedRight = borderBox.Right - scrollOffsetX;
        float adjustedTop = borderBox.Top - scrollOffsetY;
        float adjustedBottom = borderBox.Bottom - scrollOffsetY;

        if (x < adjustedLeft || x > adjustedRight || y < adjustedTop || y > adjustedBottom)
            return null;

        var paddingBox = box.BoxModel.PaddingBox;
        float pLeft = paddingBox.X - scrollOffsetX;
        float pTop = paddingBox.Y - scrollOffsetY;
        float pRight = pLeft + paddingBox.Width;
        float pBottom = pTop + paddingBox.Height;

        if (box.HasVerticalScrollbar)
        {
            float trackX = pRight - LayoutBox.ScrollbarThickness;
            float trackHeight = paddingBox.Height - (box.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);

            if (x >= trackX && x <= pRight && y >= pTop && y <= pTop + trackHeight)
            {
                var (thumbTop, thumbHeight) = GetVerticalThumbGeometry(box, trackHeight);
                float screenThumbTop = thumbTop - scrollOffsetY;
                float screenThumbBottom = screenThumbTop + thumbHeight;
                if (y >= screenThumbTop && y <= screenThumbBottom)
                    return new ScrollbarHitResult(box, ScrollbarHitType.VerticalThumb, y - screenThumbTop);
                return new ScrollbarHitResult(box, ScrollbarHitType.VerticalTrack, 0);
            }
        }

        if (box.HasHorizontalScrollbar)
        {
            float trackY = pBottom - LayoutBox.ScrollbarThickness;
            float trackWidth = paddingBox.Width - (box.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);

            if (x >= pLeft && x <= pLeft + trackWidth && y >= trackY && y <= pBottom)
            {
                var (thumbLeft, thumbWidth) = GetHorizontalThumbGeometry(box, trackWidth);
                float screenThumbLeft = thumbLeft - scrollOffsetX;
                float screenThumbRight = screenThumbLeft + thumbWidth;
                if (x >= screenThumbLeft && x <= screenThumbRight)
                    return new ScrollbarHitResult(box, ScrollbarHitType.HorizontalThumb, x - screenThumbLeft);
                return new ScrollbarHitResult(box, ScrollbarHitType.HorizontalTrack, 0);
            }
        }

        float childScrollOffsetX = scrollOffsetX + box.ScrollLeft;
        float childScrollOffsetY = scrollOffsetY + box.ScrollTop;

        foreach (var child in box.Children)
        {
            var hit = HitTestScrollbarBox(child, x, y, childScrollOffsetX, childScrollOffsetY);
            if (hit != null) return hit;
        }

        return null;
    }

    private static (float thumbTop, float thumbHeight) GetVerticalThumbGeometry(LayoutBox box, float trackHeight)
    {
        float viewportH = box.BoxModel.PaddingBox.Height;
        float contentH = box.ScrollableContentHeight;
        float thumbHeight = Math.Max(trackHeight * (viewportH / contentH), 20f);
        float scrollableTrack = trackHeight - thumbHeight;
        float maxScroll = contentH - viewportH;
        float thumbTop = box.BoxModel.PaddingBox.Y + (maxScroll > 0 ? (box.ScrollTop / maxScroll) * scrollableTrack : 0);
        return (thumbTop, thumbHeight);
    }

    private static (float thumbLeft, float thumbWidth) GetHorizontalThumbGeometry(LayoutBox box, float trackWidth)
    {
        float viewportW = box.BoxModel.PaddingBox.Width;
        float contentW = box.ScrollableContentWidth;
        float thumbWidth = Math.Max(trackWidth * (viewportW / contentW), 20f);
        float scrollableTrack = trackWidth - thumbWidth;
        float maxScroll = contentW - viewportW;
        float thumbLeft = box.BoxModel.PaddingBox.X + (maxScroll > 0 ? (box.ScrollLeft / maxScroll) * scrollableTrack : 0);
        return (thumbLeft, thumbWidth);
    }

    /// <summary>
    /// 拖拽垂直滑块到指定鼠标Y位置
    /// </summary>
    public bool DragVerticalThumb(LayoutBox box, float mouseY, float thumbOffset)
    {
        var current = FindLayoutBoxForElement(_currentLayout!, box.Element);
        if (current == null) return false;

        float trackHeight = current.BoxModel.PaddingBox.Height - (current.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);
        var (_, thumbHeight) = GetVerticalThumbGeometry(current, trackHeight);
        float scrollableTrack = trackHeight - thumbHeight;
        if (scrollableTrack <= 0) return false;

        float thumbTop = mouseY - thumbOffset - current.BoxModel.PaddingBox.Y;
        float ratio = Math.Clamp(thumbTop / scrollableTrack, 0f, 1f);

        float viewportH = current.BoxModel.PaddingBox.Height;
        float maxScroll = Math.Max(0, current.ScrollableContentHeight - viewportH);
        float newScrollTop = ratio * maxScroll;

        if (Math.Abs(current.ScrollTop - newScrollTop) < 0.01f) return false;
        current.ScrollTop = newScrollTop;
        InvalidateElement(current.Element);
        return true;
    }

    public bool DragHorizontalThumb(LayoutBox box, float mouseX, float thumbOffset)
    {
        var current = FindLayoutBoxForElement(_currentLayout!, box.Element);
        if (current == null) return false;

        float trackWidth = current.BoxModel.PaddingBox.Width - (current.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);
        var (_, thumbWidth) = GetHorizontalThumbGeometry(current, trackWidth);
        float scrollableTrack = trackWidth - thumbWidth;
        if (scrollableTrack <= 0) return false;

        float thumbLeft = mouseX - thumbOffset - current.BoxModel.PaddingBox.X;
        float ratio = Math.Clamp(thumbLeft / scrollableTrack, 0f, 1f);

        float viewportW = current.BoxModel.PaddingBox.Width;
        float maxScroll = Math.Max(0, current.ScrollableContentWidth - viewportW);
        float newScrollLeft = ratio * maxScroll;

        if (Math.Abs(current.ScrollLeft - newScrollLeft) < 0.01f) return false;
        current.ScrollLeft = newScrollLeft;
        InvalidateElement(current.Element);
        return true;
    }

    /// <summary>
    /// 点击滑轨，按页滚动（≈ clientSize * 0.875）
    /// </summary>
    public bool ScrollTrackClick(LayoutBox box, ScrollbarHitType hitType, float mouseX, float mouseY)
    {
        var current = FindLayoutBoxForElement(_currentLayout!, box.Element);
        if (current == null) return false;

        if (hitType == ScrollbarHitType.VerticalTrack)
        {
            float viewportH = current.BoxModel.PaddingBox.Height;
            float pageSize = viewportH * 0.875f;
            float trackHeight = viewportH - (current.HasHorizontalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var (thumbTop, _) = GetVerticalThumbGeometry(current, trackHeight);
            float delta = mouseY < thumbTop ? -pageSize : pageSize;

            float maxScroll = Math.Max(0, current.ScrollableContentHeight - viewportH);
            float newScrollTop = Math.Clamp(current.ScrollTop + delta, 0, maxScroll);
            if (Math.Abs(current.ScrollTop - newScrollTop) < 0.01f) return false;
            current.ScrollTop = newScrollTop;
            InvalidateElement(current.Element);
            return true;
        }

        if (hitType == ScrollbarHitType.HorizontalTrack)
        {
            float viewportW = current.BoxModel.PaddingBox.Width;
            float pageSize = viewportW * 0.875f;
            float trackWidth = viewportW - (current.HasVerticalScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var (thumbLeft, _) = GetHorizontalThumbGeometry(current, trackWidth);
            float delta = mouseX < thumbLeft ? -pageSize : pageSize;

            float maxScroll = Math.Max(0, current.ScrollableContentWidth - viewportW);
            float newScrollLeft = Math.Clamp(current.ScrollLeft + delta, 0, maxScroll);
            if (Math.Abs(current.ScrollLeft - newScrollLeft) < 0.01f) return false;
            current.ScrollLeft = newScrollLeft;
            InvalidateElement(current.Element);
            return true;
        }

        return false;
    }
}
