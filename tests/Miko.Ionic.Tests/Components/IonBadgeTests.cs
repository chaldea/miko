using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonBadgeTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    [Fact]
    public void IonBadge_RendersDefaultDom()
    {
        var cut = Context.Render<IonBadge>(p => p.AddChildContent(Text("7")));

        cut.Root.TagName.ShouldBe("span");
        cut.Root.Class.ShouldBe("md ion-badge");
        cut.GetTextContent().ShouldBe("7");
    }

    [Fact]
    public void IonBadge_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonBadge>(p => p.AddChildContent(Text("1")));

        cut.Root.Class.ShouldBe("ios ion-badge");
    }

    [Fact]
    public void IonBadge_StampsColorClass()
    {
        var cut = Context.Render<IonBadge>(p =>
        {
            p.Add(nameof(IonBadge.Color), "danger");
            p.AddChildContent(Text("!"));
        });

        cut.Root.Class.ShouldBe("md ion-badge ion-color ion-color-danger");
    }

    [Fact]
    public void IonBadge_DefaultStyle_UsesPrimaryFill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonBadge>(p => p.AddChildContent(Text("3")));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.InlineBlock);
        style.BackgroundColor.ShouldBe(Color.FromHex("0054e9"));
        style.Color.ShouldBe(Color.White);
        style.BorderTopLeftRadius.Value.ShouldBe(4f);
    }

    [Fact]
    public void IonBadge_IosStyle_UsesRounderRadius()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonBadge>(p => p.AddChildContent(Text("3")));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.BorderTopLeftRadius.Value.ShouldBe(10f);
    }

    [Fact]
    public void IonBadge_DangerColor_UsesDangerFill()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonBadge>(p =>
        {
            p.Add(nameof(IonBadge.Color), "danger");
            p.AddChildContent(Text("!"));
        });

        cut.GetComputedStyle(cut.Root)!.BackgroundColor.ShouldBe(Color.FromHex("c5000f"));
    }
}
