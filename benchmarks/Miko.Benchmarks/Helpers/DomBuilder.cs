using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;

namespace Miko.Benchmarks.Helpers;

public static class DomBuilder
{
    public static DivElement CreateFlatTree(int childCount)
    {
        var root = new DivElement { Id = "root" };
        for (int i = 0; i < childCount; i++)
        {
            root.AddChild(new DivElement
            {
                Id = $"child-{i}",
                Class = i % 2 == 0 ? "even" : "odd",
                TextContent = $"Item {i}"
            });
        }
        return root;
    }

    public static DivElement CreateDeepTree(int depth)
    {
        var root = new DivElement { Id = "root" };
        var current = root;
        for (int i = 0; i < depth; i++)
        {
            var child = new DivElement
            {
                Id = $"level-{i}",
                Class = "nested",
                TextContent = i == depth - 1 ? "Leaf" : null
            };
            current.AddChild(child);
            current = child;
        }
        return root;
    }

    public static DivElement CreateFlexContainer(int childCount)
    {
        var root = new DivElement { Id = "flex-root", Class = "flex-container" };
        for (int i = 0; i < childCount; i++)
        {
            root.AddChild(new DivElement
            {
                Id = $"flex-item-{i}",
                Class = "flex-item",
                TextContent = $"Flex {i}"
            });
        }
        return root;
    }

    public static DivElement CreateInlineContainer(int childCount)
    {
        var root = new DivElement { Id = "inline-root" };
        for (int i = 0; i < childCount; i++)
        {
            root.AddChild(new SpanElement
            {
                Class = "inline-item",
                TextContent = $"Span {i}"
            });
        }
        return root;
    }

    public static DivElement CreateRealisticPage()
    {
        var root = new DivElement { Id = "app", Class = "app" };

        var header = new DivElement { Id = "header", Class = "header" };
        header.AddChild(new H1Element { TextContent = "Miko App" });
        for (int i = 0; i < 5; i++)
            header.AddChild(new SpanElement { Class = "nav-item", TextContent = $"Nav {i}" });
        root.AddChild(header);

        var main = new DivElement { Id = "main", Class = "main" };
        var sidebar = new DivElement { Class = "sidebar" };
        for (int i = 0; i < 10; i++)
            sidebar.AddChild(new DivElement { Class = "menu-item", TextContent = $"Menu {i}" });
        main.AddChild(sidebar);

        var content = new DivElement { Class = "content" };
        for (int i = 0; i < 20; i++)
        {
            var card = new DivElement { Class = "card" };
            card.AddChild(new H2Element { TextContent = $"Card {i}" });
            card.AddChild(new ParagraphElement { TextContent = "Lorem ipsum dolor sit amet." });
            card.AddChild(new ButtonElement { TextContent = "Action" });
            content.AddChild(card);
        }
        main.AddChild(content);
        root.AddChild(main);

        var footer = new DivElement { Id = "footer", Class = "footer" };
        footer.AddChild(new SpanElement { TextContent = "Footer content" });
        root.AddChild(footer);

        return root;
    }

    public static List<StyleSheet> CreateBlockStyleSheet()
    {
        return
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style { Display = Display.Block, Padding = Length.Px(5) }
                    }
                ]
            }
        ];
    }

    public static List<StyleSheet> CreateFlexStyleSheet()
    {
        return
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Padding = Length.Px(10)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-item"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(100),
                            Height = Length.Px(50),
                            MarginRight = Length.Px(5)
                        }
                    }
                ]
            }
        ];
    }

    public static List<StyleSheet> CreateInlineStyleSheet()
    {
        return
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new ClassSelector("inline-item"),
                        Style = new Style { Display = Display.Inline }
                    }
                ]
            }
        ];
    }

    public static List<StyleSheet> CreateLargeStyleSheet(int ruleCount)
    {
        var rules = new List<StyleRule>();
        for (int i = 0; i < ruleCount; i++)
        {
            Selector selector = (i % 3) switch
            {
                0 => new ClassSelector($"class-{i}"),
                1 => new IdSelector($"id-{i}"),
                _ => new TagSelector("div")
            };
            rules.Add(new StyleRule
            {
                Selector = selector,
                Style = new Style
                {
                    Display = Display.Block,
                    Width = Length.Px(100 + i),
                    Height = Length.Px(50 + i),
                    Padding = Length.Px(i % 20),
                    MarginTop = Length.Px(i % 10)
                }
            });
        }
        return [new StyleSheet { Rules = rules }];
    }

    /// <summary>
    /// 组件库规模的样式表（ISSUE-113 复现）：模拟 Miko.Ionic 那样按「组件 × 模式」展开的
    /// 大表——上千条规则，绝大多数是不会命中当前元素的类选择器，且大量为后代/复合选择器。
    /// 这是暴露「每元素逐条全表匹配」代价的关键条件：真实 Ionic 表约 1868 条规则。
    /// </summary>
    public static List<StyleSheet> CreateComponentLibraryStyleSheet()
    {
        var sheet = new StyleSheet();

        // 每个"组件"在两种模式（md / ios）下各生成一组规则，与 Ionic 的展开方式一致。
        string[] components =
        [
            "item", "list", "button", "icon", "label", "toolbar", "header", "content",
            "card", "chip", "badge", "note", "avatar", "segment", "toggle", "checkbox",
            "radio", "range", "input", "textarea", "select", "searchbar", "spinner",
            "thumbnail", "accordion", "breadcrumb", "fab", "grid", "modal", "popover",
        ];
        string[] modes = ["md", "ios"];
        string[] parts = ["native", "inner", "wrapper", "detail-icon", "highlight"];

        foreach (var mode in modes)
        {
            foreach (var component in components)
            {
                // 宿主规则：.ion-<c>.<mode>
                sheet.AddRule(
                    new CompoundSelector(new ClassSelector($"ion-{component}"), new ClassSelector(mode)),
                    new Style { Display = Display.Block, PaddingLeft = Length.Px(16) });

                // 内部结构规则：.ion-<c>.<mode> .<part>（后代选择器，关键选择器为最右侧）
                foreach (var part in parts)
                {
                    sheet.AddRule(
                        new DescendantSelector(
                            new CompoundSelector(new ClassSelector($"ion-{component}"), new ClassSelector(mode)),
                            new ClassSelector($"{component}-{part}")),
                        new Style { Display = Display.Flex, Height = Length.Px(24) });
                }

                // 命名色变体：.ion-<c>.<mode>.ion-color-<name>
                foreach (var color in new[] { "primary", "secondary", "danger" })
                {
                    sheet.AddRule(
                        new CompoundSelector(
                            new ClassSelector($"ion-{component}"), new ClassSelector(mode),
                            new ClassSelector($"ion-color-{color}")),
                        new Style { BackgroundColor = Color.FromRgb(60, 120, 200) });
                }
            }
        }

        return [sheet];
    }

    /// <summary>
    /// 组件库风格的列表页（ISSUE-113 复现）：一个 IonList 里若干个带 href 的 IonItem，
    /// 每项都是 host → native(a) → slot/inner/wrapper 的多层结构，与 DebugDemo 一致。
    /// </summary>
    public static DivElement CreateComponentListPage(int itemCount, string mode = "md")
    {
        var page = new DivElement { Class = $"ion-page {mode}" };

        var header = new DivElement { Class = $"ion-header {mode}" };
        var toolbar = new DivElement { Class = $"ion-toolbar {mode}" };
        toolbar.AddChild(new DivElement { Class = $"ion-title {mode}", TextContent = "Components" });
        header.AddChild(toolbar);
        page.AddChild(header);

        var content = new DivElement { Class = $"ion-content {mode}" };
        var list = new DivElement { Class = $"ion-list {mode} list-lines-full" };
        for (int i = 0; i < itemCount; i++)
        {
            var host = new DivElement { Class = $"ion-item {mode} in-list item-lines-full ion-activatable" };
            // Href 形态的 IonItem 渲染为 <a class="item-native">（见 IonItem.razor）。
            var native = new AnchorElement { Class = "item-native" };
            var start = new SpanElement { Class = "ion-slot-start" };
            start.AddChild(new DivElement { Class = $"ion-icon {mode} component-icon" });
            native.AddChild(start);
            var inner = new DivElement { Class = "item-inner" };
            inner.AddChild(new DivElement { Class = "input-wrapper", TextContent = $"Component {i}" });
            native.AddChild(inner);
            host.AddChild(native);
            list.AddChild(host);
        }
        content.AddChild(list);
        page.AddChild(content);
        return page;
    }

    public static List<StyleSheet> CreateRealisticStyleSheet()
    {
        return
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new ClassSelector("app"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(1200) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("header"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Height = Length.Px(60),
                            Padding = Length.Px(10)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("main"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("sidebar"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(200),
                            Padding = Length.Px(10)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("content"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Padding = Length.Px(20)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("card"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Padding = Length.Px(15),
                            MarginBottom = Length.Px(10),
                            Width = Length.Px(300)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("footer"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Height = Length.Px(40),
                            Padding = Length.Px(10)
                        }
                    }
                ]
            }
        ];
    }
}