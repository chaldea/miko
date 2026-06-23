using Miko.Components;
using Miko.Core;
using Miko.Layout;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Testing;

/// <summary>
/// Test context for rendering and testing Miko Razor components.
/// Inspired by bUnit's test context.
/// </summary>
public class TestContext : IDisposable
{
    private readonly List<StyleSheet> _styleSheets = new();
    private readonly float _defaultViewportWidth = 800f;
    private readonly float _defaultViewportHeight = 600f;
    private SKBitmap? _testBitmap;
    private SKCanvas? _testCanvas;

    /// <summary>
    /// Gets or sets the default viewport width for rendering components.
    /// </summary>
    public float ViewportWidth { get; set; }

    /// <summary>
    /// Gets or sets the default viewport height for rendering components.
    /// </summary>
    public float ViewportHeight { get; set; }

    /// <summary>
    /// Gets the list of stylesheets that will be applied to rendered components.
    /// </summary>
    public List<StyleSheet> StyleSheets => _styleSheets;

    public TestContext()
    {
        ViewportWidth = _defaultViewportWidth;
        ViewportHeight = _defaultViewportHeight;
    }

    /// <summary>
    /// Renders a component and returns a ComponentUnderTest for assertions.
    /// </summary>
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <param name="parameters">Optional parameters to pass to the component.</param>
    /// <returns>A ComponentUnderTest instance with DOM, layout, and style information.</returns>
    public ComponentUnderTest Render<TComponent>(Action<ComponentParameterBuilder<TComponent>>? parameters = null)
        where TComponent : ComponentBase, new()
    {
        var component = new TComponent();

        // Apply parameters if provided
        if (parameters != null)
        {
            var builder = new ComponentParameterBuilder<TComponent>(component);
            parameters(builder);
            builder.ApplyParameters();
        }

        // Build the component
        var rootElement = component.Build();

        return RenderElement(rootElement);
    }

    /// <summary>
    /// Renders an element tree and returns a ComponentUnderTest for assertions.
    /// </summary>
    public ComponentUnderTest RenderElement(Element rootElement)
    {
        // Create a test canvas if needed
        EnsureTestCanvas();

        // Create layout engine
        var layoutEngine = new LayoutEngine();

        // Perform layout (this computes styles, builds layout tree, and calculates layout)
        var layoutTree = layoutEngine.Layout(
            rootElement,
            _styleSheets,
            ViewportWidth,
            ViewportHeight);

        // Collect computed styles from the layout tree
        var computedStyles = new Dictionary<Element, ComputedStyle>();
        CollectComputedStyles(layoutTree, computedStyles);

        return new ComponentUnderTest(
            rootElement,
            layoutTree,
            computedStyles,
            ViewportWidth,
            ViewportHeight);
    }

    private void CollectComputedStyles(LayoutBox box, Dictionary<Element, ComputedStyle> styles)
    {
        styles[box.Element] = box.ComputedStyle;
        foreach (var child in box.Children)
        {
            CollectComputedStyles(child, styles);
        }
    }

    /// <summary>
    /// Adds a stylesheet to be applied to all rendered components.
    /// </summary>
    public void AddStyleSheet(StyleSheet styleSheet)
    {
        _styleSheets.Add(styleSheet);
    }

    private void EnsureTestCanvas()
    {
        if (_testCanvas == null)
        {
            _testBitmap = new SKBitmap((int)ViewportWidth, (int)ViewportHeight);
            _testCanvas = new SKCanvas(_testBitmap);
        }
    }

    public void Dispose()
    {
        _testCanvas?.Dispose();
        _testBitmap?.Dispose();
    }
}
