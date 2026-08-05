using Miko.Animation;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Styling;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Computed-style contracts for the FAB family that the reported defects (issues/ion-fab.md) turned
/// on: the <c>fit-content</c> host that makes the centering rules work, the edge offsets that hang
/// the button half off the content, and the transform/opacity transitions that animate the reveal
/// instead of popping it.
/// </summary>
public class IonFabStyleTests : IonicComponentTestBase
{
    public IonFabStyleTests()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
    }

    // A fab with a main button and one fab-list (side configurable).
    private ComponentUnderTest RenderFab(
        string? horizontal = null, string? vertical = null, bool edge = false,
        string listSide = "bottom", bool activated = false, string? buttonSize = null)
        => Context.Render<IonFab>(p =>
        {
            if (horizontal != null) p.Add(nameof(IonFab.Horizontal), horizontal);
            if (vertical != null) p.Add(nameof(IonFab.Vertical), vertical);
            p.Add(nameof(IonFab.Edge), edge);
            p.Add(nameof(IonFab.Activated), activated);
            p.Add(nameof(IonFab.ChildContent), (RenderFragment)(fab =>
            {
                fab.OpenComponent<IonFabButton>(0);
                if (buttonSize != null)
                    fab.AddComponentParameter(1, nameof(IonFabButton.Size), buttonSize);
                fab.CloseComponent();

                fab.OpenComponent<IonFabList>(10);
                fab.AddComponentParameter(11, nameof(IonFabList.Side), listSide);
                fab.AddComponentParameter(12, nameof(IonFabList.ChildContent), (RenderFragment)(l =>
                {
                    l.OpenComponent<IonFabButton>(0);
                    l.CloseComponent();
                }));
                fab.CloseComponent();
            }));
        });

    private static Element MainButton(ComponentUnderTest cut)
        => cut.FindByClass("ion-fab-button").First(b => !b.HasClass("fab-button-in-list"));

    private static Element ListButton(ComponentUnderTest cut)
        => cut.FindByClass("fab-button-in-list").Single();

    // ---- the fit-content host ----

    [Fact]
    public void FabHost_IsFitContentOnBothAxes()
    {
        // fab.scss `:host { width: fit-content; height: fit-content }`. This is what keeps the
        // centering rules' opposite insets from stretching the host across the content area — the
        // regression that put the centred fabs in the corner and made them eat every tap.
        var cut = RenderFab(horizontal: "center", vertical: "center");
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Width.IsFitContent.ShouldBeTrue();
        style.Height.IsFitContent.ShouldBeTrue();
    }

    [Fact]
    public void CentredFab_PinsBothInsetsAndTakesAutoMargins()
    {
        var cut = RenderFab(horizontal: "center", vertical: "center");
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Left.ToPixels(100).ShouldBe(0f);
        style.Right.ToPixels(100).ShouldBe(0f);
        style.MarginLeft.IsAuto.ShouldBeTrue();
        style.MarginRight.IsAuto.ShouldBeTrue();
        style.Top.ToPixels(100).ShouldBe(0f);
        style.Bottom.ToPixels(100).ShouldBe(0f);
        style.MarginTop.IsAuto.ShouldBeTrue();
        style.MarginBottom.IsAuto.ShouldBeTrue();
    }

    // ---- edge offsets ----

    [Fact]
    public void EdgeTopFab_PullsItsButtonUpByHalfItsHeight()
    {
        // fab.scss `:host(.fab-vertical-top.fab-edge) ::slotted(ion-fab-button) { margin-top: -50% }`
        // — the rule that was missing, so the fab sat wholly inside the content.
        var cut = RenderFab(horizontal: "end", vertical: "top", edge: true);

        var style = cut.GetComputedStyle(MainButton(cut))!;
        // Percentage margins resolve against the containing block's width (the fit-content host,
        // i.e. the 56px button), so -50% is -28px.
        style.MarginTop.ToPixels(56f).ShouldBe(-28f, 0.5f);
    }

    [Fact]
    public void EdgeBottomFab_PushesItsButtonDownByHalfItsHeight()
    {
        var cut = RenderFab(horizontal: "end", vertical: "bottom", edge: true);

        cut.GetComputedStyle(MainButton(cut))!.MarginBottom.ToPixels(56f).ShouldBe(-28f, 0.5f);
    }

    [Fact]
    public void EdgeOffset_FoldsInASmallButtonsOwnMargin()
    {
        // A small button already carries 8px top/bottom margin, and the edge rule overwrites
        // margin-top outright — so it uses (-100% + 2*8px)/2 instead of a plain -50%.
        var cut = RenderFab(horizontal: "end", vertical: "top", edge: true, buttonSize: "small");

        // Against the 40px small button: (-40 + 16) / 2 = -12.
        cut.GetComputedStyle(MainButton(cut))!.MarginTop.ToPixels(40f).ShouldBe(-12f, 0.5f);
    }

    [Fact]
    public void EdgeOffset_DoesNotReachAListsButtons()
    {
        // Ionic's ::slotted only crosses one level; a list button must keep its own 8px margin
        // rather than inheriting the host's -50% lift.
        var cut = RenderFab(horizontal: "end", vertical: "top", edge: true, activated: true);

        cut.GetComputedStyle(ListButton(cut))!.MarginTop.ToPixels(56f).ShouldBe(8f, 0.5f);
    }

    [Fact]
    public void NonEdgeFab_LeavesItsButtonUnshifted()
    {
        var cut = RenderFab(horizontal: "end", vertical: "top");

        cut.GetComputedStyle(MainButton(cut))!.MarginTop.ToPixels(56f).ShouldBe(0f, 0.5f);
    }

    // ---- reveal animation ----

    // A closed fab-list is display:none and so has no layout box at all (that absence is asserted
    // in IonFabLayoutTests.ClosedList_HasNoHittableButtons); these therefore open the fab, which is
    // the state the reveal has to land in.

    [Theory]
    [InlineData("bottom")]
    [InlineData("top")]
    [InlineData("start")]
    [InlineData("end")]
    public void ShownListButton_IsFullyOpaque_OnEverySide(string side)
    {
        // The start/end side rules used to re-declare the hidden opacity/transform at a specificity
        // that beat `.fab-button-show`, so a horizontal list stayed invisible however far it opened.
        // They now override margins only, and every side reveals the same way.
        var cut = RenderFab(horizontal: "end", vertical: "bottom", listSide: side, activated: true);

        cut.GetComputedStyle(ListButton(cut))!.Opacity.ShouldBe(1f);
    }

    [Theory]
    [InlineData("bottom")]
    [InlineData("top")]
    [InlineData("start")]
    [InlineData("end")]
    public void ListButton_TransitionsItsTransformAndOpacity(string side)
    {
        // fab-button.scss `transition: all ease-in-out 300ms; transition-property: transform,
        // opacity` — without it the list pops in instead of scaling up.
        var cut = RenderFab(horizontal: "end", vertical: "bottom", listSide: side, activated: true);

        ShouldRevealTransition(cut.GetComputedStyle(ListButton(cut))!);
    }

    [Fact]
    public void CloseIconAndButtonInner_TransitionTheirTransformAndOpacity()
    {
        // The main button's cross-fade between its content and the close glyph.
        var cut = RenderFab(horizontal: "end", vertical: "top");
        var main = MainButton(cut);

        ShouldRevealTransition(cut.GetComputedStyle(main.FindByClass("close-icon").Single())!);
        ShouldRevealTransition(cut.GetComputedStyle(main.FindByClass("button-inner").Single())!);
    }

    private static void ShouldRevealTransition(ComputedStyle style)
    {
        style.Transitions.ShouldNotBeNull();
        var properties = style.Transitions!.Select(t => t.Property).ToList();
        properties.ShouldContain(nameof(Style.Transform));
        properties.ShouldContain(nameof(Style.Opacity));
        foreach (var transition in style.Transitions!)
        {
            transition.Duration.ShouldBeGreaterThan(0f);
            transition.TimingFunction.ShouldBe(TimingFunction.EaseInOut);
        }
    }
}
