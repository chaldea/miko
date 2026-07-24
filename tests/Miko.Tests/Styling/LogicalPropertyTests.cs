using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-103：逻辑属性（margin-inline-start、inline-size 等）支持。
/// 计算样式以逻辑值为标准存储，物理属性按 direction / writing-mode 映射为门面。
/// </summary>
public class LogicalPropertyTests
{
    private static ComputedStyle Compute(Style style) => ComputedStyle.FromStyle(style);

    #region 默认书写模式（horizontal-tb + ltr）：逻辑属性与物理属性一致

    [Fact]
    public void Should_MapMarginInlineStart_ToMarginLeft_ByDefault()
    {
        var computed = Compute(new Style { MarginInlineStart = Length.Px(10) });

        computed.MarginLeft.Value.ShouldBe(10f);
        computed.MarginInlineStart.Value.ShouldBe(10f);
        // 其余边不受影响
        computed.MarginRight.Value.ShouldBe(0f);
        computed.MarginTop.Value.ShouldBe(0f);
    }

    [Fact]
    public void Should_MapAllLogicalMargins_ToPhysicalSides_ByDefault()
    {
        var computed = Compute(new Style
        {
            MarginInlineStart = Length.Px(1),
            MarginInlineEnd = Length.Px(2),
            MarginBlockStart = Length.Px(3),
            MarginBlockEnd = Length.Px(4),
        });

        computed.MarginLeft.Value.ShouldBe(1f);
        computed.MarginRight.Value.ShouldBe(2f);
        computed.MarginTop.Value.ShouldBe(3f);
        computed.MarginBottom.Value.ShouldBe(4f);
    }

    [Fact]
    public void Should_MapAllLogicalPaddings_ToPhysicalSides_ByDefault()
    {
        var computed = Compute(new Style
        {
            PaddingInlineStart = Length.Px(1),
            PaddingInlineEnd = Length.Px(2),
            PaddingBlockStart = Length.Px(3),
            PaddingBlockEnd = Length.Px(4),
        });

        computed.PaddingLeft.Value.ShouldBe(1f);
        computed.PaddingRight.Value.ShouldBe(2f);
        computed.PaddingTop.Value.ShouldBe(3f);
        computed.PaddingBottom.Value.ShouldBe(4f);
    }

    [Fact]
    public void Should_MapLogicalSizes_ToWidthAndHeight_ByDefault()
    {
        var computed = Compute(new Style
        {
            InlineSize = Length.Px(100),
            BlockSize = Length.Px(50),
            MinInlineSize = Length.Px(10),
            MinBlockSize = Length.Px(5),
            MaxInlineSize = Length.Px(200),
            MaxBlockSize = Length.Px(80),
        });

        computed.Width.Value.ShouldBe(100f);
        computed.Height.Value.ShouldBe(50f);
        computed.MinWidth.Value.ShouldBe(10f);
        computed.MinHeight.Value.ShouldBe(5f);
        computed.MaxWidth.Value.ShouldBe(200f);
        computed.MaxHeight.Value.ShouldBe(80f);
    }

    [Fact]
    public void Should_MapLogicalInsets_ToPhysicalOffsets_ByDefault()
    {
        var computed = Compute(new Style
        {
            InsetInlineStart = Length.Px(1),
            InsetInlineEnd = Length.Px(2),
            InsetBlockStart = Length.Px(3),
            InsetBlockEnd = Length.Px(4),
        });

        computed.Left.Value.ShouldBe(1f);
        computed.Right.Value.ShouldBe(2f);
        computed.Top.Value.ShouldBe(3f);
        computed.Bottom.Value.ShouldBe(4f);
    }

    [Fact]
    public void Should_MapLogicalBorder_ToPhysicalBorder_ByDefault()
    {
        var computed = Compute(new Style
        {
            BorderInlineStartWidth = Length.Px(2),
            BorderInlineStartStyle = BorderStyle.Solid,
            BorderInlineStartColor = Color.Red,
            BorderBlockStartWidth = Length.Px(4),
            BorderBlockStartStyle = BorderStyle.Dashed,
            BorderBlockStartColor = Color.Blue,
        });

        computed.BorderLeftWidth.Value.ShouldBe(2f);
        computed.BorderLeftStyle.ShouldBe(BorderStyle.Solid);
        computed.BorderLeftColor.ShouldBe(Color.Red);
        computed.BorderTopWidth.Value.ShouldBe(4f);
        computed.BorderTopStyle.ShouldBe(BorderStyle.Dashed);
        computed.BorderTopColor.ShouldBe(Color.Blue);
    }

    #endregion

    #region 物理属性写入逻辑存储（兼容性）

    [Fact]
    public void Should_StorePhysicalMargin_AsLogicalCanonical()
    {
        // 物理样式在计算时映射成逻辑样式：物理属性写入后，逻辑计算值可读。
        var computed = Compute(new Style { MarginLeft = Length.Px(7) });

        computed.MarginInlineStart.Value.ShouldBe(7f);
        computed.MarginLeft.Value.ShouldBe(7f);
    }

    [Fact]
    public void Should_StorePhysicalWidth_AsInlineSizeCanonical()
    {
        var computed = Compute(new Style { Width = Length.Px(300), Height = Length.Px(150) });

        computed.InlineSize.Value.ShouldBe(300f);
        computed.BlockSize.Value.ShouldBe(150f);
    }

    [Fact]
    public void Should_StorePhysicalBorder_AsLogicalCanonical()
    {
        var computed = Compute(new Style { BorderTopWidth = Length.Px(3), BorderTopStyle = BorderStyle.Solid });

        computed.BorderBlockStartWidth.Value.ShouldBe(3f);
        computed.BorderBlockStartStyle.ShouldBe(BorderStyle.Solid);
    }

    [Fact]
    public void Should_PreferLogicalProperty_WhenBothLogicalAndPhysicalSet()
    {
        // 同一 Style 中逻辑与物理属性同时设置时逻辑属性优先
        // （物理样式多为 UA 默认/旧式移植样式，逻辑样式多为作者显式声明）。
        var computed = Compute(new Style
        {
            MarginLeft = Length.Px(5),
            MarginInlineStart = Length.Px(9),
        });

        computed.MarginLeft.Value.ShouldBe(9f);
        computed.MarginInlineStart.Value.ShouldBe(9f);
    }

    #endregion

    #region direction: rtl

    [Fact]
    public void Should_MapInlineStart_ToRight_WhenRtl()
    {
        var computed = Compute(new Style
        {
            Direction = Direction.Rtl,
            MarginInlineStart = Length.Px(10),
            MarginInlineEnd = Length.Px(20),
        });

        computed.MarginRight.Value.ShouldBe(10f);
        computed.MarginLeft.Value.ShouldBe(20f);
        // block 轴不受 direction 影响
        computed.MarginTop.Value.ShouldBe(0f);
    }

    [Fact]
    public void Should_MapPhysicalRight_ToInlineStartCanonical_WhenRtl()
    {
        var computed = Compute(new Style
        {
            Direction = Direction.Rtl,
            MarginRight = Length.Px(7),
        });

        computed.MarginInlineStart.Value.ShouldBe(7f);
        computed.MarginRight.Value.ShouldBe(7f);
    }

    [Fact]
    public void Should_MapLogicalInsets_WhenRtl()
    {
        var computed = Compute(new Style
        {
            Direction = Direction.Rtl,
            InsetInlineStart = Length.Px(1),
            InsetInlineEnd = Length.Px(2),
        });

        computed.Right.Value.ShouldBe(1f);
        computed.Left.Value.ShouldBe(2f);
    }

    #endregion

    #region writing-mode: vertical

    [Fact]
    public void Should_MapLogicalEdges_WhenVerticalRl()
    {
        var computed = Compute(new Style
        {
            WritingMode = WritingMode.VerticalRl,
            MarginInlineStart = Length.Px(1),
            MarginInlineEnd = Length.Px(2),
            MarginBlockStart = Length.Px(3),
            MarginBlockEnd = Length.Px(4),
        });

        // vertical-rl + ltr：inline 轴垂直向下，block 轴自右而左
        computed.MarginTop.Value.ShouldBe(1f);
        computed.MarginBottom.Value.ShouldBe(2f);
        computed.MarginRight.Value.ShouldBe(3f);
        computed.MarginLeft.Value.ShouldBe(4f);
    }

    [Fact]
    public void Should_MapInlineStart_ToBottom_WhenVerticalRlAndRtl()
    {
        var computed = Compute(new Style
        {
            WritingMode = WritingMode.VerticalRl,
            Direction = Direction.Rtl,
            MarginInlineStart = Length.Px(1),
            MarginInlineEnd = Length.Px(2),
        });

        computed.MarginBottom.Value.ShouldBe(1f);
        computed.MarginTop.Value.ShouldBe(2f);
    }

    [Fact]
    public void Should_MapLogicalEdges_WhenVerticalLr()
    {
        var computed = Compute(new Style
        {
            WritingMode = WritingMode.VerticalLr,
            MarginBlockStart = Length.Px(3),
            MarginBlockEnd = Length.Px(4),
        });

        // vertical-lr：block 轴自左而右（inline 轴同 vertical-rl）
        computed.MarginLeft.Value.ShouldBe(3f);
        computed.MarginRight.Value.ShouldBe(4f);
    }

    [Fact]
    public void Should_MapLogicalSizes_WhenVerticalRl()
    {
        var computed = Compute(new Style
        {
            WritingMode = WritingMode.VerticalRl,
            InlineSize = Length.Px(100),
            BlockSize = Length.Px(50),
        });

        // 垂直书写：inline 轴 = 垂直（inline-size ↔ height）
        computed.Height.Value.ShouldBe(100f);
        computed.Width.Value.ShouldBe(50f);
    }

    [Fact]
    public void Should_MapLogicalBorder_WhenVerticalRl()
    {
        var computed = Compute(new Style
        {
            WritingMode = WritingMode.VerticalRl,
            BorderBlockStartWidth = Length.Px(2),
            BorderInlineStartWidth = Length.Px(5),
        });

        computed.BorderRightWidth.Value.ShouldBe(2f);
        computed.BorderTopWidth.Value.ShouldBe(5f);
    }

    #endregion

    #region 简写属性

    [Fact]
    public void Should_ExpandMarginInlineShorthand()
    {
        var computed = Compute(new Style { MarginInline = Length.Px(8) });

        computed.MarginLeft.Value.ShouldBe(8f);
        computed.MarginRight.Value.ShouldBe(8f);
    }

    [Fact]
    public void Should_ExpandPaddingBlockShorthand()
    {
        var computed = Compute(new Style { PaddingBlock = Length.Px(6) });

        computed.PaddingTop.Value.ShouldBe(6f);
        computed.PaddingBottom.Value.ShouldBe(6f);
    }

    [Fact]
    public void Should_ExpandInsetInlineShorthand()
    {
        var computed = Compute(new Style { InsetInline = Length.Px(4) });

        computed.Left.Value.ShouldBe(4f);
        computed.Right.Value.ShouldBe(4f);
    }

    [Fact]
    public void Should_ExpandBorderInlineStartShorthand()
    {
        var computed = Compute(new Style
        {
            BorderInlineStart = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Green),
        });

        computed.BorderLeftWidth.Value.ShouldBe(2f);
        computed.BorderLeftStyle.ShouldBe(BorderStyle.Solid);
        computed.BorderLeftColor.ShouldBe(Color.Green);
    }

    [Fact]
    public void Should_ExpandBorderBlockShorthand()
    {
        var computed = Compute(new Style
        {
            BorderBlock = new BorderSide(Length.Px(1), BorderStyle.Dotted, Color.Black),
        });

        computed.BorderTopWidth.Value.ShouldBe(1f);
        computed.BorderBottomWidth.Value.ShouldBe(1f);
        computed.BorderTopStyle.ShouldBe(BorderStyle.Dotted);
        computed.BorderBottomStyle.ShouldBe(BorderStyle.Dotted);
    }

    #endregion

    #region 统一边框回退

    [Fact]
    public void Should_NotOverrideLogicalBorderSide_WithUnifiedBorderWidth()
    {
        var computed = Compute(new Style
        {
            BorderWidth = Length.Px(2),
            BorderInlineStartWidth = Length.Px(5),
        });

        computed.BorderInlineStartWidth.Value.ShouldBe(5f);
        computed.BorderInlineEndWidth.Value.ShouldBe(2f);
        computed.BorderBlockStartWidth.Value.ShouldBe(2f);
        computed.BorderBlockEndWidth.Value.ShouldBe(2f);
    }

    [Fact]
    public void Should_NotOverridePhysicalBorderSide_WithUnifiedBorderWidth()
    {
        // 物理边已设置时，统一边框宽度也不应覆盖其对应的逻辑边。
        var computed = Compute(new Style
        {
            BorderWidth = Length.Px(2),
            BorderLeftWidth = Length.Px(7),
        });

        computed.BorderLeftWidth.Value.ShouldBe(7f);
        computed.BorderRightWidth.Value.ShouldBe(2f);
        computed.BorderTopWidth.Value.ShouldBe(2f);
        computed.BorderBottomWidth.Value.ShouldBe(2f);
    }

    [Fact]
    public void Should_ApplyUnifiedBorderFallback_WhenRtl()
    {
        // rtl 下物理右边 = inline-start：物理右边已设置时统一值不应覆盖 inline-start。
        var computed = Compute(new Style
        {
            Direction = Direction.Rtl,
            BorderWidth = Length.Px(2),
            BorderRightWidth = Length.Px(7),
        });

        computed.BorderRightWidth.Value.ShouldBe(7f);
        computed.BorderInlineStartWidth.Value.ShouldBe(7f);
        computed.BorderLeftWidth.Value.ShouldBe(2f);
    }

    #endregion

    #region 级联与继承

    [Fact]
    public void Should_MergeLogicalProperties_WithThisWinsSemantics()
    {
        var style = new Style { MarginInlineStart = Length.Px(10) };
        style.Merge(new Style { MarginInlineStart = Length.Px(20), MarginBlockStart = Length.Px(3) });

        style.MarginInlineStart.ValueOrNull()!.Value.Value.ShouldBe(10f);
        style.MarginBlockStart.ValueOrNull()!.Value.Value.ShouldBe(3f);
    }

    [Fact]
    public void Should_InheritDirectionAndWritingMode_FromParent()
    {
        var resolver = new StyleResolver();
        var parent = new DivElement
        {
            Style = new Style { Direction = Direction.Rtl, WritingMode = WritingMode.VerticalRl },
        };
        var child = new SpanElement();
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };
        var childComputed = resolver.Resolve(child, []);

        childComputed.Direction.ShouldBe(Direction.Rtl);
        childComputed.WritingMode.ShouldBe(WritingMode.VerticalRl);
    }

    [Fact]
    public void Should_MapChildLogicalMargin_ByInheritedDirection()
    {
        var resolver = new StyleResolver();
        var parent = new DivElement { Style = new Style { Direction = Direction.Rtl } };
        var child = new SpanElement { Style = new Style { MarginInlineStart = Length.Px(12) } };
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };
        var childComputed = resolver.Resolve(child, []);

        childComputed.MarginRight.Value.ShouldBe(12f);
        childComputed.MarginLeft.Value.ShouldBe(0f);
    }

    [Fact]
    public void Should_AllowChildToOverrideInheritedDirection()
    {
        var resolver = new StyleResolver();
        var parent = new DivElement { Style = new Style { Direction = Direction.Rtl } };
        var child = new SpanElement
        {
            Style = new Style { Direction = Direction.Ltr, MarginInlineStart = Length.Px(12) },
        };
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };
        var childComputed = resolver.Resolve(child, []);

        childComputed.Direction.ShouldBe(Direction.Ltr);
        childComputed.MarginLeft.Value.ShouldBe(12f);
    }

    [Fact]
    public void Should_ResolveInheritKeyword_OnLogicalMargin()
    {
        var resolver = new StyleResolver();
        var parent = new DivElement { Style = new Style { MarginLeft = Length.Px(8) } };
        var child = new SpanElement { Style = new Style { MarginInlineStart = StyleKeyword.Inherit } };
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };
        var childComputed = resolver.Resolve(child, []);

        // margin-inline-start: inherit 继承父元素 margin-inline-start 的计算值；
        // 父元素物理 margin-left=8 在默认书写模式下即 inline-start。
        childComputed.MarginInlineStart.Value.ShouldBe(8f);
        childComputed.MarginLeft.Value.ShouldBe(8f);
    }

    [Fact]
    public void Should_ResolveUnsetKeyword_OnDirection_AsInherit()
    {
        var resolver = new StyleResolver();
        var parent = new DivElement { Style = new Style { Direction = Direction.Rtl } };
        var child = new SpanElement { Style = new Style { Direction = StyleKeyword.Unset } };
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };
        var childComputed = resolver.Resolve(child, []);

        // direction 可继承：unset 等价于 inherit。
        childComputed.Direction.ShouldBe(Direction.Rtl);
    }

    #endregion
}
