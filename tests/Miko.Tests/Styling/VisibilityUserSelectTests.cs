using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-088：Visibility 与 UserSelect 的解析、继承效果及引擎级行为。
/// </summary>
public class VisibilityUserSelectTests
{
    [Fact]
    public void FromStyle_Visibility_ResolvesAndDefaultsToVisible()
    {
        ComputedStyle.FromStyle(new Style { Visibility = Visibility.Hidden })
            .Visibility.ShouldBe(Visibility.Hidden);
        ComputedStyle.FromStyle(new Style()).Visibility.ShouldBe(Visibility.Visible);
    }

    [Fact]
    public void FromStyle_UserSelect_ResolvesAndDefaultsToAuto()
    {
        ComputedStyle.FromStyle(new Style { UserSelect = UserSelect.None })
            .UserSelect.ShouldBe(UserSelect.None);
        ComputedStyle.FromStyle(new Style()).UserSelect.ShouldBe(UserSelect.Auto);
    }

    [Fact]
    public void VisibilityHidden_KeepsLayoutSpace_UnlikeDisplayNone()
    {
        // visibility: hidden 的元素仍占据布局空间（与 display: none 相反）。
        var layoutEngine = new LayoutEngine();
        var container = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Px(200) } };
        var hidden = new DivElement { Style = new Style { Height = Length.Px(50), Visibility = Visibility.Hidden } };
        var below = new DivElement { Style = new Style { Height = Length.Px(30) } };
        container.AddChild(hidden);
        container.AddChild(below);

        var root = layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        // 隐藏元素仍占 50px 高，故后续元素从 Y=50 起。
        root.Children.Count.ShouldBe(2);
        root.Children[1].BoxModel.Content.Y.ShouldBe(50);
    }

    [Fact]
    public void IsSelectable_ReflectsUserSelectNone_AfterLayout()
    {
        var target = new DivElement
        {
            Class = "no-select",
            Style = new Style { UserSelect = UserSelect.None, Height = Length.Px(20) }
        };
        var root = new DivElement { Children = { target } };

        using var surface = SKSurface.Create(new SKImageInfo(400, 400));
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, 400, 400);

        target.IsSelectable.ShouldBeFalse();
        root.IsSelectable.ShouldBeTrue();  // 默认 auto → 可选
    }

    [Fact]
    public void IsSelectable_ChildInheritsUserSelectNoneFromParent()
    {
        // user-select: none 通过级联传播到子元素 → 子元素也不可选。
        var child = new SpanElement { Style = new Style { Height = Length.Px(10) } };
        var parent = new DivElement
        {
            Style = new Style { UserSelect = UserSelect.None, Height = Length.Px(40) },
            Children = { child }
        };
        var root = new DivElement { Children = { parent } };

        using var surface = SKSurface.Create(new SKImageInfo(400, 400));
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, 400, 400);

        child.IsSelectable.ShouldBeFalse();
    }
}
