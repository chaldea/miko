using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.DevTools.Panels;

internal static class DomTreeBuilder
{
    // 折叠状态按元素记录。用弱引用表（而非 HashSet<Element>）：主程序的 DOM 会在每次
    // Razor 重渲染中被整体替换，强引用会让所有曾折叠过的旧元素永久无法回收——大页面反复
    // 导航时这是一处真实泄漏（见 ISSUE-117）。ConditionalWeakTable 在元素不可达后自动清理。
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Element, object> _collapsedElements = new();
    private static readonly object CollapsedMarker = new();

    // 折叠状态的版本号，供窗口的重建指纹使用（折叠/展开必须触发一次重建）。
    private static long s_collapseVersion;

    /// <summary>折叠状态版本号：每次折叠/展开递增。</summary>
    internal static long CollapseVersion => Interlocked.Read(ref s_collapseVersion);

    private static bool IsCollapsed(Element element) => _collapsedElements.TryGetValue(element, out _);

    private static void ToggleCollapsed(Element element)
    {
        if (_collapsedElements.TryGetValue(element, out _))
            _collapsedElements.Remove(element);
        else
            _collapsedElements.AddOrUpdate(element, CollapsedMarker);

        Interlocked.Increment(ref s_collapseVersion);
    }

    public static DivElement Build(DevToolsBridge bridge)
    {
        var container = new DivElement { Class = "dom-tree-panel" };

        var root = bridge.MainEngine?.GetRoot();
        if (root == null)
        {
            container.AddChild(new DivElement
            {
                Class = "console-empty",
                TextContent = "No DOM tree available"
            });
            return container;
        }

        BuildTreeNode(container, root, bridge, 0);
        return container;
    }

    private static void BuildTreeNode(DivElement parent, Element element, DevToolsBridge bridge, int depth)
    {
        var node = new DivElement();

        bool isSelected = bridge.SelectedElement == element;
        node.Class = isSelected ? "tree-node tree-node-selected" : "tree-node";
        node.Style = new Styling.Style
        {
            PaddingLeft = Length.Px(depth * 16 + 4)
        };

        // 文本以 TextNode 子节点承载（见 ISSUE-086），但 DevTools 已通过 element.TextContent 预览
        // 单独显示文本，故树的「子元素」仅统计非文本节点，避免文本被重复渲染为 <#text> 行。
        bool hasChildren = HasElementChildren(element);
        bool isCollapsed = IsCollapsed(element);

        var line = new DivElement { Style = new Styling.Style { Display = Display.Flex, FlexDirection = FlexDirection.Row } };

        if (hasChildren)
        {
            var toggle = new SpanElement
            {
                Class = "tree-toggle",
                TextContent = isCollapsed ? "▶" : "▼"
            };
            toggle.OnClick = args =>
            {
                args.StopPropagation();
                ToggleCollapsed(element);
                bridge.MarkDevToolsDirty();
            };
            line.AddChild(toggle);
        }
        else
        {
            line.AddChild(new SpanElement
            {
                Class = "tree-toggle",
                TextContent = " "
            });
        }

        var tag = new SpanElement
        {
            Class = "tree-node-tag",
            TextContent = $"<{element.TagName}"
        };
        line.AddChild(tag);

        if (!string.IsNullOrEmpty(element.Id))
        {
            line.AddChild(new SpanElement { Class = "tree-node-attr", TextContent = $" id=" });
            line.AddChild(new SpanElement { Class = "tree-node-string", TextContent = $"\"{element.Id}\"" });
        }

        if (!string.IsNullOrEmpty(element.Class))
        {
            line.AddChild(new SpanElement { Class = "tree-node-attr", TextContent = $" class=" });
            line.AddChild(new SpanElement { Class = "tree-node-string", TextContent = $"\"{element.Class}\"" });
        }

        line.AddChild(new SpanElement { Class = "tree-node-tag", TextContent = ">" });

        bool hasText = !string.IsNullOrEmpty(element.TextContent);

        // 无子元素且有文本：文本和闭合标签显示在同一行
        if (hasText && !hasChildren)
        {
            line.AddChild(new SpanElement { Class = "tree-node-text", TextContent = TextPreview(element.TextContent!) });
            line.AddChild(new SpanElement { Class = "tree-node-tag", TextContent = $"</{element.TagName}>" });
        }

        node.AddChild(line);

        node.OnClick = _ =>
        {
            bridge.SelectedElement = element;
        };

        parent.AddChild(node);

        if (hasChildren && !isCollapsed)
        {
            // 元素同时拥有文本内容时，将文本作为独立的文本节点行显示在子元素之前
            if (hasText)
            {
                var textNode = new DivElement
                {
                    Class = "tree-node",
                    Style = new Styling.Style { PaddingLeft = Length.Px((depth + 1) * 16 + 4) }
                };
                var textLine = new DivElement
                {
                    Style = new Styling.Style { Display = Display.Flex, FlexDirection = FlexDirection.Row }
                };
                textLine.AddChild(new SpanElement { Class = "tree-toggle", TextContent = " " });
                textLine.AddChild(new SpanElement { Class = "tree-node-text", TextContent = TextPreview(element.TextContent!) });
                textNode.AddChild(textLine);
                parent.AddChild(textNode);
            }

            foreach (var child in element.Children)
            {
                // 文本节点已通过上方 TextContent 预览显示，不再作为独立树节点渲染。
                if (child is Core.DomElements.TextNode) continue;
                BuildTreeNode(parent, child, bridge, depth + 1);
            }

            var closingTag = new DivElement
            {
                Class = "tree-node",
                Style = new Styling.Style { PaddingLeft = Length.Px(depth * 16 + 4) }
            };
            var closingLine = new DivElement
            {
                Style = new Styling.Style { Display = Display.Flex, FlexDirection = FlexDirection.Row }
            };
            closingLine.AddChild(new SpanElement { Class = "tree-toggle", TextContent = " " });
            closingLine.AddChild(new SpanElement
            {
                Class = "tree-node-tag",
                TextContent = $"</{element.TagName}>"
            });
            closingTag.AddChild(closingLine);
            parent.AddChild(closingTag);
        }
    }

    /// <summary>
    /// 是否有非文本子元素。手写循环而非 <c>Children.Any(...)</c>：本方法在每个树节点上调用，
    /// 大页面下 LINQ 的枚举器与闭包分配会直接体现在每次重建的 GC 压力上。
    /// 文本已通过 TextContent 预览单独显示，故不计入（见 ISSUE-086）。
    /// </summary>
    private static bool HasElementChildren(Element element)
    {
        foreach (var child in element.Children)
        {
            if (child is not Core.DomElements.TextNode) return true;
        }
        return false;
    }

    private static string TextPreview(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length > 40 ? trimmed[..40] + "..." : trimmed;
    }
}
