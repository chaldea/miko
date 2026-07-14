using Miko.Common;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-088：outline 属性（outline 简写、outline-width/color/style/offset）解析测试。
/// </summary>
public class Issue088OutlineTests
{
    [Fact]
    public void FromStyle_OutlineLonghands_Resolve()
    {
        var computed = ComputedStyle.FromStyle(new Style
        {
            OutlineWidth = Length.Px(2),
            OutlineColor = Color.Red,
            OutlineStyle = BorderStyle.Solid,
            OutlineOffset = Length.Px(3)
        });

        computed.OutlineWidth.Value.ShouldBe(2f);
        computed.OutlineColor.ShouldBe(Color.Red);
        computed.OutlineStyle.ShouldBe(BorderStyle.Solid);
        computed.OutlineOffset.Value.ShouldBe(3f);
        computed.HasVisibleOutline.ShouldBeTrue();
    }

    [Fact]
    public void FromStyle_NoOutline_DefaultsToNoneAndZero()
    {
        var computed = ComputedStyle.FromStyle(new Style());

        computed.OutlineStyle.ShouldBe(BorderStyle.None);
        computed.OutlineWidth.Value.ShouldBe(0f);
        computed.OutlineOffset.Value.ShouldBe(0f);
        computed.HasVisibleOutline.ShouldBeFalse();
    }

    [Fact]
    public void OutlineShorthand_SetsWidthStyleColor()
    {
        var style = new Style { Outline = Outline.Solid(Length.Px(4), Color.Blue) };

        // 简写展开到各 longhand。
        style.OutlineWidth!.Value.Value.Value.ShouldBe(4f);
        style.OutlineStyle!.Value.Value.ShouldBe(BorderStyle.Solid);
        style.OutlineColor!.Value.Value.ShouldBe(Color.Blue);

        var computed = ComputedStyle.FromStyle(style);
        computed.HasVisibleOutline.ShouldBeTrue();
        computed.OutlineColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void OutlineShorthand_Getter_ReflectsLonghands()
    {
        var style = new Style
        {
            OutlineWidth = Length.Px(5),
            OutlineStyle = BorderStyle.Dashed,
            OutlineColor = Color.FromRgb(10, 20, 30)
        };

        var outline = style.Outline;
        outline.Width.Value.ShouldBe(5f);
        outline.Style.ShouldBe(BorderStyle.Dashed);
        outline.Color.ShouldBe(Color.FromRgb(10, 20, 30));
    }

    [Fact]
    public void HasVisibleOutline_False_WhenTransparent()
    {
        // 有宽度和线型，但颜色全透明 → 不可见。
        var computed = ComputedStyle.FromStyle(new Style
        {
            OutlineWidth = Length.Px(2),
            OutlineStyle = BorderStyle.Solid,
            OutlineColor = Color.Transparent
        });
        computed.HasVisibleOutline.ShouldBeFalse();
    }
}
