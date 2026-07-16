using Miko.Common;
using Miko.Components;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-fab</c>. Covers the DOM contract, the horizontal/vertical/edge positioning
/// class stamping, the absolute host style, and the activated-state cascade to a nested main button
/// and fab-list.
/// </summary>
public class IonFabTests : IonicComponentTestBase
{
    // A minimal main fab-button so the fab has renderable content (CascadingValue needs an element).
    private static RenderFragment MainButton() => builder =>
    {
        builder.OpenComponent<IonFabButton>(0);
        builder.CloseComponent();
    };

    // A fab with a main button and a bottom fab-list containing one list button. Used to exercise
    // the activated cascade end-to-end.
    private static RenderFragment ButtonWithList() => builder =>
    {
        builder.OpenComponent<IonFabButton>(0);
        builder.CloseComponent();

        builder.OpenComponent<IonFabList>(1);
        builder.AddComponentParameter(2, nameof(IonFabList.Side), "bottom");
        builder.AddComponentParameter(3, nameof(IonFabList.ChildContent), (RenderFragment)(lb =>
        {
            lb.OpenComponent<IonFabButton>(0);
            lb.CloseComponent();
        }));
        builder.CloseComponent();
    };

    private static ComponentUnderTest RenderFab(TestContext ctx,
        RenderFragment child,
        Action<ComponentParameterBuilder<IonFab>>? configure = null)
        => ctx.Render<IonFab>(p =>
        {
            p.Add(nameof(IonFab.ChildContent), child);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonFab_RendersDomContract()
    {
        var cut = RenderFab(Context, MainButton());

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-fab");
        // The main fab-button is cascaded through as a child.
        cut.FindByClass("ion-fab-button").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonFab_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderFab(Context, MainButton());

        cut.Root.Class.ShouldStartWith("ios ion-fab");
    }

    // ---- Positioning classes ----------------------------------------------

    [Theory]
    [InlineData("start", "fab-horizontal-start")]
    [InlineData("end", "fab-horizontal-end")]
    [InlineData("center", "fab-horizontal-center")]
    public void IonFab_StampsHorizontalClass(string value, string expected)
    {
        var cut = RenderFab(Context, MainButton(), p => p.Add(nameof(IonFab.Horizontal), value));

        cut.Root.ShouldHaveClass(expected);
    }

    [Theory]
    [InlineData("top", "fab-vertical-top")]
    [InlineData("bottom", "fab-vertical-bottom")]
    [InlineData("center", "fab-vertical-center")]
    public void IonFab_StampsVerticalClass(string value, string expected)
    {
        var cut = RenderFab(Context, MainButton(), p => p.Add(nameof(IonFab.Vertical), value));

        cut.Root.ShouldHaveClass(expected);
    }

    [Fact]
    public void IonFab_Edge_StampsEdgeClass()
    {
        var cut = RenderFab(Context, MainButton(), p => p.Add(nameof(IonFab.Edge), true));

        cut.Root.ShouldHaveClass("fab-edge");
    }

    // ---- Key style ---------------------------------------------------------

    [Fact]
    public void IonFab_Style_HostIsAbsolute()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderFab(Context, MainButton());

        cut.GetComputedStyle(cut.Root)!.Position.ShouldBe(Position.Absolute);
    }

    // The fab's fit-content shrink-wrap is an engine behavior verified against the real child-layout
    // path in Miko.Tests (AbsoluteShrinkToFitTests) — rendering the fab here would make it the layout
    // root (viewport-sized), which doesn't exercise the out-of-flow child sizing.

    // ---- Activated cascade -------------------------------------------------

    [Fact]
    public void IonFab_Activated_CascadesCloseActiveToMainButton()
    {
        var cut = RenderFab(Context, ButtonWithList(), p => p.Add(nameof(IonFab.Activated), true));

        // The main button (not in a list) gets the close-active class from the fab context.
        var mainButton = cut.FindByClass("ion-fab-button")
            .First(b => !b.HasClass("fab-button-in-list"));
        mainButton.ShouldHaveClass("fab-button-close-active");
    }

    [Fact]
    public void IonFab_Activated_CascadesActiveToList()
    {
        var cut = RenderFab(Context, ButtonWithList(), p => p.Add(nameof(IonFab.Activated), true));

        cut.FindByClass("ion-fab-list").Single().ShouldHaveClass("fab-list-active");
    }

    [Fact]
    public void IonFab_NotActivated_ListIsInactive()
    {
        var cut = RenderFab(Context, ButtonWithList());

        cut.FindByClass("ion-fab-list").Single().ShouldNotHaveClass("fab-list-active");
        var mainButton = cut.FindByClass("ion-fab-button")
            .First(b => !b.HasClass("fab-button-in-list"));
        mainButton.ShouldNotHaveClass("fab-button-close-active");
    }
}
