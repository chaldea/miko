using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// 高宽循环依赖测试（ISSUE-077）：
/// 父元素 width/height=auto（依赖内容），子元素 width/height=100%（依赖父元素）。
/// 这构成循环依赖，CSS 规范下百分比针对"不确定尺寸"的包含块应视为 auto，
/// 最终由内容决定尺寸，而不是塌缩为 0。
/// </summary>
public class CircularSizeDependencyTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>
    /// ISSUE-077 原始复现：span.btn(auto/auto) > span.btn-native(100%/100%, 文本"Default")。
    /// 期望：两个盒子的宽高都由文本内容撑起，均 > 0。
    /// </summary>
    [Fact]
    public void AutoParent_WithPercentChild_ShouldSizeToContent()
    {
        var native = new SpanElement { Class = "btn-native", TextContent = "Default" };
        var btn = new SpanElement { Class = "btn", Children = { native } };
        var root = new DivElement { Class = "root", Children = { btn } };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
                    },
                    new()
                    {
                        Selector = new ClassSelector("btn"),
                        Style = new Style { Width = Length.Auto, Height = Length.Auto }
                    },
                    new()
                    {
                        Selector = new ClassSelector("btn-native"),
                        Style = new Style { Width = Length.Percent(100), Height = Length.Percent(100) }
                    },
                }
            }
        };

        var layout = _layoutEngine.Layout(root, styleSheets, 800, 600);

        var btnBox = layout.Children[0];
        var nativeBox = btnBox.Children[0];

        // 文本 "Default" 的实际宽度（用于比较）
        var (textWidth, _) = Miko.Utils.TextMeasurer.MeasureText(
            "Default",
            nativeBox.ComputedStyle.FontFamily,
            nativeBox.ComputedStyle.FontSize.Value,
            nativeBox.ComputedStyle.FontWeight);

        // 循环依赖解除后，子元素百分比针对不确定父尺寸退化为 auto（内容宽度）。
        nativeBox.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        nativeBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
        nativeBox.BoxModel.Content.Width.ShouldBe(textWidth, 0.5f);

        // 父元素 auto 尺寸由子内容撑起，也应 > 0。
        btnBox.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        btnBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 块级变体：div(inline-block, auto/auto) > div(inline-block, 100%/100%, 文本)。
    /// inline-block 的 auto 宽度收缩到内容，其百分比子元素同样应退化为内容尺寸。
    /// </summary>
    [Fact]
    public void InlineBlockAutoParent_WithPercentChild_ShouldSizeToContent()
    {
        var native = new DivElement { Class = "native", TextContent = "Default" };
        var btn = new DivElement { Class = "btn", Children = { native } };
        var root = new DivElement { Class = "root", Children = { btn } };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
                    },
                    new()
                    {
                        Selector = new ClassSelector("btn"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Auto,
                            Height = Length.Auto
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("native"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100)
                        }
                    },
                }
            }
        };

        var layout = _layoutEngine.Layout(root, styleSheets, 800, 600);

        var btnBox = layout.Children[0];
        var nativeBox = btnBox.Children[0];

        nativeBox.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        nativeBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
        btnBox.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        btnBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 确定尺寸的包含块不受影响：父元素显式 200x100，子元素 100% 应解析为 200x100（不退化）。
    /// 确保修复没有破坏正常的百分比解析。
    /// </summary>
    [Fact]
    public void DefiniteParent_WithPercentChild_ShouldResolveToParentSize()
    {
        var native = new DivElement { Class = "native" };
        var btn = new DivElement { Class = "btn", Children = { native } };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("btn"),
                        Style = new Style { Width = Length.Px(200), Height = Length.Px(100) }
                    },
                    new()
                    {
                        Selector = new ClassSelector("native"),
                        Style = new Style { Width = Length.Percent(100), Height = Length.Percent(100) }
                    },
                }
            }
        };

        var layout = _layoutEngine.Layout(btn, styleSheets, 800, 600);
        var nativeBox = layout.Children[0];

        nativeBox.BoxModel.Content.Width.ShouldBe(200);
        nativeBox.BoxModel.Content.Height.ShouldBe(100);
    }

    /// <summary>
    /// ISSUE-077 Flex 布局循环依赖：
    /// .button(inline-block, auto/auto) > .button-native(flex, 100%/100%) > .button-inner(flex, 100%/100%, 文本)。
    /// 多层嵌套的 flex 容器，最内层有文本，所有尺寸应由内容撑起，不应塌缩为 0。
    /// </summary>
    [Fact]
    public void FlexContainer_WithPercentSize_ShouldSizeToContent()
    {
        var inner = new SpanElement { Class = "button-inner", TextContent = "Default" };
        var native = new SpanElement { Class = "button-native", Children = { inner } };
        var button = new DivElement { Class = "button", Children = { native } };
        var container = new DivElement { Class = "container", Children = { button } };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Auto,
                            Height = Length.Auto
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button-native"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100)
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button-inner"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100)
                        }
                    },
                }
            }
        };

        var layout = _layoutEngine.Layout(container, styleSheets, 800, 600);

        var buttonBox = layout.Children[0];
        var nativeBox = buttonBox.Children[0];
        var innerBox = nativeBox.Children[0];

        // 循环依赖解除后，所有层级的宽高都应由最内层文本内容撑起，不应为 0。
        innerBox.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        innerBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
        nativeBox.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        nativeBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
        buttonBox.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        buttonBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// ISSUE-077 问题1：min-height 与百分比高度的交互。
    /// .button(inline-block, auto/auto, min-height:36) >
    /// .button-native(flex, 100%/100%, padding:8px, min-height:36) >
    /// .button-inner(flex, 100%/100%, 文本)
    ///
    /// button-inner 的 height:100% 应相对于 button-native 的内容区高度（36 - 8*2 = 20px），
    /// 而非整个 36px。
    /// </summary>
    [Fact]
    public void FlexWithMinHeight_PercentChildHeight_ShouldResolveToContentArea()
    {
        var inner = new SpanElement { Class = "button-inner", TextContent = "Default" };
        var native = new SpanElement { Class = "button-native", Children = { inner } };
        var button = new DivElement { Class = "button", Children = { native } };
        var container = new DivElement { Class = "root", Children = { button } };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new UniversalSelector(),
                        Style = new Style { BoxSizing = BoxSizing.BorderBox }
                    },
                    new()
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Auto,
                            Height = Length.Auto,
                            MinHeight = Length.Px(36)
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button-native"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100),
                            Padding = new Padding(Length.Px(8), Length.Rem(1.1f)),
                            MinHeight = Length.Px(36),
                            FontSize = Length.Px(14)
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button-inner"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100)
                        }
                    },
                }
            }
        };

        var layout = _layoutEngine.Layout(container, styleSheets, 800, 600);

        var buttonBox = layout.Children[0];
        var nativeBox = buttonBox.Children[0];
        var innerBox = nativeBox.Children[0];

        // button: min-height 36px，由于是 border-box，内容区就是 36px
        buttonBox.BoxModel.Content.Height.ShouldBe(36f, 0.1f);

        // button-native: height:100% = 36px，min-height:36px，padding 8*2=16px
        // border-box 下：总高度 36px，内容区 = 36 - 16 = 20px
        nativeBox.BoxModel.Content.Height.ShouldBe(20f, 0.5f);

        // button-inner: height:100% 应相对于 button-native 的内容区（20px和浏览器值存在偏差，Miko差不多17-18左右）
        innerBox.BoxModel.Content.Height.ShouldBe(18f, 0.5f);
    }
}
