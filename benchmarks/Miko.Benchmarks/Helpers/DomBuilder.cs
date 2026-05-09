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