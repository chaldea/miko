using Miko.Core;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Core;

/// <summary>
/// Miko 渲染引擎
/// </summary>
public class MikoEngine
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();
    private readonly DirtyRegionManager _dirtyManager = new();
    private List<StyleSheet> _styleSheets = new();

    private Element? _root;
    private LayoutBox? _currentLayout;
    private float _viewportWidth;
    private float _viewportHeight;

    /// <summary>
    /// 初始化引擎
    /// </summary>
    public void Initialize(Element root, List<StyleSheet> styleSheets, SKCanvas canvas, float viewportWidth, float viewportHeight)
    {
        _root = root;
        _styleSheets = styleSheets;
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        _renderEngine.SetCanvas(canvas);

        // 首次完整渲染
        _currentLayout = _layoutEngine.Layout(root, _styleSheets, viewportWidth, viewportHeight);
        _renderEngine.Render(_currentLayout);
    }

    /// <summary>
    /// 更新渲染
    /// </summary>
    public void Update(SKCanvas canvas)
    {
        if (_root == null)
        {
            throw new InvalidOperationException("Engine not initialized. Call Initialize first.");
        }

        _renderEngine.SetCanvas(canvas);

        // 增量更新
        if (_dirtyManager.HasDirtyRegions())
        {
            // 重新布局
            _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight);

            // 只重绘脏区域
            var dirtyRegions = _dirtyManager.GetDirtyRegions();
            _renderEngine.RenderDirty(_currentLayout, dirtyRegions);
        }
    }

    /// <summary>
    /// 完整重新渲染
    /// </summary>
    public void Render(SKCanvas canvas)
    {
        if (_root == null)
        {
            throw new InvalidOperationException("Engine not initialized. Call Initialize first.");
        }

        _renderEngine.SetCanvas(canvas);

        // 重新布局
        _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight);

        // 完整渲染
        _renderEngine.Render(_currentLayout);

        // 清空脏区域
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
}
