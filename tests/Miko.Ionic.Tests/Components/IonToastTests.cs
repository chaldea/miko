using Microsoft.Extensions.DependencyInjection;
using Miko.Animation;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-toast</c>. Covers the DOM contract (host + wrapper + container + content, with
/// NO backdrop), the position class on the wrapper, the header/message/icon renders, the buttons
/// split into start/end groups, the open/closed <c>overlay-hidden</c> gating, the color class, the
/// dismiss callbacks (a normal button runs its handler then dismisses; a cancel button dismisses
/// without running; both raise <c>OnDidDismiss</c> with the role), the <c>Duration</c> auto-dismiss
/// timer, and the enter/leave animations (including the edge offset that lifts the toast off the
/// screen edge).
/// </summary>
public class IonToastTests : IonicComponentTestBase
{
    private static IReadOnlyList<IonToastButton> Buttons() => new List<IonToastButton>
    {
        new() { Text = "Undo", Side = "start", Handler = null },
        new() { Text = "Close", Role = "cancel" },
    };

    private static ComponentUnderTest RenderToast(TestContext ctx,
        Action<ComponentParameterBuilder<IonToast>>? configure = null)
        => ctx.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), true);
            p.Add(nameof(IonToast.Message), "Saved.");
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonToast_RendersOverlayContract()
    {
        var cut = RenderToast(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-toast");
        cut.FindByClass("toast-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("toast-container").ShouldHaveSingleItem();
        cut.FindByClass("toast-content").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonToast_HasNoBackdrop()
    {
        // A toast is a non-blocking notification: no backdrop, unlike action-sheet/alert/loading.
        var cut = RenderToast(Context);

        cut.FindByClass("ion-backdrop").ShouldBeEmpty();
        cut.FindByClass("toast-backdrop").ShouldBeEmpty();
    }

    [Fact]
    public void IonToast_WrapperCarriesPositionClass_Default()
    {
        var cut = RenderToast(Context);

        var wrapper = cut.FindByClass("toast-wrapper").ShouldHaveSingleItem();
        wrapper.ShouldHaveClass("toast-bottom");
        wrapper.ShouldHaveClass("ion-overlay-wrapper");
    }

    [Theory]
    [InlineData("top", "toast-top")]
    [InlineData("middle", "toast-middle")]
    [InlineData("bottom", "toast-bottom")]
    public void IonToast_WrapperCarriesPositionClass(string position, string expectedClass)
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Position), position));

        cut.FindByClass("toast-wrapper").ShouldHaveSingleItem().ShouldHaveClass(expectedClass);
    }

    [Fact]
    public void IonToast_RendersHeaderAndMessage()
    {
        var cut = RenderToast(Context, p =>
        {
            p.Add(nameof(IonToast.Header), "Success");
            p.Add(nameof(IonToast.Message), "Your changes were saved.");
        });

        cut.FindByClass("toast-header").ShouldHaveSingleItem();
        cut.FindByClass("toast-message").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Success");
        cut.GetTextContent().ShouldContain("Your changes were saved.");
    }

    [Fact]
    public void IonToast_NoHeader_OmitsHeader()
    {
        var cut = RenderToast(Context);

        cut.FindByClass("toast-header").ShouldBeEmpty();
    }

    [Fact]
    public void IonToast_RendersIcon()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Icon), "information-circle"));

        cut.FindByClass("toast-icon").ShouldNotBeEmpty();
    }

    // ---- Buttons split by side --------------------------------------------

    [Fact]
    public void IonToast_SplitsButtonsIntoStartAndEndGroups()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Buttons), Buttons()));

        cut.FindByClass("toast-button-group-start").ShouldHaveSingleItem();
        cut.FindByClass("toast-button-group-end").ShouldHaveSingleItem();
        cut.FindByClass("toast-button").Count.ShouldBe(2);
    }

    [Fact]
    public void IonToast_CancelButton_StampsRoleClass()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Buttons), Buttons()));

        cut.FindByClass("toast-button").ShouldContain(b => b.HasClass("toast-button-cancel"));
    }

    [Fact]
    public void IonToast_IconOnlyButton_StampsIconOnlyClass()
    {
        var buttons = (IReadOnlyList<IonToastButton>)new List<IonToastButton>
        {
            new() { Icon = "close", Role = "cancel" },
        };
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Buttons), buttons));

        cut.FindByClass("toast-button").ShouldHaveSingleItem().ShouldHaveClass("toast-button-icon-only");
    }

    // ---- Open / closed gating ---------------------------------------------

    [Fact]
    public void IonToast_Closed_StampsOverlayHidden()
    {
        var cut = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), false);
            p.Add(nameof(IonToast.Message), "Hidden");
        });

        cut.Root.ShouldHaveClass("overlay-hidden");
        cut.Root.ShouldNotHaveClass("toast-mounted");
        cut.Root.ShouldNotHaveClass("toast-open");
    }

    [Fact]
    public void IonToast_Open_DoesNotStampOverlayHidden()
    {
        var cut = RenderToast(Context);

        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    // ---- Color -------------------------------------------------------------

    [Fact]
    public void IonToast_Color_StampsColorClass()
    {
        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Color), "success"));

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-success");
    }

    // ---- Dismiss interaction ----------------------------------------------

    [Fact]
    public async Task IonToast_ButtonTap_RunsHandlerAndDismisses()
    {
        var ran = false;
        IonOverlayDismissEventArgs? dismissed = null;
        var button = new IonToastButton { Text = "Undo", Role = "undo", Handler = () => ran = true };
        var toast = new IonToast
        {
            IsOpen = true,
            Buttons = new List<IonToastButton> { button },
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        toast.Build();

        await Invoke(toast, "ButtonClickAsync", button);

        ran.ShouldBeTrue();
        dismissed.ShouldNotBeNull();
        dismissed!.Role.ShouldBe("undo");
    }

    [Fact]
    public async Task IonToast_CancelButton_DismissesWithoutRunningHandler()
    {
        var ran = false;
        var closed = false;
        IonOverlayDismissEventArgs? dismissed = null;
        var button = new IonToastButton { Text = "Close", Role = "cancel", Handler = () => ran = true };
        var toast = new IonToast
        {
            IsOpen = true,
            Buttons = new List<IonToastButton> { button },
            IsOpenChanged = EventCallback.Factory.Create<bool>(this, v => closed = !v),
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this, e => dismissed = e),
        };
        toast.Build();

        await Invoke(toast, "ButtonClickAsync", button);

        ran.ShouldBeFalse();
        closed.ShouldBeTrue();
        dismissed!.Role.ShouldBe("cancel");
        dismissed!.IsCancel.ShouldBeTrue();
    }

    // ---- Duration auto-dismiss (issue 1) -----------------------------------

    [Fact]
    public async Task IonToast_WithDuration_AutoDismissesWithTimeoutRole()
    {
        var dispatcher = new MikoDispatcher();
        Context.Services.AddSingleton(dispatcher);

        var closed = false;
        var dismissed = false;
        string? role = "unset";

        Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), true);
            p.Add(nameof(IonToast.Message), "Paired successfully");
            p.Add(nameof(IonToast.Duration), 60);
            p.Add(nameof(IonToast.IsOpenChanged), EventCallback.Factory.Create<bool>(this, open => closed = !open));
            p.Add(nameof(IonToast.OnDidDismiss), EventCallback.Factory.Create<IonOverlayDismissEventArgs>(
                this, e => { dismissed = true; role = e.Role; }));
        });

        await WaitForAsync(() => { dispatcher.Drain(); return dismissed; });

        dismissed.ShouldBeTrue();
        closed.ShouldBeTrue();
        // toast.tsx: setTimeout(() => this.dismiss(undefined, 'timeout'), this.duration).
        role.ShouldBe("timeout");
    }

    [Fact]
    public async Task IonToast_WithoutDuration_StaysOpen()
    {
        var dispatcher = new MikoDispatcher();
        Context.Services.AddSingleton(dispatcher);

        var dismissed = false;

        var cut = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), true);
            p.Add(nameof(IonToast.Message), "Sticky");
            p.Add(nameof(IonToast.OnDidDismiss), EventCallback.Factory.Create<IonOverlayDismissEventArgs>(
                this, _ => dismissed = true));
        });

        await Task.Delay(80);
        dispatcher.Drain();

        dismissed.ShouldBeFalse();
        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task IonToast_WithNonPositiveDuration_StaysOpen(int duration)
    {
        var dispatcher = new MikoDispatcher();
        Context.Services.AddSingleton(dispatcher);

        var dismissed = false;

        Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), true);
            p.Add(nameof(IonToast.Message), "Sticky");
            p.Add(nameof(IonToast.Duration), duration);
            p.Add(nameof(IonToast.OnDidDismiss), EventCallback.Factory.Create<IonOverlayDismissEventArgs>(
                this, _ => dismissed = true));
        });

        await Task.Delay(80);
        dispatcher.Drain();

        dismissed.ShouldBeFalse();
    }

    [Fact]
    public void IonToast_WithDurationButNoDispatcher_DoesNotThrow()
    {
        // A bare test / no DI has nothing to marshal the timer back onto the render thread; the
        // toast must still render rather than blow up.
        var cut = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), true);
            p.Add(nameof(IonToast.Message), "No dispatcher");
            p.Add(nameof(IonToast.Duration), 50);
        });

        cut.Root.ShouldNotBeNull();
        cut.Root.ShouldNotHaveClass("overlay-hidden");
    }

    [Fact]
    public async Task IonToast_ButtonDismissBeforeTimer_DoesNotDismissTwice()
    {
        var dispatcher = new MikoDispatcher();
        var button = new IonToastButton { Text = "Close", Role = "cancel" };
        var dismissCount = 0;
        var roles = new List<string?>();

        var toast = new IonToast
        {
            IsOpen = true,
            Duration = 60,
            Buttons = new List<IonToastButton> { button },
            OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(
                this, e => { dismissCount++; roles.Add(e.Role); }),
        };
        SetDispatcher(toast, dispatcher);
        toast.Build();

        // Tap the button well before the 60ms timer would fire.
        await Invoke(toast, "ButtonClickAsync", button);

        // Let the timer's delay elapse; its tick must find the toast already closed and bail.
        await Task.Delay(120);
        dispatcher.Drain();

        dismissCount.ShouldBe(1);
        roles.ShouldHaveSingleItem().ShouldBe("cancel");
    }

    // ---- Button color (issue 2) --------------------------------------------

    [Fact]
    public void IonToast_ColoredToast_ButtonsUseTheHostContrastColor()
    {
        // Ionic: :host(.ion-color) { --button-color: inherit } — a tinted toast drops the default
        // primary button color, which is otherwise near-invisible on a dark/primary surface, and
        // reads the host's contrast color so the buttons match the message text.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToast(Context, p =>
        {
            p.Add(nameof(IonToast.Color), "dark");
            p.Add(nameof(IonToast.Buttons), Buttons());
        });

        var contrast = cut.GetComputedStyle(cut.Root)!.Color;
        foreach (var button in cut.FindByClass("toast-button"))
        {
            cut.GetComputedStyle(button)!.Color.ShouldBe(contrast);
        }
    }

    [Fact]
    public void IonToast_ColoredToast_ButtonColorDiffersFromTheSurface()
    {
        // The regression this guards: with Color="dark" the buttons rendered in the theme primary,
        // which sat almost on top of the dark wrapper background and made them unreadable.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToast(Context, p =>
        {
            p.Add(nameof(IonToast.Color), "dark");
            p.Add(nameof(IonToast.Buttons), Buttons());
        });

        var wrapperBackground = cut.GetComputedStyle(cut.FindByClass("toast-wrapper").Single())!.BackgroundColor;
        var buttonColor = cut.GetComputedStyle(cut.FindByClass("toast-button").First())!.Color;

        buttonColor.ShouldNotBe(wrapperBackground);
    }

    [Fact]
    public void IonToast_UncoloredToast_ButtonsUseThePrimaryColor()
    {
        // Without ion-color the default --button-color (ion-color(primary, base)) still applies.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Buttons), Buttons()));

        var button = cut.FindByClass("toast-button").First(b => !b.HasClass("toast-button-cancel"));
        cut.GetComputedStyle(button)!.Color.ShouldBe(IonicTheme.CreateMd().Primary);
    }

    // ---- Enter / leave animation and edge offset (issue 3) -----------------

    [Fact]
    public void IonToast_Open_StampsMountedAndOpenClasses()
    {
        var cut = RenderToast(Context);

        cut.Root.ShouldHaveClass("toast-mounted");
        cut.Root.ShouldHaveClass("toast-open");
    }

    [Fact]
    public void IonToast_Closed_KeepsLayoutBox_SoTheEnterAnimationCanRun()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), false);
            p.Add(nameof(IonToast.Message), "Hidden");
        });

        // Ionic's :host(.overlay-hidden) is display:none, but a display:none element has no
        // previous-frame layout box for the engine to diff against — the enter animation could never
        // run. The closed toast stays laid out and goes inert via pointer-events instead.
        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style!.Display.ShouldNotBe(Display.None);
        style.PointerEvents.ShouldBe(PointerEvents.None);
    }

    [Fact]
    public void IonToast_ClosedWrapper_IsTransparent_AndOpenWrapperIsOpaque()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var closed = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), false);
            p.Add(nameof(IonToast.Message), "Hidden");
        });
        var open = RenderToast(Context);

        // wrapperAnimation fromTo('opacity', 0.01, 1).
        closed.GetComputedStyle(closed.FindByClass("toast-wrapper").Single())!.Opacity.ShouldBe(0.01f, 0.001f);
        open.GetComputedStyle(open.FindByClass("toast-wrapper").Single())!.Opacity.ShouldBe(1f, 0.001f);
    }

    [Theory]
    [InlineData("bottom", -8f)]
    [InlineData("top", 8f)]
    public void IonToast_Md_OpenWrapper_RestsAtTheEdgeOffset(string position, float expectedPx)
    {
        // The heart of issue 3: `.toast-bottom` really IS at bottom:0 in Ionic — the visible gap
        // from the screen edge is the enter animation's end transform,
        // translateY(calc(-8px - var(--ion-safe-area-bottom))) for md (animations/utils.ts
        // getAnimationPosition). Without it the toast sat flush against the edge.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Position), position));

        var wrapper = cut.FindByClass("toast-wrapper").Single();
        // The anchor offset itself stays at 0, exactly as in Ionic.
        var style = cut.GetComputedStyle(wrapper)!;
        (position == "bottom" ? style.Bottom : style.Top).Value.ShouldBe(0f);

        TranslateYPx(style).ShouldBe(expectedPx, 0.001f);
    }

    [Theory]
    [InlineData("bottom", -10f)]
    [InlineData("top", 10f)]
    public void IonToast_Ios_OpenWrapper_RestsAtTheEdgeOffset(string position, float expectedPx)
    {
        // ios uses a 10px offset (animations/utils.ts: mode === 'md' ? 8 : 10).
        UsePlatform(HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Position), position));

        TranslateYPx(cut.GetComputedStyle(cut.FindByClass("toast-wrapper").Single())!).ShouldBe(expectedPx, 0.001f);
    }

    [Fact]
    public void IonToast_EdgeOffset_AddsTheSafeAreaInset()
    {
        // getAnimationPosition: bottom is calc(-8px - var(--ion-safe-area-bottom, 0px)), so a device
        // with a home indicator pushes the toast further up rather than under the system bar.
        Context.SafeArea = new SafeAreaInsets(0, 0, 0, 34);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderToast(Context, p => p.Add(nameof(IonToast.Position), "bottom"));

        // -(8px + 34px safe-area) — the offset and the inset stack.
        var wrapper = cut.FindByClass("toast-wrapper").Single();
        TranslateYPx(cut.GetComputedStyle(wrapper)!).ShouldBe(-42f, 0.001f);
    }

    [Fact]
    public void IonToast_Ios_ClosedWrapper_IsParkedOffScreen()
    {
        // ios.enter.ts slides the toast in: translateY(-100%) → translateY(top) for position=top,
        // translateY(100%) → translateY(bottom) for position=bottom.
        UsePlatform(HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var closed = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), false);
            p.Add(nameof(IonToast.Message), "Hidden");
            p.Add(nameof(IonToast.Position), "bottom");
        });

        TranslateYPercent(closed.GetComputedStyle(closed.FindByClass("toast-wrapper").Single())).ShouldBe(100f);
    }

    [Fact]
    public void IonToast_Wrapper_DeclaresTheIonicDurations()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var closed = Context.Render<IonToast>(p =>
        {
            p.Add(nameof(IonToast.IsOpen), false);
            p.Add(nameof(IonToast.Message), "Hidden");
        });
        var open = RenderToast(Context);

        // Miko reads the transition list from the PREVIOUS frame's style, so the closed-state list
        // governs the transition OUT of closed — the 400ms ENTER — and the open-state list the
        // 300ms LEAVE. See the note in ToastStyles.AddAnimation.
        closed.GetComputedStyle(closed.FindByClass("toast-wrapper").Single())!
            .Transitions.ShouldAllBe(t => t.Duration == 0.4f);
        open.GetComputedStyle(open.FindByClass("toast-wrapper").Single())!
            .Transitions.ShouldAllBe(t => t.Duration == 0.3f);
    }

    [Fact]
    public async Task IonToast_Dismiss_StaysMountedForTheLeaveAnimation()
    {
        var animations = new AnimationManager();
        var button = new IonToastButton { Text = "Close", Role = "cancel" };
        var toast = new IonToast { IsOpen = true, Buttons = new List<IonToastButton> { button } };
        SetAnimations(toast, animations);
        toast.Build();

        await Invoke(toast, "ButtonClickAsync", button);
        var root = toast.Build();

        // Mid-dismiss the host keeps its on-screen state so the fade/slide-out is visible.
        root.ShouldHaveClass("toast-mounted");
        root.ShouldNotHaveClass("toast-open");
        root.ShouldNotHaveClass("overlay-hidden");
    }

    [Fact]
    public async Task IonToast_SettlesToHidden_AfterTheLeaveAnimationCompletes()
    {
        var animations = new AnimationManager();
        var button = new IonToastButton { Text = "Close", Role = "cancel" };
        var toast = new IonToast { IsOpen = true, Buttons = new List<IonToastButton> { button } };
        SetAnimations(toast, animations);
        toast.Build();
        await Invoke(toast, "ButtonClickAsync", button);

        var wrapper = toast.Build().FindByClass("toast-wrapper").Single();
        RaiseTransitionCompleted(animations, wrapper);

        var root = toast.Build();
        root.ShouldHaveClass("overlay-hidden");
        root.ShouldNotHaveClass("toast-mounted");
    }

    [Fact]
    public async Task IonToast_ReopenedMidDismiss_StaysOpen()
    {
        var animations = new AnimationManager();
        var button = new IonToastButton { Text = "Close", Role = "cancel" };
        var toast = new IonToast { IsOpen = true, Buttons = new List<IonToastButton> { button } };
        SetAnimations(toast, animations);
        toast.Build();
        await Invoke(toast, "ButtonClickAsync", button);

        // Host re-opens before the leave finishes; the late completion must not hide the toast.
        toast.IsOpen = true;
        var wrapper = toast.Build().FindByClass("toast-wrapper").Single();
        RaiseTransitionCompleted(animations, wrapper);

        var root = toast.Build();
        root.ShouldHaveClass("toast-open");
        root.ShouldNotHaveClass("overlay-hidden");
    }

    [Fact]
    public async Task IonToast_WithoutAnimationManager_SettlesToHiddenImmediately()
    {
        var button = new IonToastButton { Text = "Close", Role = "cancel" };
        var toast = new IonToast { IsOpen = true, Buttons = new List<IonToastButton> { button } };
        toast.Build();

        await Invoke(toast, "ButtonClickAsync", button);

        // No animation manager (bare test / no DI): nothing would ever report the fade-out as done,
        // so the toast must not stay mounted forever.
        toast.Build().ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonToast_Opening_ActuallyRunsATransition_InTheEngine()
    {
        // End-to-end proof that the toast animates in rather than snapping: drive a real MikoEngine
        // with the Ionic stylesheet and flip the host's classes the way OnParametersSet does.
        using var bitmap = new SKBitmap(600, 600);
        using var canvas = new SKCanvas(bitmap);

        var root = new Miko.Core.DomElements.DivElement
        {
            Style = new Miko.Styling.Style { Width = Length.Px(600), Height = Length.Px(600) },
        };
        var host = new Miko.Core.DomElements.DivElement { Class = "md ion-toast overlay-hidden" };
        var wrapper = new Miko.Core.DomElements.DivElement
        {
            Class = "toast-wrapper ion-overlay-wrapper toast-bottom toast-layout-baseline",
        };
        host.AddChild(wrapper);
        root.AddChild(host);

        var engine = new MikoEngine();
        engine.Initialize(root, [IonicStyleSheetFactory.CreateAllModes()], canvas, 600, 600);
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();

        host.Class = "md ion-toast toast-mounted toast-open";
        engine.Render(canvas);

        // The wrapper's opacity is in flight (md cross-fades; its transform never travels).
        engine.AnimationManager.ActiveTransitionCount.ShouldBeGreaterThan(0);

        // Mid-flight the wrapper sits between 0.01 and 1 rather than at the end state — i.e. it is
        // genuinely animating. The manager writes each interpolated frame to the inline Style.
        engine.AnimationManager.Update(0.2f);
        var opacity = wrapper.Style?.Opacity;
        opacity.ShouldNotBeNull();
        var mid = opacity!.Value.Value;
        mid.ShouldBeGreaterThan(0.01f);
        mid.ShouldBeLessThan(1f);
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonToast_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderToast(Context);

        cut.Root.Class.ShouldStartWith("ios ion-toast");
    }

    // Invokes a private async handler on the component (mirrors what a click/tap dispatches).
    private static async Task Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }

    /// <summary>Assigns an <c>[Inject]</c> property directly; a bare-instance test has no DI scope.</summary>
    private static void SetInjected(IonToast toast, string property, object value)
        => typeof(IonToast)
            .GetProperty(property, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(toast, value);

    private static void SetAnimations(IonToast toast, AnimationManager animations)
        => SetInjected(toast, "Animations", animations);

    private static void SetDispatcher(IonToast toast, MikoDispatcher dispatcher)
        => SetInjected(toast, "Dispatcher", dispatcher);

    /// <summary>
    /// Runs a real opacity transition on the wrapper to completion, so the manager raises
    /// <c>TransitionCompleted</c> through its normal path (rather than the test faking the event).
    /// </summary>
    private static void RaiseTransitionCompleted(AnimationManager animations, Element wrapper)
    {
        animations.TrackPropertyChange(
            wrapper,
            nameof(Miko.Styling.Style.Opacity),
            1f,
            0.01f,
            Transition.For(x => x.Opacity).Duration(0.3f).Build());

        // One tick past the duration completes it and fires the event.
        animations.Update(0.5f);
    }

    /// <summary>Waits for <paramref name="condition"/> to hold, polling until the timeout.</summary>
    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs && !condition())
        {
            await Task.Delay(10);
        }
    }

    /// <summary>The translateY in px from a computed style; fails the test when there is none.</summary>
    private static float TranslateYPx(Miko.Styling.ComputedStyle style)
        => TranslateYPercent(style) ?? throw new ShouldAssertException("wrapper carries no translateY");

    /// <summary>The translateY percentage in a computed style, or null when there is no translateY.</summary>
    private static float? TranslateYPercent(Miko.Styling.ComputedStyle? style)
        => style?.Transform?.Functions.OfType<TransformFunction.TranslateY>().FirstOrDefault()?.Y.Value;
}
