using Miko.Common;
using Miko.Components;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-action-sheet</c>. Covers the DOM contract (backdrop + wrapper + container +
/// group; the title/sub-title; per-button structure), the cancel-button split into its own group,
/// role/state class stamping, the open/closed <c>overlay-hidden</c> gating, and the dismiss
/// callbacks (button tap with role/data, backdrop dismiss).
/// </summary>
public class IonActionSheetTests : IonicComponentTestBase
{
    private static IReadOnlyList<IonActionSheetButton> Buttons() => new List<IonActionSheetButton>
    {
        new() { Text = "Delete", Role = "destructive", Data = "delete" },
        new() { Text = "Share", Data = "share" },
        new() { Text = "Cancel", Role = "cancel", Data = "cancel" },
    };

    private static ComponentUnderTest RenderSheet(TestContext ctx,
        Action<ComponentParameterBuilder<IonActionSheet>>? configure = null)
        => ctx.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), true);
            p.Add(nameof(IonActionSheet.Header), "Actions");
            p.Add(nameof(IonActionSheet.Buttons), Buttons());
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonActionSheet_RendersOverlayContract()
    {
        var cut = RenderSheet(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-action-sheet");
        cut.FindByClass("action-sheet-backdrop").ShouldHaveSingleItem();
        cut.FindByClass("action-sheet-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("action-sheet-container").ShouldHaveSingleItem();
        cut.FindByClass("action-sheet-group").Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void IonActionSheet_RendersHeaderAndSubHeader()
    {
        var cut = RenderSheet(Context, p => p.Add(nameof(IonActionSheet.SubHeader), "Choose an action"));

        var title = cut.FindByClass("action-sheet-title").ShouldHaveSingleItem();
        title.ShouldHaveClass("action-sheet-has-sub-title");
        cut.FindByClass("action-sheet-sub-title").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Actions");
        cut.GetTextContent().ShouldContain("Choose an action");
    }

    [Fact]
    public void IonActionSheet_NoHeader_OmitsTitle()
    {
        var cut = Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), true);
            p.Add(nameof(IonActionSheet.Buttons), Buttons());
        });

        cut.FindByClass("action-sheet-title").ShouldBeEmpty();
    }

    [Fact]
    public void IonActionSheet_RendersButtonPerNonCancelOption()
    {
        var cut = RenderSheet(Context);

        // Delete + Share render in the main group; Cancel is split out.
        var mainGroup = cut.FindByClass("action-sheet-group")
            .First(g => !g.HasClass("action-sheet-group-cancel"));
        mainGroup.FindByClass("action-sheet-button").Count.ShouldBe(2);
    }

    [Fact]
    public void IonActionSheet_ButtonInner_WrapsLabel()
    {
        var cut = RenderSheet(Context);

        var firstButton = cut.FindByClass("action-sheet-button").First();
        firstButton.FindByClass("action-sheet-button-inner").ShouldHaveSingleItem();
    }

    // ---- Cancel split & roles ---------------------------------------------

    [Fact]
    public void IonActionSheet_CancelButton_RendersInSeparateGroup()
    {
        var cut = RenderSheet(Context);

        var cancelGroup = cut.FindByClass("action-sheet-group-cancel").ShouldHaveSingleItem();
        var cancelButton = cancelGroup.FindByClass("action-sheet-button").ShouldHaveSingleItem();
        cancelButton.ShouldHaveClass("action-sheet-cancel");
    }

    [Fact]
    public void IonActionSheet_DestructiveButton_StampsRoleClass()
    {
        var cut = RenderSheet(Context);

        var buttons = cut.FindByClass("action-sheet-button");
        buttons.ShouldContain(b => b.HasClass("action-sheet-destructive"));
    }

    [Fact]
    public void IonActionSheet_DisabledButton_IsNotActivatable()
    {
        var cut = Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), true);
            p.Add(nameof(IonActionSheet.Buttons), (IReadOnlyList<IonActionSheetButton>)new List<IonActionSheetButton>
            {
                new() { Text = "Nope", Disabled = true },
            });
        });

        var button = cut.FindByClass("action-sheet-button").ShouldHaveSingleItem();
        button.ShouldNotHaveClass("ion-activatable");
    }

    // ---- Open / closed gating ---------------------------------------------

    [Fact]
    public void IonActionSheet_Closed_StampsOverlayHidden()
    {
        var cut = Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), false);
            p.Add(nameof(IonActionSheet.Buttons), Buttons());
        });

        cut.Root.ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonActionSheet_Open_DoesNotStampOverlayHidden()
    {
        var cut = RenderSheet(Context);

        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    // ---- Dismiss interaction ----------------------------------------------

    [Fact]
    public async Task IonActionSheet_ButtonTap_DismissesWithRoleAndData()
    {
        IonOverlayDismissEventArgs? dismissed = null;
        var sheet = new IonActionSheet
        {
            IsOpen = true,
            Buttons = Buttons(),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        sheet.Build();

        await Invoke(sheet, "ButtonClickAsync", Buttons().First(b => b.Text == "Share"));

        dismissed.ShouldNotBeNull();
        dismissed!.Data.ShouldBe("share");
    }

    [Fact]
    public async Task IonActionSheet_ButtonTap_RunsHandler()
    {
        var ran = false;
        var buttons = new List<IonActionSheetButton>
        {
            new() { Text = "Go", Handler = () => ran = true },
        };
        var sheet = new IonActionSheet { IsOpen = true, Buttons = buttons };
        sheet.Build();

        await Invoke(sheet, "ButtonClickAsync", buttons[0]);

        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task IonActionSheet_BackdropTap_Dismisses_WhenBackdropDismissEnabled()
    {
        var closed = false;
        IonOverlayDismissEventArgs? dismissed = null;
        var sheet = new IonActionSheet
        {
            IsOpen = true,
            BackdropDismiss = true,
            Buttons = Buttons(),
            IsOpenChanged = EventCallback.Factory.Create<bool>(this, v => closed = !v),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        sheet.Build();

        await Invoke(sheet, "OnBackdropTapAsync", new MouseEventArgs());

        closed.ShouldBeTrue();
        dismissed!.Role.ShouldBe("backdrop");
        dismissed!.IsCancel.ShouldBeTrue();
    }

    [Fact]
    public async Task IonActionSheet_BackdropTap_IsNoOp_WhenBackdropDismissDisabled()
    {
        var invoked = false;
        var sheet = new IonActionSheet
        {
            IsOpen = true,
            BackdropDismiss = false,
            Buttons = Buttons(),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => invoked = true),
        };
        sheet.Build();

        await Invoke(sheet, "OnBackdropTapAsync", new MouseEventArgs());

        invoked.ShouldBeFalse();
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonActionSheet_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderSheet(Context);

        cut.Root.Class.ShouldStartWith("ios ion-action-sheet");
    }

    // Invokes a private async handler on the component (mirrors what a click/tap dispatches).
    private static async Task Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
