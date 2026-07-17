using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonItemGroupTests : IonicComponentTestBase
{
    private static readonly RenderFragment MinimalChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.CloseElement();
    };

    [Fact]
    public void IonItemGroup_RendersWithCorrectClass()
    {
        var cut = Context.Render<IonItemGroup>(parameters =>
            parameters.Add(nameof(IonItemGroup.ChildContent), MinimalChild));

        cut.Root.TagName.ShouldBe("div");
        // Host carries ion-item-group plus Ionic's internal item-group-{mode} styling class.
        cut.Root.Class.ShouldBe("md ion-item-group item-group-md");
    }

    [Fact]
    public void IonItemGroup_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonItemGroup>(parameters =>
            parameters.Add(nameof(IonItemGroup.ChildContent), MinimalChild));

        cut.Root.Class.ShouldBe("ios ion-item-group item-group-ios");
    }

    [Fact]
    public void IonItemGroup_RendersChildContent()
    {
        var cut = Context.Render<IonItemGroup>(parameters =>
            parameters.Add(nameof(IonItemGroup.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Section A");
                builder.CloseElement();
            })));

        cut.GetTextContent().ShouldContain("Section A");
    }
}
