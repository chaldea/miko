using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonTextTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonText_RendersDefaultDom()
    {
        var cut = Context.Render<IonText>(p => p.AddChildContent(Text("Hello")));

        cut.Root.TagName.ShouldBe("span");
        cut.Root.Class.ShouldBe("md ion-text");
        cut.GetTextContent().ShouldBe("Hello");
    }

    [Fact]
    public void IonText_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonText>(p => p.AddChildContent(Text("Hello")));

        cut.Root.Class.ShouldBe("ios ion-text");
    }

    [Fact]
    public void IonText_StampsColorClass()
    {
        var cut = Context.Render<IonText>(p =>
        {
            p.Add(nameof(IonText.Color), "primary");
            p.AddChildContent(Text("Hello"));
        });

        cut.Root.Class.ShouldBe("md ion-text ion-color ion-color-primary");
    }

    [Fact]
    public void IonText_NoColor_OmitsColorClasses()
    {
        var cut = Context.Render<IonText>(p => p.AddChildContent(Text("Hello")));

        cut.Root.ShouldNotHaveClass("ion-color");
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonText_ColorClass_SetsTextColor()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonText>(p =>
        {
            p.Add(nameof(IonText.Color), "success");
            p.AddChildContent(Text("Hello"));
        });

        cut.GetComputedStyle(cut.Root)!.Color.ShouldBe(Color.FromHex("2dd55b"));
    }

    [Fact]
    public void IonText_DangerColor_SetsDangerText()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonText>(p =>
        {
            p.Add(nameof(IonText.Color), "danger");
            p.AddChildContent(Text("Hello"));
        });

        cut.GetComputedStyle(cut.Root)!.Color.ShouldBe(Color.FromHex("c5000f"));
    }
}
