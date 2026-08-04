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

    // ---- Enter / leave animation -------------------------------------------

    [Fact]
    public void IonActionSheet_Open_StampsMountedAndOpenClasses()
    {
        var cut = RenderSheet(Context);

        // Open: interactive (mounted) and in its slid-in end state (open).
        cut.Root.ShouldHaveClass("action-sheet-mounted");
        cut.Root.ShouldHaveClass("action-sheet-open");
    }

    [Fact]
    public void IonActionSheet_Closed_KeepsLayoutBox_SoTheEnterAnimationCanRun()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), false);
            p.Add(nameof(IonActionSheet.Buttons), Buttons());
        });

        // Ionic's :host(.overlay-hidden) is display:none, but a display:none element has no
        // previous-frame layout box for the engine to diff against — the slide-up could never
        // animate. The closed sheet stays laid out and goes inert via pointer-events instead.
        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.Display.ShouldNotBe(Display.None);
        style.PointerEvents.ShouldBe(PointerEvents.None);
    }

    [Fact]
    public void IonActionSheet_Open_IsInteractive()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSheet(Context);

        cut.GetComputedStyle(cut.Root)!.PointerEvents.ShouldBe(PointerEvents.Auto);
    }

    [Fact]
    public void IonActionSheet_Closed_WrapperIsParkedBelowTheBottomEdge()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), false);
            p.Add(nameof(IonActionSheet.Buttons), Buttons());
        });

        // wrapperAnimation fromTo('transform', 'translateY(100%)', 'translateY(0%)').
        var wrapper = cut.FindByClass("action-sheet-wrapper").Single();
        TranslateYPercent(cut.GetComputedStyle(wrapper)).ShouldBe(100f);
    }

    [Fact]
    public void IonActionSheet_Open_WrapperIsSlidIntoPlace()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSheet(Context);

        var wrapper = cut.FindByClass("action-sheet-wrapper").Single();
        TranslateYPercent(cut.GetComputedStyle(wrapper)).ShouldBe(0f);
    }

    [Fact]
    public void IonActionSheet_Backdrop_FadesBetweenClosedAndOpen()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var closed = Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), false);
            p.Add(nameof(IonActionSheet.Buttons), Buttons());
        });
        var open = RenderSheet(Context);

        // backdropAnimation fromTo('opacity', 0.01, 'var(--backdrop-opacity)').
        closed.GetComputedStyle(closed.FindByClass("action-sheet-backdrop").Single())!
            .Opacity.ShouldBe(0f);
        open.GetComputedStyle(open.FindByClass("action-sheet-backdrop").Single())!
            .Opacity.ShouldBe(0.32f, 0.001f);
    }

    [Fact]
    public void IonActionSheet_WrapperAndBackdrop_DeclareTheIonicEasingAndDurations()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSheet(Context);

        // Miko reads the transition list from the PREVIOUS frame's style, so the open-state list is
        // the LEAVE animation (450ms) — see the note in ActionSheetStyles.
        var wrapper = cut.GetComputedStyle(cut.FindByClass("action-sheet-wrapper").Single())!;
        var transition = wrapper.Transitions.ShouldHaveSingleItem();
        transition.Property.ShouldBe(nameof(Miko.Styling.Style.Transform));
        transition.Duration.ShouldBe(0.45f, 0.001f);
        transition.CubicBezier.ShouldNotBeNull();
        transition.CubicBezier!.Value.X1.ShouldBe(0.36f, 0.001f);
        transition.CubicBezier!.Value.Y1.ShouldBe(0.66f, 0.001f);
        transition.CubicBezier!.Value.X2.ShouldBe(0.04f, 0.001f);
        transition.CubicBezier!.Value.Y2.ShouldBe(1f, 0.001f);
    }

    [Fact]
    public void IonActionSheet_Closed_DeclaresTheEnterDuration()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonActionSheet>(p =>
        {
            p.Add(nameof(IonActionSheet.IsOpen), false);
            p.Add(nameof(IonActionSheet.Buttons), Buttons());
        });

        // The closed-state list governs the transition OUT of closed — the 400ms enter.
        var wrapper = cut.GetComputedStyle(cut.FindByClass("action-sheet-wrapper").Single())!;
        wrapper.Transitions.ShouldHaveSingleItem().Duration.ShouldBe(0.4f, 0.001f);
    }

    [Fact]
    public async Task IonActionSheet_Dismiss_StaysMountedForTheLeaveAnimation()
    {
        var animations = new AnimationManager();
        var sheet = new IonActionSheet
        {
            IsOpen = true,
            Buttons = Buttons(),
        };
        SetAnimations(sheet, animations);
        sheet.Build();

        await Invoke(sheet, "OnBackdropTapAsync", new MouseEventArgs());
        var root = sheet.Build();

        // Mid-dismiss the host must keep its interactive, on-screen state so the slide-down is
        // visible — it is NOT inert yet.
        root.ShouldHaveClass("action-sheet-mounted");
        root.ShouldNotHaveClass("action-sheet-open");
        root.ShouldNotHaveClass("overlay-hidden");
    }

    [Fact]
    public async Task IonActionSheet_SettlesToHidden_AfterTheSlideOutCompletes()
    {
        var animations = new AnimationManager();
        var sheet = new IonActionSheet
        {
            IsOpen = true,
            Buttons = Buttons(),
        };
        SetAnimations(sheet, animations);
        sheet.Build();
        await Invoke(sheet, "OnBackdropTapAsync", new MouseEventArgs());

        // Report the wrapper's slide-down as finished, the way AnimationManager.Update would.
        var wrapper = sheet.Build().FindByClass("action-sheet-wrapper").Single();
        RaiseTransitionCompleted(animations, wrapper, nameof(Miko.Styling.Style.Transform));

        var root = sheet.Build();
        root.ShouldHaveClass("overlay-hidden");
        root.ShouldNotHaveClass("action-sheet-mounted");
    }

    [Fact]
    public async Task IonActionSheet_ReopenedMidDismiss_StaysOpen()
    {
        var animations = new AnimationManager();
        var sheet = new IonActionSheet
        {
            IsOpen = true,
            Buttons = Buttons(),
        };
        SetAnimations(sheet, animations);
        sheet.Build();
        await Invoke(sheet, "OnBackdropTapAsync", new MouseEventArgs());

        // Host re-opens before the slide-out finishes; the late completion must not hide the sheet.
        sheet.IsOpen = true;
        var wrapper = sheet.Build().FindByClass("action-sheet-wrapper").Single();
        RaiseTransitionCompleted(animations, wrapper, nameof(Miko.Styling.Style.Transform));

        var root = sheet.Build();
        root.ShouldHaveClass("action-sheet-open");
        root.ShouldNotHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonActionSheet_Opening_ActuallyRunsATransition_InTheEngine()
    {
        // End-to-end proof that the slide/fade animate rather than snap: drive a real MikoEngine
        // with the Ionic stylesheet and flip the host's classes the way OnParametersSet does.
        using var bitmap = new SKBitmap(600, 600);
        using var canvas = new SKCanvas(bitmap);

        var root = new Miko.Core.DomElements.DivElement
        {
            Style = new Miko.Styling.Style { Width = Length.Px(600), Height = Length.Px(600) },
        };
        var host = new Miko.Core.DomElements.DivElement { Class = "md ion-action-sheet overlay-hidden" };
        var backdrop = new Miko.Core.DomElements.DivElement { Class = "ion-backdrop action-sheet-backdrop" };
        var wrapper = new Miko.Core.DomElements.DivElement { Class = "action-sheet-wrapper ion-overlay-wrapper" };
        host.AddChild(backdrop);
        host.AddChild(wrapper);
        root.AddChild(host);

        var engine = new MikoEngine();
        engine.Initialize(root, [IonicStyleSheetFactory.CreateAllModes()], canvas, 600, 600);
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();

        // Open the sheet: the wrapper's translateY and the backdrop's opacity both change.
        host.Class = "md ion-action-sheet action-sheet-mounted action-sheet-open";
        engine.Render(canvas);

        // Both the wrapper's transform and the backdrop's opacity are in flight.
        engine.AnimationManager.ActiveTransitionCount.ShouldBe(2);

        // Mid-flight the wrapper sits between the parked and slid-in positions rather than at the
        // end state — i.e. it is genuinely animating. The manager writes each interpolated frame to
        // the element's inline Style.
        engine.AnimationManager.Update(0.2f);
        var animated = wrapper.Style?.Transform;
        animated.ShouldNotBeNull();
        var mid = TranslateYPercent(animated!.Value.Value)
            ?? throw new Exception("wrapper carries no translateY mid-animation");
        mid.ShouldBeGreaterThan(0f);
        mid.ShouldBeLessThan(100f);
    }

    [Fact]
    public async Task IonActionSheet_WithoutAnimationManager_SettlesToHiddenImmediately()
    {
        var sheet = new IonActionSheet { IsOpen = true, Buttons = Buttons() };
        sheet.Build();

        await Invoke(sheet, "OnBackdropTapAsync", new MouseEventArgs());

        // No animation manager (bare test / no DI): nothing would ever report the slide-out as
        // done, so the sheet must not stay mounted and keep swallowing taps.
        sheet.Build().ShouldHaveClass("overlay-hidden");
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

    /// <summary>
    /// Assigns the sheet's injected <c>AnimationManager</c>. The property is a protected
    /// <c>[Inject]</c> slot the DI scope normally fills; a bare-instance test has no scope, so it is
    /// set directly (same reflection approach these tests use for the private click handlers).
    /// </summary>
    private static void SetAnimations(IonActionSheet sheet, AnimationManager animations)
    {
        typeof(IonActionSheet)
            .GetProperty("Animations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(sheet, animations);
    }

    /// <summary>
    /// Runs a real transform transition on <paramref name="element"/> to completion, so the manager
    /// raises <c>TransitionCompleted</c> through its normal path (rather than the test faking the
    /// event). Mirrors what the engine does each frame: track the change, then tick past its end.
    /// </summary>
    private static void RaiseTransitionCompleted(AnimationManager animations, Element element, string property)
    {
        animations.TrackTransformChange(
            element,
            new Transform(new TransformFunction.TranslateY(Length.Percent(0))),
            new Transform(new TransformFunction.TranslateY(Length.Percent(100))),
            Transition.For(x => x.Transform).Duration(0.45f).Build());

        // One tick past the duration completes it and fires the event.
        animations.Update(0.5f);
    }

    /// <summary>The translateY percentage in a computed style, or null when there is no such transform.</summary>
    private static float? TranslateYPercent(Miko.Styling.ComputedStyle? style)
        => TranslateYPercent(style?.Transform);

    /// <summary>The translateY percentage in a transform, or null when it has no translateY.</summary>
    private static float? TranslateYPercent(Transform? transform)
        => transform?.Functions.OfType<TransformFunction.TranslateY>().FirstOrDefault()?.Y.Value;
}
