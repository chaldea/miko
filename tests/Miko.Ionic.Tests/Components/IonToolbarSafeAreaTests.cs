using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-064: <c>ion-toolbar</c>'s top safe-area inset must apply ONLY to the first toolbar in a
/// header (Ionic's <c>ion-header ion-toolbar:first-of-type { padding-top: safe-area-top }</c>).
/// Every toolbar still clears a side notch via the left/right insets, and a toolbar outside a
/// header never gets the top inset.
/// </summary>
public class IonToolbarSafeAreaTests : IonicComponentTestBase
{
    // A non-zero inset so the env(safe-area-inset-*) lengths resolve to observable px.
    private static readonly SafeAreaInsets Insets = new(Left: 10, Top: 44, Right: 12, Bottom: 0);

    public IonToolbarSafeAreaTests()
    {
        // Mode-scoped Ionic rules (md + ios); the test base defaults the platform to md.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.SafeArea = Insets;
    }

    // ChildContent that emits two <IonToolbar/> components inside the header.
    private static readonly RenderFragment TwoToolbars = builder =>
    {
        builder.OpenComponent<IonToolbar>(0);
        builder.CloseComponent();
        builder.OpenComponent<IonToolbar>(1);
        builder.CloseComponent();
    };

    private ComponentUnderTest RenderHeader() =>
        Context.Render<IonHeader>(p => p.AddChildContent(TwoToolbars));

    private static List<Element> Toolbars(ComponentUnderTest cut)
    {
        var toolbars = cut.FindByClass("ion-toolbar");
        toolbars.Count.ShouldBe(2);
        return toolbars;
    }

    [Fact]
    public void FirstToolbarInHeader_GetsTopSafeAreaInset()
    {
        var cut = RenderHeader();
        var first = Toolbars(cut)[0];

        // The first toolbar sits under the status bar: padding-top == safe-area-top.
        cut.GetComputedStyle(first)!.PaddingTop.Value.ShouldBe(Insets.Top);
    }

    [Fact]
    public void SecondToolbarInHeader_HasNoTopSafeAreaInset()
    {
        var cut = RenderHeader();
        var second = Toolbars(cut)[1];

        // Subsequent toolbars are not under the status bar: zero top padding.
        cut.GetComputedStyle(second)!.PaddingTop.Value.ShouldBe(0f);
    }

    [Fact]
    public void AllToolbarsInHeader_GetSideSafeAreaInsets()
    {
        var cut = RenderHeader();

        // Every toolbar clears a side notch via the left/right insets, regardless of order.
        foreach (var tb in Toolbars(cut))
        {
            var style = cut.GetComputedStyle(tb)!;
            style.PaddingLeft.Value.ShouldBe(Insets.Left);
            style.PaddingRight.Value.ShouldBe(Insets.Right);
        }
    }

    [Fact]
    public void StandaloneToolbar_OutsideHeader_HasNoTopInset()
    {
        var cut = Context.Render<IonToolbar>();

        // No enclosing header → the first-of-type header rule never applies.
        var style = cut.GetComputedStyle(cut.Root)!;
        style.PaddingTop.Value.ShouldBe(0f);
        // Side insets still apply (toolbar.scss :host).
        style.PaddingLeft.Value.ShouldBe(Insets.Left);
        style.PaddingRight.Value.ShouldBe(Insets.Right);
    }
}
