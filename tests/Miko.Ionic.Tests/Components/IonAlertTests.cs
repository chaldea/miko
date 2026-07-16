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
/// Tests for <c>ion-alert</c>. Covers the DOM contract (backdrop + wrapper + head + message +
/// button group), the header/sub-header heading levels, string-button normalization, the
/// vertical button group when there are more than two buttons, the radio/checkbox/text input
/// groups, the open/closed gating, and the dismiss callbacks.
/// </summary>
public class IonAlertTests : IonicComponentTestBase
{
    private static IReadOnlyList<IonAlertButton> OneButton() =>
        new List<IonAlertButton> { IonAlertButton.FromText("Action") };

    private static ComponentUnderTest RenderAlert(TestContext ctx,
        Action<ComponentParameterBuilder<IonAlert>>? configure = null)
        => ctx.Render<IonAlert>(p =>
        {
            p.Add(nameof(IonAlert.IsOpen), true);
            p.Add(nameof(IonAlert.Header), "A Short Title Is Best");
            p.Add(nameof(IonAlert.Message), "A message should be a short, complete sentence.");
            p.Add(nameof(IonAlert.Buttons), OneButton());
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonAlert_RendersOverlayContract()
    {
        var cut = RenderAlert(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-alert");
        cut.FindByClass("alert-backdrop").ShouldHaveSingleItem();
        cut.FindByClass("alert-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("alert-head").ShouldHaveSingleItem();
        cut.FindByClass("alert-message").ShouldHaveSingleItem();
        cut.FindByClass("alert-button-group").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonAlert_Header_RendersH2Title()
    {
        var cut = RenderAlert(Context);

        var title = cut.FindByClass("alert-title").ShouldHaveSingleItem();
        title.TagName.ShouldBe("h2");
        cut.GetTextContent().ShouldContain("A Short Title Is Best");
    }

    [Fact]
    public void IonAlert_SubHeaderWithHeader_RendersH3()
    {
        var cut = RenderAlert(Context, p => p.Add(nameof(IonAlert.SubHeader), "A Sub Header Is Optional"));

        var sub = cut.FindByClass("alert-sub-title").ShouldHaveSingleItem();
        // With a header present, the sub-title is one heading level lower (h3).
        sub.TagName.ShouldBe("h3");
    }

    [Fact]
    public void IonAlert_SubHeaderWithoutHeader_RendersH2()
    {
        var cut = Context.Render<IonAlert>(p =>
        {
            p.Add(nameof(IonAlert.IsOpen), true);
            p.Add(nameof(IonAlert.SubHeader), "Only a sub header");
            p.Add(nameof(IonAlert.Buttons), OneButton());
        });

        var sub = cut.FindByClass("alert-sub-title").ShouldHaveSingleItem();
        // Without a header, the sub-title is the top heading (h2).
        sub.TagName.ShouldBe("h2");
    }

    [Fact]
    public void IonAlert_NoMessage_OmitsMessage()
    {
        var cut = Context.Render<IonAlert>(p =>
        {
            p.Add(nameof(IonAlert.IsOpen), true);
            p.Add(nameof(IonAlert.Header), "Title");
            p.Add(nameof(IonAlert.Buttons), OneButton());
        });

        cut.FindByClass("alert-message").ShouldBeEmpty();
    }

    // ---- Buttons -----------------------------------------------------------

    [Fact]
    public void IonAlert_StringButton_RendersAsButton()
    {
        var cut = RenderAlert(Context);

        var button = cut.FindByClass("alert-button").ShouldHaveSingleItem();
        button.FindByClass("alert-button-inner").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Action");
    }

    [Fact]
    public void IonAlert_CancelStringButton_GetsCancelRoleClass()
    {
        var cut = Context.Render<IonAlert>(p =>
        {
            p.Add(nameof(IonAlert.IsOpen), true);
            p.Add(nameof(IonAlert.Buttons), (IReadOnlyList<IonAlertButton>)new List<IonAlertButton>
            {
                IonAlertButton.FromText("Cancel"),
                IonAlertButton.FromText("OK"),
            });
        });

        var buttons = cut.FindByClass("alert-button");
        buttons.ShouldContain(b => b.HasClass("alert-button-role-cancel"));
    }

    [Fact]
    public void IonAlert_TwoButtons_UsesHorizontalGroup()
    {
        var cut = Context.Render<IonAlert>(p =>
        {
            p.Add(nameof(IonAlert.IsOpen), true);
            p.Add(nameof(IonAlert.Buttons), (IReadOnlyList<IonAlertButton>)new List<IonAlertButton>
            {
                IonAlertButton.FromText("Cancel"),
                IonAlertButton.FromText("OK"),
            });
        });

        cut.FindByClass("alert-button-group").ShouldHaveSingleItem()
            .ShouldNotHaveClass("alert-button-group-vertical");
    }

    [Fact]
    public void IonAlert_MoreThanTwoButtons_UsesVerticalGroup()
    {
        var cut = Context.Render<IonAlert>(p =>
        {
            p.Add(nameof(IonAlert.IsOpen), true);
            p.Add(nameof(IonAlert.Buttons), (IReadOnlyList<IonAlertButton>)new List<IonAlertButton>
            {
                IonAlertButton.FromText("One"),
                IonAlertButton.FromText("Two"),
                IonAlertButton.FromText("Three"),
            });
        });

        cut.FindByClass("alert-button-group").ShouldHaveSingleItem()
            .ShouldHaveClass("alert-button-group-vertical");
    }

    // ---- Inputs ------------------------------------------------------------

    [Fact]
    public void IonAlert_RadioInputs_RenderRadioGroup()
    {
        var cut = RenderAlert(Context, p => p.Add(nameof(IonAlert.Inputs),
            (IReadOnlyList<IonAlertInput>)new List<IonAlertInput>
            {
                new() { Type = "radio", Label = "A", Value = "a", Checked = true },
                new() { Type = "radio", Label = "B", Value = "b" },
            }));

        cut.FindByClass("alert-radio-group").ShouldHaveSingleItem();
        cut.FindByClass("alert-radio-button").Count.ShouldBe(2);
        cut.FindByClass("alert-checkbox-group").ShouldBeEmpty();
    }

    [Fact]
    public void IonAlert_CheckboxInputs_RenderCheckboxGroup()
    {
        var cut = RenderAlert(Context, p => p.Add(nameof(IonAlert.Inputs),
            (IReadOnlyList<IonAlertInput>)new List<IonAlertInput>
            {
                new() { Type = "checkbox", Label = "A", Value = "a" },
            }));

        cut.FindByClass("alert-checkbox-group").ShouldHaveSingleItem();
        cut.FindByClass("alert-checkbox-button").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonAlert_TextInputs_RenderInputGroup()
    {
        var cut = RenderAlert(Context, p => p.Add(nameof(IonAlert.Inputs),
            (IReadOnlyList<IonAlertInput>)new List<IonAlertInput>
            {
                new() { Type = "text", Placeholder = "Name" },
            }));

        cut.FindByClass("alert-input-group").ShouldHaveSingleItem();
        cut.FindByClass("alert-input").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonAlert_RadioSelection_IsSingleSelect()
    {
        var inputs = new List<IonAlertInput>
        {
            new() { Type = "radio", Label = "A", Value = "a", Checked = true },
            new() { Type = "radio", Label = "B", Value = "b" },
        };
        var alert = new IonAlert { IsOpen = true, Inputs = inputs };
        alert.Build();

        // Selecting B unchecks A (single-select radios).
        Invoke(alert, "RadioClickAsync", inputs[1]);

        inputs[0].Checked.ShouldBeFalse();
        inputs[1].Checked.ShouldBeTrue();
    }

    // ---- Open / closed gating & dismiss -----------------------------------

    [Fact]
    public void IonAlert_Closed_StampsOverlayHidden()
    {
        var cut = Context.Render<IonAlert>(p =>
        {
            p.Add(nameof(IonAlert.IsOpen), false);
            p.Add(nameof(IonAlert.Buttons), OneButton());
        });

        cut.Root.ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public async Task IonAlert_ButtonTap_DismissesWithRole_AndRunsHandler()
    {
        var ran = false;
        IonOverlayDismissEventArgs? dismissed = null;
        var buttons = new List<IonAlertButton>
        {
            new() { Text = "Delete", Role = "destructive", Handler = () => ran = true },
        };
        var alert = new IonAlert
        {
            IsOpen = true,
            Buttons = buttons,
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        alert.Build();

        await InvokeAsync(alert, "ButtonClickAsync", buttons[0]);

        ran.ShouldBeTrue();
        dismissed!.Role.ShouldBe("destructive");
    }

    [Fact]
    public async Task IonAlert_BackdropTap_Dismisses_WhenEnabled()
    {
        IonOverlayDismissEventArgs? dismissed = null;
        var alert = new IonAlert
        {
            IsOpen = true,
            BackdropDismiss = true,
            Buttons = OneButton(),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        alert.Build();

        await InvokeAsync(alert, "OnBackdropTapAsync", new MouseEventArgs());

        dismissed!.Role.ShouldBe("backdrop");
    }

    [Fact]
    public async Task IonAlert_BackdropTap_IsNoOp_WhenDisabled()
    {
        var invoked = false;
        var alert = new IonAlert
        {
            IsOpen = true,
            BackdropDismiss = false,
            Buttons = OneButton(),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, _ => invoked = true),
        };
        alert.Build();

        await InvokeAsync(alert, "OnBackdropTapAsync", new MouseEventArgs());

        invoked.ShouldBeFalse();
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonAlert_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderAlert(Context);

        cut.Root.Class.ShouldStartWith("ios ion-alert");
    }

    private static void Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        mi.Invoke(component, new[] { arg });
    }

    private static async Task InvokeAsync(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
