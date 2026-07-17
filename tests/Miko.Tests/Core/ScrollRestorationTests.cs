using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 测试导航/重建后滚动位置恢复（ISSUE-092）
/// </summary>
public class ScrollRestorationTests
{
    [Fact]
    public void Initialize_ShouldRestoreScrollPosition_WhenElementIdentityMatches()
    {
        // 模拟布局：左侧可滚动菜单 + 右侧内容区
        var sidebar = new DivElement
        {
            Id = "sidebar",
            Class = "sidebar",
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            }
        };

        // 添加足够的菜单项以产生滚动
        for (int i = 0; i < 20; i++)
        {
            sidebar.AddChild(new DivElement
            {
                Class = "nav-item",
                Style = new Style { Height = Length.Px(40) },
                TextContent = $"Item {i}"
            });
        }

        var mainContent = new DivElement
        {
            Id = "main-content",
            Class = "main-content",
            Style = new Style { Width = Length.Px(600), Height = Length.Px(300) },
            TextContent = "Page 1"
        };

        var layout = new DivElement
        {
            Class = "layout",
            Style = new Style
            {
                Display = Display.Flex,
                Width = Length.Px(800),
                Height = Length.Px(300),
            }
        };
        layout.AddChild(sidebar);
        layout.AddChild(mainContent);

        // 初始化引擎
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 300));
        engine.Initialize(layout, new List<StyleSheet>(), surface.Canvas, 800, 300);

        // 滚动侧边栏到底部
        engine.ScrollBy(100, 150, 0, 500);
        var layoutAfterScroll = engine.GetCurrentLayout()!;
        var sidebarBox = FindLayoutBoxById(layoutAfterScroll, "sidebar");
        sidebarBox.ShouldNotBeNull();
        var scrollTopAfterScroll = sidebarBox.ScrollTop;
        scrollTopAfterScroll.ShouldBeGreaterThan(0, "Sidebar should be scrolled");

        // 模拟导航：重建 DOM（右侧内容变化，左侧保持相同结构）
        var newSidebar = new DivElement
        {
            Id = "sidebar",
            Class = "sidebar",
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            }
        };

        for (int i = 0; i < 20; i++)
        {
            newSidebar.AddChild(new DivElement
            {
                Class = "nav-item",
                Style = new Style { Height = Length.Px(40) },
                TextContent = $"Item {i}"
            });
        }

        var newMainContent = new DivElement
        {
            Id = "main-content",
            Class = "main-content",
            Style = new Style { Width = Length.Px(600), Height = Length.Px(300) },
            TextContent = "Page 2"  // 内容变化
        };

        var newLayout = new DivElement
        {
            Class = "layout",
            Style = new Style
            {
                Display = Display.Flex,
                Width = Length.Px(800),
                Height = Length.Px(300),
            }
        };
        newLayout.AddChild(newSidebar);
        newLayout.AddChild(newMainContent);

        // 重新初始化（模拟导航）
        engine.Initialize(newLayout, new List<StyleSheet>(), surface.Canvas, 800, 300);

        // 验证：左侧菜单的滚动位置应该被保留
        var layoutAfterRebuild = engine.GetCurrentLayout()!;
        var sidebarBoxAfterRebuild = FindLayoutBoxById(layoutAfterRebuild, "sidebar");
        sidebarBoxAfterRebuild.ShouldNotBeNull();
        sidebarBoxAfterRebuild.ScrollTop.ShouldBe(scrollTopAfterScroll,
            "Sidebar scroll position should be restored after navigation");
    }

    [Fact]
    public void Initialize_ShouldNotRestoreScrollPosition_WhenElementIdentityChanges()
    {
        // 可滚动元素
        var scrollable = new DivElement
        {
            Id = "scrollable",
            Style = new Style
            {
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(800) } } }
        };

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        engine.Initialize(scrollable, new List<StyleSheet>(), surface.Canvas, 400, 300);

        // 滚动
        engine.ScrollBy(200, 150, 0, 200);
        var layoutAfterScroll = engine.GetCurrentLayout()!;
        layoutAfterScroll.ScrollTop.ShouldBe(200);

        // 重建为完全不同的元素（div → span，标签名不同）
        var newElement = new SpanElement
        {
            Id = "scrollable",  // ID 相同但标签名不同
            Style = new Style
            {
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(800) } } }
        };

        engine.Initialize(newElement, new List<StyleSheet>(), surface.Canvas, 400, 300);

        // 滚动位置不应该被恢复（因为元素身份不同）
        var layoutAfterRebuild = engine.GetCurrentLayout()!;
        layoutAfterRebuild.ScrollTop.ShouldBe(0,
            "Scroll position should NOT be restored when element identity changes");
    }

    [Fact]
    public void Initialize_ShouldRestoreScrollPosition_ForNestedScrollableElements()
    {
        // 测试简化：只测试内层可滚动元素的滚动位置恢复
        var innerScrollable = new DivElement
        {
            Id = "inner",
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(400) } } }
        };

        var outerContainer = new DivElement
        {
            Id = "outer",
            Style = new Style
            {
                Width = Length.Px(400),
                Height = Length.Px(300),
            }
        };
        outerContainer.AddChild(innerScrollable);

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        engine.Initialize(outerContainer, new List<StyleSheet>(), surface.Canvas, 400, 300);

        // 滚动内层容器
        engine.ScrollBy(100, 50, 0, 50);
        var innerBox = FindLayoutBoxById(engine.GetCurrentLayout()!, "inner");
        innerBox.ShouldNotBeNull();
        var scrollAfterScroll = innerBox.ScrollTop;
        scrollAfterScroll.ShouldBeGreaterThan(0, "Inner container should be scrolled");

        // 重建相同结构
        var newInnerScrollable = new DivElement
        {
            Id = "inner",
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(400) } } }
        };

        var newOuterContainer = new DivElement
        {
            Id = "outer",
            Style = new Style
            {
                Width = Length.Px(400),
                Height = Length.Px(300),
            }
        };
        newOuterContainer.AddChild(newInnerScrollable);

        engine.Initialize(newOuterContainer, new List<StyleSheet>(), surface.Canvas, 400, 300);

        // 验证：内层滚动位置应该被恢复
        var newInnerBox = FindLayoutBoxById(engine.GetCurrentLayout()!, "inner");
        newInnerBox.ShouldNotBeNull();
        newInnerBox.ScrollTop.ShouldBe(scrollAfterScroll, "Inner scroll position should be restored");
    }

    [Fact]
    public void Initialize_ShouldRestoreHorizontalScrollPosition()
    {
        var scrollable = new DivElement
        {
            Id = "horizontal-scroll",
            Style = new Style
            {
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowX = Overflow.Scroll,
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Display = Display.InlineBlock,
                        Width = Length.Px(1000),
                        Height = Length.Px(100),
                    }
                }
            }
        };

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        engine.Initialize(scrollable, new List<StyleSheet>(), surface.Canvas, 400, 300);

        // 水平滚动
        engine.ScrollBy(200, 150, 150, 0);
        var layoutAfterScroll = engine.GetCurrentLayout()!;
        layoutAfterScroll.ScrollLeft.ShouldBe(150);

        // 重建
        var newScrollable = new DivElement
        {
            Id = "horizontal-scroll",
            Style = new Style
            {
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowX = Overflow.Scroll,
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Display = Display.InlineBlock,
                        Width = Length.Px(1000),
                        Height = Length.Px(100),
                    }
                }
            }
        };

        engine.Initialize(newScrollable, new List<StyleSheet>(), surface.Canvas, 400, 300);

        // 验证水平滚动位置恢复
        var layoutAfterRebuild = engine.GetCurrentLayout()!;
        layoutAfterRebuild.ScrollLeft.ShouldBe(150,
            "Horizontal scroll position should be restored");
    }

    [Fact]
    public void Initialize_ShouldNotCrossRestoreScroll_BetweenSiblingsWithSameTag()
    {
        // 复现 ISSUE-092 的回归：.sidebar 与 .main-content 都是 <div> 且都可滚动，
        // 仅靠 class 区分（无 Id）。滚动 .sidebar 后重建，.main-content 不应被串入
        // .sidebar 的滚动偏移。
        static DivElement BuildLayout()
        {
            var sidebar = new DivElement
            {
                Class = "sidebar",
                Style = new Style
                {
                    Width = Length.Px(200),
                    Height = Length.Px(300),
                    OverflowY = Overflow.Auto,
                },
                Children = { new DivElement { Style = new Style { Height = Length.Px(1000) } } }
            };

            var mainContent = new DivElement
            {
                Class = "main-content",
                Style = new Style
                {
                    Width = Length.Px(600),
                    Height = Length.Px(300),
                    OverflowY = Overflow.Auto,
                },
                Children = { new DivElement { Style = new Style { Height = Length.Px(1000) } } }
            };

            var layout = new DivElement
            {
                Class = "layout",
                Style = new Style { Display = Display.Flex, Width = Length.Px(800), Height = Length.Px(300) }
            };
            layout.AddChild(sidebar);
            layout.AddChild(mainContent);
            return layout;
        }

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 300));
        engine.Initialize(BuildLayout(), new List<StyleSheet>(), surface.Canvas, 800, 300);

        // 仅滚动 .sidebar（x 落在左侧 200px 宽的侧栏内）
        engine.ScrollBy(100, 150, 0, 200);
        var sidebarBox = FindLayoutBoxByClass(engine.GetCurrentLayout()!, "sidebar")!;
        sidebarBox.ScrollTop.ShouldBe(200);
        var mainBox = FindLayoutBoxByClass(engine.GetCurrentLayout()!, "main-content")!;
        mainBox.ScrollTop.ShouldBe(0, "main-content should not have scrolled");

        // 模拟导航重建
        engine.Initialize(BuildLayout(), new List<StyleSheet>(), surface.Canvas, 800, 300);

        // .sidebar 的滚动位置应保留，.main-content 必须仍为 0（未被串位）
        var newSidebarBox = FindLayoutBoxByClass(engine.GetCurrentLayout()!, "sidebar")!;
        newSidebarBox.ScrollTop.ShouldBe(200, "sidebar scroll should be restored");
        var newMainBox = FindLayoutBoxByClass(engine.GetCurrentLayout()!, "main-content")!;
        newMainBox.ScrollTop.ShouldBe(0, "main-content scroll must NOT be cross-restored from sidebar");
    }

    [Fact]
    public void Initialize_ShouldNotRestoreScroll_WhenContainerContentChanges()
    {
        // 复现 ISSUE-092 问题2（Ionic 场景）：.main-content 是稳定的路由内容容器
        // （同标签、同 class、同位置），但导航时其内部页面完全替换。
        // 从长内容页（button，已滚动到底部）切到短内容页（accordion）时，
        // 绝不能把 button 页的滚动偏移串到 accordion 页——否则短内容被顶出可视区。

        // 长内容页：main-content 内含大量条目，产生滚动
        var longContent = new DivElement
        {
            Class = "main-content",
            Style = new Style
            {
                Width = Length.Px(600),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            }
        };
        for (int i = 0; i < 20; i++)
            longContent.AddChild(new DivElement
            {
                Class = "button",
                Style = new Style { Height = Length.Px(50) },
                TextContent = $"Button {i}"
            });

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));
        engine.Initialize(longContent, new List<StyleSheet>(), surface.Canvas, 600, 300);

        // 滚动到底部
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "long content should be scrolled");

        // 导航到短内容页：同样是 .main-content 容器，但内部结构完全不同（单个短卡片）
        var shortContent = new DivElement
        {
            Class = "main-content",
            Style = new Style
            {
                Width = Length.Px(600),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement
                {
                    Class = "accordion",
                    Style = new Style { Height = Length.Px(80) },
                    TextContent = "Accordion"
                }
            }
        };

        engine.Initialize(shortContent, new List<StyleSheet>(), surface.Canvas, 600, 300);

        // 关键断言：内容替换后滚动必须重置为 0，短内容才能从顶部显示
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0,
            "Scroll must reset when the container's content is replaced (button page -> accordion page)");
    }

    [Fact]
    public void Initialize_ShouldRestoreScroll_WhenOnlyLeafTextChanges()
    {
        // 结构不变、仅叶子文本变化（如 StateHasChanged 重新渲染列表项文案）时，
        // 应视为「同一内容」并恢复滚动位置。
        static DivElement BuildList(string suffix)
        {
            var list = new DivElement
            {
                Class = "scroll-list",
                Style = new Style
                {
                    Width = Length.Px(400),
                    Height = Length.Px(300),
                    OverflowY = Overflow.Auto,
                }
            };
            for (int i = 0; i < 20; i++)
                list.AddChild(new DivElement
                {
                    Class = "row",
                    Style = new Style { Height = Length.Px(40) },
                    TextContent = $"Row {i} {suffix}"  // 文本不同，但结构相同
                });
            return list;
        }

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        engine.Initialize(BuildList("v1"), new List<StyleSheet>(), surface.Canvas, 400, 300);

        engine.ScrollBy(200, 150, 0, 300);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        // 重新渲染：结构一致，仅文本变化
        engine.Initialize(BuildList("v2"), new List<StyleSheet>(), surface.Canvas, 400, 300);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "Scroll should be restored when structure is unchanged and only leaf text differs");
    }

    [Fact]
    public void Initialize_ShouldRestoreScroll_WhenDescendantTogglesDisplayNone()
    {
        // 复现 ion-accordion 问题1：可滚动容器 .root 内含一个折叠面板，其内容盒在折叠时为
        // display:none、展开时可见。切换该后代的 display 会改变 .root 的<b>布局</b>子树形状
        // （display:none 的盒子被从布局树过滤），但 DOM 子树保持不变（内容元素始终在树中）。
        // 展开/折叠面板时，绝不能重置外层 .root 的滚动条。
        static DivElement BuildRoot(bool expanded)
        {
            // 内容盒：折叠时 display:none，展开时为块级（始终存在于 DOM 中）。
            var content = new DivElement
            {
                Class = "accordion-content",
                Style = new Style
                {
                    Display = expanded ? Display.Block : Display.None,
                    Height = Length.Px(120),
                },
                TextContent = "Panel content"
            };

            var accordion = new DivElement { Class = "accordion" };
            accordion.AddChild(new DivElement
            {
                Class = "accordion-header",
                Style = new Style { Height = Length.Px(48) },
                TextContent = "Header"
            });
            accordion.AddChild(content);

            // 高内容块，使 .root 产生可滚动溢出。
            var container = new DivElement
            {
                Class = "container",
                Style = new Style { Height = Length.Px(600) },
                TextContent = "Demo"
            };

            var root = new DivElement
            {
                Class = "root",
                Style = new Style
                {
                    Width = Length.Px(500),
                    Height = Length.Px(500),
                    OverflowY = Overflow.Scroll,
                }
            };
            root.AddChild(container);
            root.AddChild(accordion);
            return root;
        }

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(500, 500));
        // 初始：面板折叠。
        engine.Initialize(BuildRoot(expanded: false), new List<StyleSheet>(), surface.Canvas, 500, 500);

        // 滚动 .root 到底部。
        engine.ScrollBy(250, 250, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "root should be scrolled to the bottom");

        // 展开面板：内容盒从 display:none 变为可见（模拟 IonAccordion 展开触发重建）。
        engine.Initialize(BuildRoot(expanded: true), new List<StyleSheet>(), surface.Canvas, 500, 500);

        // 关键断言：展开面板不得重置 .root 的滚动条。
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "Expanding an accordion panel must not reset the outer scroll position");

        // 再次折叠面板：内容盒回到 display:none。
        engine.Initialize(BuildRoot(expanded: false), new List<StyleSheet>(), surface.Canvas, 500, 500);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "Collapsing an accordion panel must not reset the outer scroll position");
    }

    private LayoutBox? FindLayoutBoxById(LayoutBox root, string id)
    {
        if (root.Element.Id == id) return root;
        foreach (var child in root.Children)
        {
            var found = FindLayoutBoxById(child, id);
            if (found != null) return found;
        }
        return null;
    }

    private LayoutBox? FindLayoutBoxByClass(LayoutBox root, string className)
    {
        if (root.Element.HasClass(className)) return root;
        foreach (var child in root.Children)
        {
            var found = FindLayoutBoxByClass(child, className);
            if (found != null) return found;
        }
        return null;
    }
}
