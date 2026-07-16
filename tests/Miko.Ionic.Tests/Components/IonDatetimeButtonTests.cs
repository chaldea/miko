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
/// Tests for <c>ion-datetime-button</c>. Covers the DOM contract (the date/time button pair, each
/// rendered only when its text is present), the active/disabled class stamping, and the click
/// callbacks the host uses to open its datetime.
/// </summary>
public class IonDatetimeButtonTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderButton(TestContext ctx,
        Action<ComponentParameterBuilder<IonDatetimeButton>>? configure = null)
        => ctx.Render<IonDatetimeButton>(p =>
        {
            p.Add(nameof(IonDatetimeButton.DateText), "Jul 15, 2026");
            p.Add(nameof(IonDatetimeButton.TimeText), "10:30 AM");
            configure?.Invoke(p);
        });

    [Fact]
    public void IonDatetimeButton_RendersDateAndTimeButtons()
    {
        var cut = RenderButton(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-datetime-button");
        cut.FindById("date-button").ShouldNotBeNull();
        cut.FindById("time-button").ShouldNotBeNull();
        cut.GetTextContent().ShouldContain("Jul 15, 2026");
        cut.GetTextContent().ShouldContain("10:30 AM");
    }

    [Fact]
    public void IonDatetimeButton_OmitsDateButton_WhenNoDateText()
    {
        var cut = Context.Render<IonDatetimeButton>(p =>
            p.Add(nameof(IonDatetimeButton.TimeText), "10:30 AM"));

        cut.FindById("date-button").ShouldBeNull();
        cut.FindById("time-button").ShouldNotBeNull();
    }

    [Fact]
    public void IonDatetimeButton_OmitsTimeButton_WhenNoTimeText()
    {
        var cut = Context.Render<IonDatetimeButton>(p =>
            p.Add(nameof(IonDatetimeButton.DateText), "Jul 15, 2026"));

        cut.FindById("date-button").ShouldNotBeNull();
        cut.FindById("time-button").ShouldBeNull();
    }

    [Fact]
    public void IonDatetimeButton_Active_StampsSelectedActiveClass()
    {
        var cut = RenderButton(Context, p =>
        {
            p.Add(nameof(IonDatetimeButton.SelectedButton), "time");
            p.Add(nameof(IonDatetimeButton.DatetimeActive), true);
        });

        cut.Root.ShouldHaveClass("time-active");
    }

    [Fact]
    public void IonDatetimeButton_Disabled_StampsClass()
    {
        var cut = RenderButton(Context, p => p.Add(nameof(IonDatetimeButton.Disabled), true));

        cut.Root.ShouldHaveClass("datetime-button-disabled");
    }

    [Fact]
    public async Task IonDatetimeButton_DateClick_RaisesCallback()
    {
        var clicked = false;
        var button = new IonDatetimeButton
        {
            DateText = "Jul 15, 2026",
            OnDateClick = EventCallback.Factory.Create(this, () => clicked = true),
        };
        button.Build();

        await InvokeAsync(button, "DateClickAsync", new MouseEventArgs());

        clicked.ShouldBeTrue();
    }

    [Fact]
    public async Task IonDatetimeButton_TimeClick_RaisesCallback()
    {
        var clicked = false;
        var button = new IonDatetimeButton
        {
            TimeText = "10:30 AM",
            OnTimeClick = EventCallback.Factory.Create(this, () => clicked = true),
        };
        button.Build();

        await InvokeAsync(button, "TimeClickAsync", new MouseEventArgs());

        clicked.ShouldBeTrue();
    }

    [Fact]
    public async Task IonDatetimeButton_Disabled_ClickIsNoOp()
    {
        var clicked = false;
        var button = new IonDatetimeButton
        {
            DateText = "Jul 15, 2026",
            Disabled = true,
            OnDateClick = EventCallback.Factory.Create(this, () => clicked = true),
        };
        button.Build();

        await InvokeAsync(button, "DateClickAsync", new MouseEventArgs());

        clicked.ShouldBeFalse();
    }

    [Fact]
    public void IonDatetimeButton_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderButton(Context);

        cut.Root.Class.ShouldStartWith("ios ion-datetime-button");
    }

    private static async Task InvokeAsync(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
