using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonItemDividerTests : IonicComponentTestBase
{
    private static readonly RenderFragment MinimalChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.CloseElement();
    };

    [Fact]
    public void IonItemDivider_RendersWithCorrectClass()
    {
        var cut = Context.Render<IonItemDivider>(parameters =>
            parameters.Add(nameof(IonItemDivider.ChildContent), MinimalChild));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-item-divider");
    }

    [Fact]
    public void IonItemDivider_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonItemDivider>(parameters =>
            parameters.Add(nameof(IonItemDivider.ChildContent), MinimalChild));

        cut.Root.Class.ShouldBe("ios ion-item-divider");
    }

    [Fact]
    public void IonItemDivider_HasTintedBackground_FromTheme()
    {
        // Key style assertion: the divider carries a fill distinct from the page background.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItemDivider>(parameters =>
            parameters.Add(nameof(IonItemDivider.ChildContent), MinimalChild));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        // md divider background is #f2f2f2 (step-50), not the white page background.
        style.BackgroundColor.ShouldBe(Miko.Common.Color.FromHex("f2f2f2"));
    }

    [Fact]
    public void IonItemDivider_RendersChildContent()
    {
        var cut = Context.Render<IonItemDivider>(parameters =>
            parameters.Add(nameof(IonItemDivider.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Section A");
                builder.CloseElement();
            })));

        cut.GetTextContent().ShouldContain("Section A");
    }
}
