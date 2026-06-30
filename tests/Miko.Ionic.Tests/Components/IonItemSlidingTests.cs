using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonItemSlidingTests : IonicComponentTestBase
{
    private static readonly RenderFragment MinimalChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.CloseElement();
    };

    [Fact]
    public void IonItemSliding_RendersWithCorrectClass_WhenClosed()
    {
        var cut = Context.Render<IonItemSliding>(parameters =>
            parameters.Add(nameof(IonItemSliding.ChildContent), MinimalChild));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-item-sliding");
    }

    [Fact]
    public void IonItemSliding_StampsOpenClass_WhenOpen_DefaultEndSide()
    {
        var cut = Context.Render<IonItemSliding>(parameters =>
        {
            parameters.Add(nameof(IonItemSliding.Open), true);
            parameters.Add(nameof(IonItemSliding.ChildContent), MinimalChild);
        });

        // State assertion: open with the default end side reveals end options.
        cut.Root.Class.ShouldBe("md ion-item-sliding item-sliding-open item-sliding-open-end");
    }

    [Fact]
    public void IonItemSliding_StampsStartOpenClass_WhenOpenSideStart()
    {
        var cut = Context.Render<IonItemSliding>(parameters =>
        {
            parameters.Add(nameof(IonItemSliding.Open), true);
            parameters.Add(nameof(IonItemSliding.OpenSide), "start");
            parameters.Add(nameof(IonItemSliding.ChildContent), MinimalChild);
        });

        cut.Root.Class.ShouldBe("md ion-item-sliding item-sliding-open item-sliding-open-start");
    }

    [Fact]
    public void IonItemSliding_ClipsOverflow_FromStylesheet()
    {
        // Key style assertion: the host clips so the off-screen options stay hidden.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItemSliding>(parameters =>
            parameters.Add(nameof(IonItemSliding.ChildContent), MinimalChild));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.OverflowX.ShouldBe(Miko.Common.Overflow.Hidden);
    }
}
