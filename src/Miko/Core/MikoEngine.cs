using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Core;
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
            _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight);
            _renderEngine.RenderDirty(_currentLayout, dirtyRegions);
        }
    }

    public void Render(SKCanvas canvas)
    {
        if (_root == null) throw new InvalidOperationException("Engine not initialized. Call Initialize first.");

        _renderEngine.SetCanvas(canvas);
        _logger.LogDebug("Full render pass");
        _currentLayout = _layoutEngine.Layout(_root, _styleSheets, _viewportWidth, _viewportHeight);
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
}
