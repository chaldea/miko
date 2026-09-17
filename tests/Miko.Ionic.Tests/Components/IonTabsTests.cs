using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Routing;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-tabs</c> default navigation (issues/ion-animation): IonTabs owns the tab
/// router — a click on a nested <see cref="IonTabButton"/> with an <c>Href</c> performs a
/// root-level navigation (history stack cleared, no page transition), mirroring Ionic's tab
/// switching.
/// </summary>
public class IonTabsTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    private (NavigationManager Nav, Func<NavigationEventArgs?> LastArgs) UseNavigation()
    {
        var nav = new NavigationManager();
        NavigationEventArgs? last = null;
        nav.LocationChanged += e => last = e;
        Context.Services.AddSingleton(nav);
        return (nav, () => last);
    }

    // Renders an IonTabs whose bottom slot holds a single tab button.
    private ComponentUnderTest RenderTabs(string? href, Action<ComponentParameterBuilder<IonTabs>>? configure = null)
        => Context.Render<IonTabs>(p =>
        {
            p.Add(nameof(IonTabs.Content), (RenderFragment)(b => b.AddContent("tab content")));
            p.Add(nameof(IonTabs.Bottom), (RenderFragment)(bar =>
            {
                var __c1 = bar.OpenComponent<IonTabBar>();
                __c1.ChildContent = (RenderFragment)(bb =>
                {
                    var __c5 = bb.OpenComponent<IonTabButton>();
                    __c5.Tab = "tab1";
                    if (href is not null)
                        __c5.Href = href;
                    __c5.ChildContent = Text("Favorites");
                    bb.CloseComponent();
                });
                bar.CloseComponent();
            }));
            configure?.Invoke(p);
        });

    private static void ClickTabButton(ComponentUnderTest cut)
    {
        var button = cut.FindByClass("ion-tab-button").Single();
        button.OnClick!.Invoke(new MouseEventArgs { Target = button });
    }

    [Fact]
    public void TabButtonClick_InsideTabs_NavigatesRoot_WithoutTransition()
    {
        var (nav, lastArgs) = UseNavigation();
        nav.NavigateTo("/somewhere");

        var cut = RenderTabs("/tab1");
        ClickTabButton(cut);

        // Root switch: stack cleared down to the target, no transition.
        nav.CurrentPath.ShouldBe("/tab1");
        nav.History.ShouldBe(new[] { "/tab1" });
        var args = lastArgs()!;
        args.Direction.ShouldBe(NavigationDirection.Root);
        args.Transition.ShouldBeNull();
    }

    [Fact]
    public void TabButtonClick_WithoutHref_DoesNotNavigate()
    {
        var (nav, _) = UseNavigation();

        var cut = RenderTabs(null);
        ClickTabButton(cut);

        nav.CurrentPath.ShouldBe("/");
    }

    [Fact]
    public void TabButtonClick_Standalone_DoesNotNavigate()
    {
        // No IonTabs ancestor: the button only raises OnClick, navigation is owned by IonTabs.
        var (nav, _) = UseNavigation();

        var cut = Context.Render<IonTabButton>(p =>
        {
            p.Add(nameof(IonTabButton.Tab), "tab1");
            p.Add(nameof(IonTabButton.Href), "/tab1");
            p.Add(nameof(IonTabButton.ChildContent), Text("Favorites"));
        });

        cut.Root.OnClick!.Invoke(new MouseEventArgs { Target = cut.Root });

        nav.CurrentPath.ShouldBe("/");
    }

    [Fact]
    public void TabButtonClick_InvokesOnClick_BeforeRootNavigation()
    {
        var (nav, _) = UseNavigation();
        var clicked = false;

        var cut = Context.Render<IonTabs>(p =>
        {
            p.Add(nameof(IonTabs.Content), (RenderFragment)(b => b.AddContent("tab content")));
            p.Add(nameof(IonTabs.Bottom), (RenderFragment)(bar =>
            {
                var __c2 = bar.OpenComponent<IonTabButton>();
                __c2.Tab = "tab1";
                __c2.Href = "/tab1";
                __c2.OnClick = 
                    EventCallback.Factory.Create(this, () => clicked = true);
                __c2.ChildContent = Text("Favorites");
                bar.CloseComponent();
            }));
        });

        ClickTabButton(cut);

        clicked.ShouldBeTrue();
        nav.CurrentPath.ShouldBe("/tab1");
    }

    [Fact]
    public void Tabs_KeepsDomContract_WithCascadingBottomSlot()
    {
        // The cascading value wrapper is transparent: tabs-inner content + tab bar are still
        // direct children of the ion-tabs host.
        var cut = RenderTabs("/tab1");

        cut.Root.ShouldHaveClass("ion-tabs");
        cut.FindByClass("tabs-inner").ShouldHaveSingleItem();
        cut.FindByClass("ion-tab-button").ShouldHaveSingleItem();
    }

    [Fact]
    public void Tabs_RendersTopContentBeforeInnerAndBottomContentAfterInner()
    {
        var cut = Context.Render<IonTabs>(p =>
        {
            p.Add(nameof(IonTabs.Top), (RenderFragment)(top =>
            {
                var __c3 = top.OpenComponent<IonTabBar>();
                __c3.Slot = "top";
                top.CloseComponent();
            }));
            p.Add(nameof(IonTabs.Content), (RenderFragment)(content =>
                content.AddContent("content")));
            p.Add(nameof(IonTabs.Bottom), (RenderFragment)(bottom =>
            {
                var __c4 = bottom.OpenComponent<IonTabBar>();
                __c4.Slot = "bottom";
                bottom.CloseComponent();
            }));
        });

        var directStructure = cut.GetAllElements()
            .Where(element => element.HasClass("ion-tab-bar-top")
                || element.HasClass("tabs-inner")
                || element.HasClass("ion-tab-bar-bottom"))
            .Select(element => element.Class)
            .ToArray();

        directStructure.ShouldBe(new[] { "md ion-tab-bar ion-tab-bar-top", "tabs-inner", "md ion-tab-bar ion-tab-bar-bottom" });
    }
}
