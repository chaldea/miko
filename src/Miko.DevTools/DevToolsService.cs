using System.Text.Json;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Layout;

namespace Miko.DevTools;

/// <summary>
/// DevTools 服务实现。通过 <see cref="IDomAccessor"/> 在渲染线程上安全地检查/操作
/// 运行中的 <see cref="MikoEngine"/> DOM，向 MCP 等外部工具暴露调试能力。
/// </summary>
public sealed class DevToolsService : IDevToolsService
{
    private readonly IDomAccessor _dom;
    private readonly Func<IReadOnlyList<LogEntryInfo>>? _logSource;

    public DevToolsService(IDomAccessor dom, Func<IReadOnlyList<LogEntryInfo>>? logSource = null)
    {
        _dom = dom;
        _logSource = logSource;
    }

    public string GetDomTree()
    {
        return _dom.Invoke(engine =>
        {
            var root = engine.GetRoot();
            if (root == null) return "{}";
            var node = SerializeElement(root);
            return JsonSerializer.Serialize(node, JsonOpts);
        });
    }

    public ElementInfo? FindElementById(string id)
    {
        return _dom.Invoke(engine =>
        {
            var root = engine.GetRoot();
            if (root == null) return null;
            var element = FindElement(root, e => MatchesId(e, id));
            return element != null ? ToElementInfo(element) : null;
        });
    }

    public IReadOnlyList<ElementInfo> QueryElements(string selector)
    {
        return _dom.Invoke<IReadOnlyList<ElementInfo>>(engine =>
        {
            var root = engine.GetRoot();
            if (root == null) return Array.Empty<ElementInfo>();
            var results = new List<ElementInfo>();
            CollectElements(root, selector, results);
            return results;
        });
    }

    public string? GetComputedStyle(string elementId)
    {
        return _dom.Invoke(engine =>
        {
            var layoutRoot = engine.GetCurrentLayout();
            if (layoutRoot == null) return null;
            var box = FindLayoutBoxById(layoutRoot, elementId);
            var computed = box?.ComputedStyle;
            if (computed == null) return null;
            return JsonSerializer.Serialize(computed, JsonOpts);
        });
    }

    public bool ClickElement(string elementId)
    {
        return _dom.Invoke(engine =>
        {
            var root = engine.GetRoot();
            if (root == null) return false;
            var element = FindElement(root, e => MatchesId(e, elementId));
            if (element == null) return false;

            // 尽量以元素中心作为点击坐标（若已布局），使命中/坐标相关的处理器行为合理。
            float x = 0, y = 0;
            var layoutRoot = engine.GetCurrentLayout();
            var box = layoutRoot != null ? FindLayoutBox(layoutRoot, element) : null;
            if (box != null)
            {
                var content = box.BoxModel.BorderBox;
                x = content.X + content.Width / 2f;
                y = content.Y + content.Height / 2f;
            }

            var args = new MouseEventArgs
            {
                Target = element,
                X = x,
                Y = y,
                Button = MouseButton.Left,
                Bubbles = true
            };
            new EventDispatcher().Dispatch(element, EventTypes.Click, args);
            engine.InvalidateElement(element);
            return true;
        });
    }

    public BoxModelInfo? GetBoxModel(string elementId)
    {
        return _dom.Invoke(engine =>
        {
            var layoutRoot = engine.GetCurrentLayout();
            if (layoutRoot == null) return null;
            var box = FindLayoutBoxById(layoutRoot, elementId);
            if (box == null) return null;

            var m = box.BoxModel;
            return new BoxModelInfo(
                m.BorderBox.X, m.BorderBox.Y, m.BorderBox.Width, m.BorderBox.Height,
                m.Content.Width, m.Content.Height,
                m.Padding.Top, m.Padding.Right, m.Padding.Bottom, m.Padding.Left,
                m.Border.Top, m.Border.Right, m.Border.Bottom, m.Border.Left,
                m.Margin.Top, m.Margin.Right, m.Margin.Bottom, m.Margin.Left);
        });
    }

    public IReadOnlyList<LogEntryInfo> GetLogs(int maxCount = 100)
    {
        if (_logSource == null) return Array.Empty<LogEntryInfo>();
        var all = _logSource();
        return maxCount > 0 && all.Count > maxCount
            ? all.Skip(all.Count - maxCount).ToList()
            : all;
    }

    // ---------------------------------------------------------------------
    // 内部辅助
    // ---------------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private static bool MatchesId(Element e, string id) => e.Id == id || GenerateId(e) == id;

    private static string GenerateId(Element element) => $"elem_{element.GetHashCode():X8}";

    private static object SerializeElement(Element element) => new
    {
        id = element.Id ?? GenerateId(element),
        tagName = element.TagName,
        @class = element.Class,
        textContent = string.IsNullOrEmpty(element.TextContent) ? null : element.TextContent,
        children = element.Children
            .Where(c => c is not TextNode)
            .Select(SerializeElement)
            .ToList()
    };

    private static void CollectElements(Element element, string selector, List<ElementInfo> results)
    {
        if (MatchesSelector(element, selector))
            results.Add(ToElementInfo(element));

        foreach (var child in element.Children.Where(c => c is not TextNode))
            CollectElements(child, selector, results);
    }

    private static bool MatchesSelector(Element element, string selector)
    {
        selector = selector.Trim();
        if (selector.StartsWith('#'))
            return element.Id == selector[1..];
        if (selector.StartsWith('.'))
            return element.Class?.Split(' ').Contains(selector[1..]) ?? false;
        return element.TagName.Equals(selector, StringComparison.OrdinalIgnoreCase);
    }

    private static Element? FindElement(Element root, Func<Element, bool> predicate)
    {
        if (predicate(root)) return root;
        foreach (var child in root.Children.Where(c => c is not TextNode))
        {
            var found = FindElement(child, predicate);
            if (found != null) return found;
        }
        return null;
    }

    private static LayoutBox? FindLayoutBox(LayoutBox root, Element element)
    {
        if (root.Element == element) return root;
        foreach (var child in root.Children)
        {
            var found = FindLayoutBox(child, element);
            if (found != null) return found;
        }
        return null;
    }

    private static LayoutBox? FindLayoutBoxById(LayoutBox root, string elementId)
    {
        if (MatchesId(root.Element, elementId)) return root;
        foreach (var child in root.Children)
        {
            var found = FindLayoutBoxById(child, elementId);
            if (found != null) return found;
        }
        return null;
    }

    private static ElementInfo ToElementInfo(Element element) => new(
        element.Id ?? GenerateId(element),
        element.TagName,
        element.Class,
        string.IsNullOrEmpty(element.TextContent) ? null : element.TextContent,
        element.Children.Count(c => c is not TextNode));
}
