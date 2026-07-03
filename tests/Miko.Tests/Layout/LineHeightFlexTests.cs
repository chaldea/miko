using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-078 回归测试：
/// .button-inner 设置了 line-height: 1（number）与 font-size: 14px，作为 flex 容器自身带文本 "Default"。
/// 当其 height:100% 因父级尺寸不确定退化为 auto 时，内容高度应按 line-height 解析为 14px，
/// 而非字体自然度量（约 20px）。嵌套结构 .button(inline-block) > .button-native(flex) > .button-inner(flex)。
/// </summary>
public class LineHeightFlexTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> BuildSheets()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject()
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".root"] = new()
            {
                Width = Length.Px(500),
                Height = Length.Px(500)
            }
        });
        sheet.Add(new CssObject
        {
            [".button"] = new()
            {
                Display = Display.InlineBlock,
                Height = Length.Auto,
                Width = Length.Auto,
                MinHeight = Length.Px(36),
                BackgroundColor = Color.FromRgb(0, 38, 200),
                Color = Color.FromRgb(255, 255, 255),
            },
            [".button-native"] = new()
            {
                Display = Display.Flex,
                Height = Length.Percent(100),
                Width = Length.Percent(100),
                Padding = new Padding(Length.Px(8), Length.Rem(1.1f)),
                MinHeight = Length.Px(36),
            },
            [".button-inner"] = new()
            {
                Display = Display.Flex,
                Height = Length.Percent(100),
                Width = Length.Percent(100),
                FontSize = Length.Px(14),
                LineHeight = Length.Number(1),
            }
        });
        return new List<StyleSheet> { sheet };
    }

    [Fact]
    public void ButtonInner_WithLineHeightOne_ShouldSizeToFourteenPx()
    {
        var inner = new SpanElement { Class = "button-inner", TextContent = "Default" };
        var native = new SpanElement { Class = "button-native", Children = { inner } };
        var button = new SpanElement { Class = "button", Children = { native } };
        var root = new DivElement { Class = "root", Children = { button } };

        var layout = _layoutEngine.Layout(root, BuildSheets(), 800, 600);

        var buttonBox = layout.Children[0];
        var nativeBox = buttonBox.Children[0];
        var innerBox = nativeBox.Children[0];

        // .button-inner 内容高度应按 line-height 1 × font-size 14 = 14px
        innerBox.BoxModel.Content.Height.ShouldBe(14f, 0.01f,
            ".button-inner content height should follow line-height 1 × font-size 14px = 14px, not font metrics (~20px)");

        // .button-native 由 min-height 撑到 36px（border-box），其内容高度 = 36 - padding(8+8) = 20px。
        nativeBox.BoxModel.BorderBox.Height.ShouldBe(36f, 0.01f);
        // .button 由 min-height 撑到 36px。
        buttonBox.BoxModel.BorderBox.Height.ShouldBe(36f, 0.01f);
    }
}
