using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-153 (footer and tab bar): the footer's last toolbar was padded 0 instead of the bottom
/// safe area, <c>.ion-no-border</c> wrote <c>BoxShadow = null</c> ("not set") so the md shadow
/// stayed, and a top tab bar was padded for the bottom safe area instead of the top one.
/// </summary>
public class FooterAndTabBarSafeAreaTests : IonicComponentTestBase
{
    private static readonly SafeAreaInsets Insets = new(Left: 0, Top: 44, Right: 0, Bottom: 34);

    public FooterAndTabBarSafeAreaTests()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.SafeArea = Insets;
    }

    private static RenderFragment FooterWithToolbar(string? footerClass = null) => builder =>
    {
        var footer = builder.OpenComponent<IonFooter>();
        footer.Class = footerClass;
        footer.ChildContent = b =>
        {
            b.OpenComponent<IonToolbar>();
            b.CloseComponent();
        };
        builder.CloseComponent();
    };

    private static RenderFragment TabBar(string slot) => builder =>
    {
        var bar = builder.OpenComponent<IonTabBar>();
        bar.Slot = slot;
        builder.CloseComponent();
    };

    [Fact]
    public void FooterToolbar_ClearsTheBottomSafeArea()
    {
        var cut = Context.Render<IonPage>(p => p.AddChildContent(FooterWithToolbar()));

        var toolbar = cut.FindByClass("ion-toolbar").Single();
        cut.GetComputedStyle(toolbar)!.PaddingBottom.Value.ShouldBe(Insets.Bottom);
    }

    [Fact]
    public void FooterAboveABottomTabBar_LeavesTheSafeAreaToTheTabBar()
    {
        // footer.tsx: no footer-toolbar-padding when the enclosing ion-tabs has a bottom tab bar.
        var cut = Context.Render<IonTabs>(p => p
            .Add(nameof(IonTabs.Content), FooterWithToolbar())
            .Add(nameof(IonTabs.Bottom), TabBar("bottom")));

        var footer = cut.FindByClass("ion-footer").Single();
        footer.ShouldNotHaveClass("footer-toolbar-padding");
        cut.GetComputedStyle(footer.FindByClass("ion-toolbar").Single())!.PaddingBottom.Value.ShouldBe(0);
        cut.GetComputedStyle(cut.FindByClass("ion-tab-bar").Single())!.PaddingBottom.Value.ShouldBe(Insets.Bottom);
    }

    [Fact]
    public void FooterInTabsWithOnlyATopTabBar_StillPads()
    {
        var cut = Context.Render<IonTabs>(p => p
            .Add(nameof(IonTabs.Content), FooterWithToolbar())
            .Add(nameof(IonTabs.Top), TabBar("top")));

        cut.FindByClass("ion-footer").Single().ShouldHaveClass("footer-toolbar-padding");
    }

    [Fact]
    public void TopTabBar_PadsForTheTopSafeArea_NotTheBottom()
    {
        var cut = Context.Render<IonTabs>(p => p.Add(nameof(IonTabs.Top), TabBar("top")));

        var style = cut.GetComputedStyle(cut.FindByClass("ion-tab-bar").Single())!;
        style.PaddingTop.Value.ShouldBe(Insets.Top);
        style.PaddingBottom.Value.ShouldBe(0);
    }

    [Fact]
    public void NoBorderFooter_DropsTheMdShadow()
    {
        var cut = Context.Render<IonPage>(p => p.AddChildContent(FooterWithToolbar("ion-no-border")));
        var plain = Context.Render<IonPage>(p => p.AddChildContent(FooterWithToolbar()));

        ShadowCount(plain).ShouldBeGreaterThan(0);
        ShadowCount(cut).ShouldBe(0);
    }

    private static int ShadowCount(ComponentUnderTest cut)
    {
        var shadow = cut.GetComputedStyle(cut.FindByClass("ion-footer").Single())!.BoxShadow;
        return shadow?.Value?.Count ?? 0;
    }
}
