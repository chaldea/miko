using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonNoteTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonNote_RendersDefaultDom()
    {
        var cut = Context.Render<IonNote>(p => p.AddChildContent(Text("3 unread")));

        cut.Root.TagName.ShouldBe("span");
        cut.Root.Class.ShouldBe("md ion-note");
        cut.GetTextContent().ShouldBe("3 unread");
    }

    [Fact]
    public void IonNote_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonNote>(p => p.AddChildContent(Text("note")));

        cut.Root.Class.ShouldBe("ios ion-note");
    }

    [Fact]
    public void IonNote_StampsColorClass()
    {
        var cut = Context.Render<IonNote>(p =>
        {
            p.Add(nameof(IonNote.Color), "danger");
            p.AddChildContent(Text("!"));
        });

        cut.Root.Class.ShouldBe("md ion-note ion-color ion-color-danger");
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonNote_DefaultStyle_UsesMdGrayText()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonNote>(p => p.AddChildContent(Text("note")));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.InlineBlock);
        style.Color.ShouldBe(Color.FromHex("666666"));
        style.FontSize.ShouldBe(Length.Px(14));
    }

    [Fact]
    public void IonNote_IosStyle_UsesLighterGrayText()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonNote>(p => p.AddChildContent(Text("note")));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Color.ShouldBe(Color.FromHex("a6a6a6"));
    }

    [Fact]
    public void IonNote_ColorClass_OverridesTextColor()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonNote>(p =>
        {
            p.Add(nameof(IonNote.Color), "primary");
            p.AddChildContent(Text("note"));
        });

        cut.GetComputedStyle(cut.Root)!.Color.ShouldBe(Color.FromHex("0054e9"));
    }
}
