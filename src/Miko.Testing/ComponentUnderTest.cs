using Miko.Core;
using Miko.Layout;
using Miko.Styling;

namespace Miko.Testing;

/// <summary>
/// Represents a rendered component under test, providing access to DOM structure,
/// computed styles, layout information, and element queries.
/// </summary>
public class ComponentUnderTest
{
    /// <summary>
    /// The root element of the rendered component.
    /// </summary>
    public Element Root { get; }

    /// <summary>
    /// The layout tree built from the component's DOM.
    /// </summary>
    public LayoutBox Layout { get; }

    /// <summary>
    /// The computed styles for all elements in the component tree.
    /// </summary>
    public Dictionary<Element, ComputedStyle> ComputedStyles { get; }

    /// <summary>
    /// The viewport width used for rendering.
    /// </summary>
    public float ViewportWidth { get; }

    /// <summary>
    /// The viewport height used for rendering.
    /// </summary>
    public float ViewportHeight { get; }

    internal ComponentUnderTest(
        Element root,
        LayoutBox layout,
        Dictionary<Element, ComputedStyle> computedStyles,
        float viewportWidth,
        float viewportHeight)
    {
        Root = root;
        Layout = layout;
        ComputedStyles = computedStyles;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    /// <summary>
    /// Finds an element by its ID.
    /// </summary>
    public Element? FindById(string id) => Root.FindById(id);

    /// <summary>
    /// Finds all elements with the specified class name.
    /// </summary>
    public List<Element> FindByClass(string className) => Root.FindByClass(className);

    /// <summary>
    /// Finds all elements with the specified tag name.
    /// </summary>
    public List<Element> FindByTagName(string tagName) => Root.FindByTagName(tagName);

    /// <summary>
    /// Gets the computed style for the specified element.
    /// </summary>
    public ComputedStyle? GetComputedStyle(Element element)
    {
        return ComputedStyles.TryGetValue(element, out var style) ? style : null;
    }

    /// <summary>
    /// Gets the layout box for the specified element.
    /// </summary>
    public LayoutBox? FindLayoutBox(Element element)
    {
        return FindLayoutBoxRecursive(Layout, element);
    }

    private LayoutBox? FindLayoutBoxRecursive(LayoutBox box, Element target)
    {
        if (box.Element == target)
            return box;

        foreach (var child in box.Children)
        {
            var result = FindLayoutBoxRecursive(child, target);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the box model (content, padding, border, margin boxes) for the specified element.
    /// </summary>
    public BoxModel? GetBoxModel(Element element)
    {
        var layoutBox = FindLayoutBox(element);
        return layoutBox?.BoxModel;
    }

    /// <summary>
    /// Gets all elements in the component tree using depth-first traversal.
    /// </summary>
    public List<Element> GetAllElements()
    {
        var elements = new List<Element>();
        CollectElements(Root, elements);
        return elements;
    }

    private void CollectElements(Element element, List<Element> result)
    {
        result.Add(element);
        foreach (var child in element.Children)
        {
            CollectElements(child, result);
        }
    }

    /// <summary>
    /// Gets the text content of the root element and all its descendants.
    /// </summary>
    public string GetTextContent()
    {
        return GetTextContentRecursive(Root);
    }

    private string GetTextContentRecursive(Element element)
    {
        var text = element.TextContent ?? "";
        foreach (var child in element.Children)
        {
            text += GetTextContentRecursive(child);
        }
        return text;
    }
}
