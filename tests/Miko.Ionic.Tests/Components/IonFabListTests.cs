using Miko.Common;
using Miko.Components;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-fab-list</c>. Covers the DOM contract, the side class stamping, the in-list
/// marker cascaded to child buttons, and the active/inactive display state (driven by the enclosing
/// fab's activation).
/// </summary>
public class IonFabListTests : IonicComponentTestBase
{
    // A fab-list containing one button.
    private static RenderFragment ListButton() => builder =>
    {
        builder.OpenComponent<IonFabButton>(0);
        builder.CloseComponent();
    };

    private static ComponentUnderTest RenderList(TestContext ctx,
        Action<ComponentParameterBuilder<IonFabList>>? configure = null)
        => ctx.Render<IonFabList>(p =>
        {
            p.Add(nameof(IonFabList.ChildContent), ListButton());
            configure?.Invoke(p);
        });

    // A fab wrapping a fab-list (so the list reads the fab's cascaded activation).
    private static ComponentUnderTest RenderListInFab(TestContext ctx, bool activated) =>
        ctx.Render<IonFab>(p =>
        {
            p.Add(nameof(IonFab.Activated), activated);
            p.Add(nameof(IonFab.ChildContent), (RenderFragment)(builder =>
            {
                // A main button so the fab has a list (Build detects the list) and can toggle.
                builder.OpenComponent<IonFabButton>(0);
                builder.CloseComponent();

                builder.OpenComponent<IonFabList>(1);
                builder.AddComponentParameter(2, nameof(IonFabList.ChildContent),
                    (RenderFragment)(lb =>
                    {
                        lb.OpenComponent<IonFabButton>(0);
                        lb.CloseComponent();
                    }));
                builder.CloseComponent();
            }));
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonFabList_RendersDomContract()
    {
        var cut = RenderList(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-fab-list");
        // Default side is bottom.
        cut.Root.ShouldHaveClass("fab-list-side-bottom");
    }

    [Theory]
    [InlineData("start", "fab-list-side-start")]
    [InlineData("end", "fab-list-side-end")]
    [InlineData("top", "fab-list-side-top")]
    [InlineData("bottom", "fab-list-side-bottom")]
    public void IonFabList_StampsSideClass(string side, string expected)
    {
        var cut = RenderList(Context, p => p.Add(nameof(IonFabList.Side), side));

        cut.Root.ShouldHaveClass(expected);
    }

    // ---- In-list marker cascade -------------------------------------------

    [Fact]
    public void IonFabList_MarksChildButtonsAsInList()
    {
        var cut = RenderList(Context);

        cut.FindByClass("ion-fab-button").Single().ShouldHaveClass("fab-button-in-list");
    }

    // ---- Active state ------------------------------------------------------

    [Fact]
    public void IonFabList_InActivatedFab_IsActive()
    {
        var cut = RenderListInFab(Context, activated: true);

        cut.FindByClass("ion-fab-list").Single().ShouldHaveClass("fab-list-active");
    }

    [Fact]
    public void IonFabList_InInactiveFab_IsNotActive()
    {
        var cut = RenderListInFab(Context, activated: false);

        cut.FindByClass("ion-fab-list").Single().ShouldNotHaveClass("fab-list-active");
    }

    [Fact]
    public void IonFabList_Activated_ShowsChildButtons()
    {
        // When the enclosing fab is activated the list buttons get the show class.
        var cut = RenderListInFab(Context, activated: true);

        var listButton = cut.FindByClass("ion-fab-button")
            .Single(b => b.HasClass("fab-button-in-list"));
        listButton.ShouldHaveClass("fab-button-show");
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonFabList_Style_InactiveIsHidden()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderListInFab(Context, activated: false);
        var list = cut.FindByClass("ion-fab-list").Single();

        // display:none removes the element from the layout tree, so it has no computed style / box —
        // the observable effect of the inactive list being hidden.
        cut.GetComputedStyle(list).ShouldBeNull();
        cut.FindLayoutBox(list).ShouldBeNull();
    }

    [Fact]
    public void IonFabList_Style_ActiveIsFlex()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderListInFab(Context, activated: true);
        var list = cut.FindByClass("ion-fab-list").Single();

        cut.GetComputedStyle(list)!.Display.ShouldBe(Display.Flex);
    }
}
