using System.Diagnostics.CodeAnalysis;
using Miko.Components;
using Miko.Events;

namespace Miko.Ionic.Components;

/// <summary>Options for <see cref="IonModalController.CreateAsync(IonModalOptions)"/>. Each property
/// maps to the <see cref="IonModal"/> parameter of the same name.</summary>
public sealed class IonModalOptions
{
    /// <summary>
    /// A component type to render as the modal content (used when <see cref="Content"/> is null),
    /// with <see cref="ComponentParameters"/> as its parameters. Prefer
    /// <see cref="IonModalController.CreateAsync{TComponent}"/>, which sets it from a type argument.
    /// When assigning it directly in a trimmed (AOT) app, the source type must carry the same
    /// <c>DynamicallyAccessedMembers</c> annotation, or the parameters are trimmed away and silently
    /// keep their defaults (ISSUE-140).
    /// </summary>
    [DynamicallyAccessedMembers(ModalComponentMembers)]
    public Type? Component { get; set; }

    /// <summary>
    /// <see cref="Component"/> 所需保留的成员。<c>PublicProperties</c> 覆盖
    /// <c>RenderTreeBuilder.AddComponentParameter</c> 按名字写入的参数属性，
    /// 其余与 <c>Miko.Components.ComponentTypeMembers.Activation</c> 一致（该常量是 internal，
    /// 故在此复述而非引用）。
    /// </summary>
    internal const DynamicallyAccessedMemberTypes ModalComponentMembers =
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.NonPublicProperties
        | DynamicallyAccessedMemberTypes.NonPublicPropertiesWithInherited;

    /// <summary>Parameters for <see cref="Component"/>, keyed by its public property names.</summary>
    public IReadOnlyDictionary<string, object?> ComponentParameters { get; set; } =
        new Dictionary<string, object?>();
    /// <summary>The modal content. Takes precedence over <see cref="Component"/>.</summary>
    public RenderFragment? Content { get; set; }
    /// <summary>If true (default), a backdrop tap or the system back button dismisses the modal.</summary>
    public bool BackdropDismiss { get; set; } = true;
    /// <summary>If true (default), the backdrop is rendered.</summary>
    public bool ShowBackdrop { get; set; } = true;
    /// <summary>Extra CSS class(es) for the modal host (its <c>Class</c>).</summary>
    public string? CssClass { get; set; }
    /// <summary>Any non-null value makes it a card modal in ios mode (see <see cref="IonModal.Presenting"/>).</summary>
    public object? Presenting { get; set; }
    /// <summary>Sheet snap points, 0..1 (see <see cref="IonModal.Breakpoints"/>).</summary>
    public double[]? Breakpoints { get; set; }
    /// <summary>Initial sheet fraction; together with <see cref="Breakpoints"/> it makes a sheet modal.</summary>
    public double? InitialBreakpoint { get; set; }
    /// <summary>Shows or hides the sheet's drag handle (shown by default for sheets).</summary>
    public bool? Handle { get; set; }
}

/// <summary>Options for <see cref="IonAlertController.CreateAsync"/>. Each property maps to the
/// <see cref="IonAlert"/> parameter of the same name.</summary>
public sealed class IonAlertOptions
{
    /// <summary>The alert title.</summary>
    public string? Header { get; set; }
    /// <summary>The secondary title under the header.</summary>
    public string? SubHeader { get; set; }
    /// <summary>The body message.</summary>
    public string? Message { get; set; }
    /// <summary>The buttons to show.</summary>
    public IReadOnlyList<IonAlertButton> Buttons { get; set; } = Array.Empty<IonAlertButton>();
    /// <summary>Optional inputs, all of the same kind.</summary>
    public IReadOnlyList<IonAlertInput> Inputs { get; set; } = Array.Empty<IonAlertInput>();
    /// <summary>If true (default), tapping the backdrop dismisses the alert.</summary>
    public bool BackdropDismiss { get; set; } = true;
    /// <summary>Extra CSS class(es) for the alert host (its <c>Class</c>).</summary>
    public string? CssClass { get; set; }
}

/// <summary>Options for <see cref="IonActionSheetController.CreateAsync"/>. Each property maps to the
/// <see cref="IonActionSheet"/> parameter of the same name.</summary>
public sealed class IonActionSheetOptions
{
    /// <summary>The optional title shown above the buttons.</summary>
    public string? Header { get; set; }
    /// <summary>The optional sub-title shown under the header.</summary>
    public string? SubHeader { get; set; }
    /// <summary>The buttons to show.</summary>
    public IReadOnlyList<IonActionSheetButton> Buttons { get; set; } = Array.Empty<IonActionSheetButton>();
    /// <summary>If true (default), a backdrop tap or the system back button dismisses the sheet.</summary>
    public bool BackdropDismiss { get; set; } = true;
    /// <summary>Extra CSS class(es) for the sheet host (its <c>Class</c>).</summary>
    public string? CssClass { get; set; }
}

/// <summary>Options for <see cref="IonLoadingController.CreateAsync"/>. Each property maps to the
/// <see cref="IonLoading"/> parameter of the same name.</summary>
public sealed class IonLoadingOptions
{
    /// <summary>Optional text shown with the spinner.</summary>
    public string? Message { get; set; }
    /// <summary>Spinner name; null uses the mode default, an empty string hides it.</summary>
    public string? Spinner { get; set; }
    /// <summary>Milliseconds before the indicator dismisses itself; 0 (default) never.</summary>
    public int Duration { get; set; }
    /// <summary>If true, tapping the backdrop dismisses the indicator (default false).</summary>
    public bool BackdropDismiss { get; set; }
    /// <summary>If true (default), the backdrop is rendered.</summary>
    public bool ShowBackdrop { get; set; } = true;
    /// <summary>Extra CSS class(es) for the indicator host (its <c>Class</c>).</summary>
    public string? CssClass { get; set; }
}

/// <summary>Options for <see cref="IonPopoverController.CreateAsync"/>. Each property maps to the
/// <see cref="IonPopover"/> parameter of the same name.</summary>
public sealed class IonPopoverOptions
{
    /// <summary>The popover content.</summary>
    public RenderFragment? Content { get; set; }
    /// <summary>Pointer event used to anchor the popover to its presenting trigger.</summary>
    public MouseEventArgs? Event { get; set; }
    /// <summary>If true (default), tapping the backdrop dismisses the popover.</summary>
    public bool BackdropDismiss { get; set; } = true;
    /// <summary>If true (default), the backdrop is rendered.</summary>
    public bool ShowBackdrop { get; set; } = true;
    /// <summary>Side of the trigger to open on (see <see cref="IonPopover.Side"/>).</summary>
    public string Side { get; set; } = "bottom";
    /// <summary>Alignment with the trigger (see <see cref="IonPopover.Alignment"/>).</summary>
    public string? Alignment { get; set; }
    /// <summary>If true (default), renders the arrow element.</summary>
    public bool Arrow { get; set; } = true;
    /// <summary>Extra CSS class(es) for the popover host (its <c>Class</c>).</summary>
    public string? CssClass { get; set; }
}

/// <summary>Options for <see cref="IonToastController.CreateAsync"/>. Each property maps to the
/// <see cref="IonToast"/> parameter of the same name.</summary>
public sealed class IonToastOptions
{
    /// <summary>Optional bold header above the message.</summary>
    public string? Header { get; set; }
    /// <summary>The body message.</summary>
    public string? Message { get; set; }
    /// <summary><c>"top"</c>, <c>"middle"</c>, or <c>"bottom"</c> (default).</summary>
    public string Position { get; set; } = "bottom";
    /// <summary>Optional leading icon (an Ionicons name).</summary>
    public string? Icon { get; set; }
    /// <summary>Named palette color, e.g. <c>"success"</c>.</summary>
    public string? Color { get; set; }
    /// <summary>The buttons to show.</summary>
    public IReadOnlyList<IonToastButton> Buttons { get; set; } = Array.Empty<IonToastButton>();
    /// <summary>Milliseconds before the toast dismisses itself (role <c>"timeout"</c>); 0 (default) never.</summary>
    public int Duration { get; set; }
    /// <summary>Extra CSS class(es) for the toast host (its <c>Class</c>).</summary>
    public string? CssClass { get; set; }
}

/// <summary>Creates modals from code (Ionic's <c>modalController</c>). Inject it; <c>AddIonic()</c>
/// registers it.</summary>
public sealed class IonModalController : IonOverlayControllerBase
{
    public IonModalController(IonOverlayRegistry registry) : base(registry) { }

    /// <summary>Creates a modal from <paramref name="options"/>. Call
    /// <see cref="IonOverlayReference.PresentAsync"/> on the result to show it.</summary>
    public Task<IonOverlayReference> CreateAsync(IonModalOptions options)
        => Task.FromResult(CreateReference("modal", overlay => builder =>
        {
            var __c1 = builder.OpenComponent<IonModal>();
            __c1.IsOpen = overlay.IsOpen;
            __c1.BackdropDismiss = options.BackdropDismiss;
            __c1.ShowBackdrop = options.ShowBackdrop;
            __c1.Class = options.CssClass;
            __c1.Presenting = options.Presenting;
            __c1.Breakpoints = options.Breakpoints;
            __c1.InitialBreakpoint = options.InitialBreakpoint;
            __c1.Handle = options.Handle;
            __c1.OnDidDismiss = DismissCallback(overlay);
            __c1.ChildContent = BuildModalContent(options);
            builder.CloseComponent();
        }));

    /// <summary>
    /// Creates a modal whose content is a <typeparamref name="TComponent"/>, with
    /// <paramref name="componentParameters"/> assigned to its public properties by name;
    /// <paramref name="configure"/> sets the remaining <see cref="IonModalOptions"/>. The type
    /// argument also tells the trimmer to keep the component's parameter properties, so
    /// parameters survive AOT (ISSUE-140).
    /// </summary>
    public Task<IonOverlayReference> CreateAsync<
        [DynamicallyAccessedMembers(IonModalOptions.ModalComponentMembers)] TComponent>(
        IReadOnlyDictionary<string, object?>? componentParameters = null,
        Action<IonModalOptions>? configure = null)
        where TComponent : ComponentBase, new()
    {
        var options = new IonModalOptions
        {
            Component = typeof(TComponent),
            ComponentParameters = componentParameters ?? new Dictionary<string, object?>(),
        };
        configure?.Invoke(options);
        return CreateAsync(options);
    }

    private EventCallback<IonOverlayDismissEventArgs> DismissCallback(ControllerOverlay overlay)
        => EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
            args => CompleteDismissAsync(overlay, args));

    private static RenderFragment? BuildModalContent(IonModalOptions options)
    {
        if (options.Content is not null) return options.Content;
        if (options.Component is null) return null;

        return builder =>
        {
            builder.OpenComponent(0, options.Component);
            var sequence = 1;
            foreach (var parameter in options.ComponentParameters)
                builder.AddComponentParameter(sequence++, parameter.Key, parameter.Value);
            builder.CloseComponent();
        };
    }
}

/// <summary>Creates alerts from code (Ionic's <c>alertController</c>). Inject it; <c>AddIonic()</c>
/// registers it.</summary>
public sealed class IonAlertController : IonOverlayControllerBase
{
    public IonAlertController(IonOverlayRegistry registry) : base(registry) { }

    /// <summary>Creates an alert from <paramref name="options"/>. Call
    /// <see cref="IonOverlayReference.PresentAsync"/> on the result to show it.</summary>
    public Task<IonOverlayReference> CreateAsync(IonAlertOptions options)
        => Task.FromResult(CreateReference("alert", overlay => builder =>
        {
            var __c2 = builder.OpenComponent<IonAlert>();
            __c2.IsOpen = overlay.IsOpen;
            __c2.Header = options.Header;
            __c2.SubHeader = options.SubHeader;
            __c2.Message = options.Message;
            __c2.Buttons = options.Buttons;
            __c2.Inputs = options.Inputs;
            __c2.BackdropDismiss = options.BackdropDismiss;
            __c2.Class = options.CssClass;
            __c2.OnDidDismiss = 
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args));
            builder.CloseComponent();
        }));
}

/// <summary>Creates action sheets from code (Ionic's <c>actionSheetController</c>). Inject it;
/// <c>AddIonic()</c> registers it.</summary>
public sealed class IonActionSheetController : IonOverlayControllerBase
{
    public IonActionSheetController(IonOverlayRegistry registry) : base(registry) { }

    /// <summary>Creates an action sheet from <paramref name="options"/>. Call
    /// <see cref="IonOverlayReference.PresentAsync"/> on the result to show it.</summary>
    public Task<IonOverlayReference> CreateAsync(IonActionSheetOptions options)
        => Task.FromResult(CreateReference("action-sheet", overlay => builder =>
        {
            var __c3 = builder.OpenComponent<IonActionSheet>();
            __c3.IsOpen = overlay.IsOpen;
            __c3.Header = options.Header;
            __c3.SubHeader = options.SubHeader;
            __c3.Buttons = options.Buttons;
            __c3.BackdropDismiss = options.BackdropDismiss;
            __c3.Class = options.CssClass;
            __c3.OnDidDismiss = 
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args));
            builder.CloseComponent();
        }));
}

/// <summary>Creates loading indicators from code (Ionic's <c>loadingController</c>). Inject it;
/// <c>AddIonic()</c> registers it.</summary>
public sealed class IonLoadingController : IonOverlayControllerBase
{
    public IonLoadingController(IonOverlayRegistry registry) : base(registry) { }

    /// <summary>Creates a loading indicator from <paramref name="options"/>. Call
    /// <see cref="IonOverlayReference.PresentAsync"/> on the result to show it.</summary>
    public Task<IonOverlayReference> CreateAsync(IonLoadingOptions options)
        => Task.FromResult(CreateReference("loading", overlay => builder =>
        {
            var __c4 = builder.OpenComponent<IonLoading>();
            __c4.IsOpen = overlay.IsOpen;
            __c4.Message = options.Message;
            __c4.Spinner = options.Spinner;
            __c4.Duration = options.Duration;
            __c4.BackdropDismiss = options.BackdropDismiss;
            __c4.ShowBackdrop = options.ShowBackdrop;
            __c4.Class = options.CssClass;
            __c4.OnDidDismiss = 
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args));
            builder.CloseComponent();
        }));
}

/// <summary>Creates popovers from code (Ionic's <c>popoverController</c>). Inject it;
/// <c>AddIonic()</c> registers it.</summary>
public sealed class IonPopoverController : IonOverlayControllerBase
{
    public IonPopoverController(IonOverlayRegistry registry) : base(registry) { }

    /// <summary>Creates a popover from <paramref name="options"/>. Call
    /// <see cref="IonOverlayReference.PresentAsync"/> on the result to show it.</summary>
    public Task<IonOverlayReference> CreateAsync(IonPopoverOptions options)
        => Task.FromResult(CreateReference("popover", overlay => builder =>
        {
            var __c5 = builder.OpenComponent<IonPopover>();
            __c5.IsOpen = overlay.IsOpen;
            __c5.Event = options.Event;
            __c5.BackdropDismiss = options.BackdropDismiss;
            __c5.ShowBackdrop = options.ShowBackdrop;
            __c5.Side = options.Side;
            __c5.Alignment = options.Alignment;
            __c5.Arrow = options.Arrow;
            __c5.Class = options.CssClass;
            __c5.ChildContent = options.Content;
            __c5.OnDidDismiss = 
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args));
            builder.CloseComponent();
        }));
}

/// <summary>Creates toasts from code (Ionic's <c>toastController</c>). Inject it; <c>AddIonic()</c>
/// registers it.</summary>
public sealed class IonToastController : IonOverlayControllerBase
{
    public IonToastController(IonOverlayRegistry registry) : base(registry) { }

    /// <summary>Creates a toast from <paramref name="options"/>. Call
    /// <see cref="IonOverlayReference.PresentAsync"/> on the result to show it.</summary>
    public Task<IonOverlayReference> CreateAsync(IonToastOptions options)
        => Task.FromResult(CreateReference("toast", overlay => builder =>
        {
            var __c6 = builder.OpenComponent<IonToast>();
            __c6.IsOpen = overlay.IsOpen;
            __c6.Header = options.Header;
            __c6.Message = options.Message;
            __c6.Position = options.Position;
            __c6.Icon = options.Icon;
            __c6.Color = options.Color;
            __c6.Buttons = options.Buttons;
            __c6.Duration = options.Duration;
            __c6.Class = options.CssClass;
            __c6.OnDidDismiss = 
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args));
            builder.CloseComponent();
        }));
}
