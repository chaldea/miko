using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.DevTools.Panels;

internal static class DomTreeBuilder
{
    private static readonly HashSet<Element> _collapsedElements = new();

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

        bool hasChildren = element.Children.Count > 0;
        bool isCollapsed = _collapsedElements.Contains(element);

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
                if (_collapsedElements.Contains(element))
                    _collapsedElements.Remove(element);
                else
                    _collapsedElements.Add(element);
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

        if (!string.IsNullOrEmpty(element.TextContent) && element.Children.Count == 0)
        {
            var textPreview = element.TextContent.Length > 40
                ? element.TextContent[..40] + "..."
                : element.TextContent;
            line.AddChild(new SpanElement { Class = "tree-node-text", TextContent = textPreview });
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
            foreach (var child in element.Children)
            {
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
}
