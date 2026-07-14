using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-088：新增可继承属性（text-transform、letter-spacing、overflow-wrap、
/// word-break、visibility、user-select）应从父元素级联继承。
/// </summary>
public class InheritanceTests
{
    private static ComputedStyle ResolveChildUnder(Style parentStyle)
    {
        var resolver = new StyleResolver();
        var parent = new DivElement { Style = parentStyle };
        var child = new SpanElement();
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };

        return resolver.Resolve(child, []);
    }

    [Fact]
    public void TextTransform_InheritsFromParent()
    {
        ResolveChildUnder(new Style { TextTransform = TextTransform.Uppercase })
            .TextTransform.ShouldBe(TextTransform.Uppercase);
    }

    [Fact]
    public void LetterSpacing_InheritsFromParent()
    {
        ResolveChildUnder(new Style { LetterSpacing = Length.Px(3) })
            .LetterSpacing.Value.ShouldBe(3f);
    }

    [Fact]
    public void OverflowWrap_InheritsFromParent()
    {
        ResolveChildUnder(new Style { OverflowWrap = OverflowWrap.BreakWord })
            .OverflowWrap.ShouldBe(OverflowWrap.BreakWord);
    }

    [Fact]
    public void WordBreak_InheritsFromParent()
    {
        ResolveChildUnder(new Style { WordBreak = WordBreak.BreakAll })
            .WordBreak.ShouldBe(WordBreak.BreakAll);
    }

    [Fact]
    public void Visibility_InheritsFromParent()
    {
        ResolveChildUnder(new Style { Visibility = Visibility.Hidden })
            .Visibility.ShouldBe(Visibility.Hidden);
    }

    [Fact]
    public void UserSelect_InheritsFromParent()
    {
        ResolveChildUnder(new Style { UserSelect = UserSelect.None })
            .UserSelect.ShouldBe(UserSelect.None);
    }

    [Fact]
    public void Visibility_ChildCanOverrideHiddenParent()
    {
        // visibility 可继承，但子元素能用 visible 覆盖被隐藏的父元素。
        var resolver = new StyleResolver();
        var parent = new DivElement { Style = new Style { Visibility = Visibility.Hidden } };
        var child = new SpanElement { Style = new Style { Visibility = Visibility.Visible } };
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };

        resolver.Resolve(child, []).Visibility.ShouldBe(Visibility.Visible);
    }
}
