using Miko.Components;
using Miko.Events;

namespace Miko.Ionic.Components;

public sealed class IonModalOptions
{
    public Type? Component { get; set; }
    public IReadOnlyDictionary<string, object?> ComponentParameters { get; set; } =
        new Dictionary<string, object?>();
    public RenderFragment? Content { get; set; }
    public bool BackdropDismiss { get; set; } = true;
    public bool ShowBackdrop { get; set; } = true;
    public string? CssClass { get; set; }
    public object? Presenting { get; set; }
    public double[]? Breakpoints { get; set; }
    public double? InitialBreakpoint { get; set; }
    public bool? Handle { get; set; }
}

public sealed class IonAlertOptions
{
    public string? Header { get; set; }
    public string? SubHeader { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<IonAlertButton> Buttons { get; set; } = Array.Empty<IonAlertButton>();
    public IReadOnlyList<IonAlertInput> Inputs { get; set; } = Array.Empty<IonAlertInput>();
    public bool BackdropDismiss { get; set; } = true;
    public string? CssClass { get; set; }
}

public sealed class IonActionSheetOptions
{
    public string? Header { get; set; }
    public string? SubHeader { get; set; }
    public IReadOnlyList<IonActionSheetButton> Buttons { get; set; } = Array.Empty<IonActionSheetButton>();
    public bool BackdropDismiss { get; set; } = true;
    public string? CssClass { get; set; }
}

public sealed class IonLoadingOptions
{
    public string? Message { get; set; }
    public string? Spinner { get; set; }
    public int Duration { get; set; }
    public bool BackdropDismiss { get; set; }
    public bool ShowBackdrop { get; set; } = true;
    public string? CssClass { get; set; }
}

public sealed class IonPopoverOptions
{
    public RenderFragment? Content { get; set; }
    /// <summary>Pointer event used to anchor the popover to its presenting trigger.</summary>
    public MouseEventArgs? Event { get; set; }
    public bool BackdropDismiss { get; set; } = true;
    public bool ShowBackdrop { get; set; } = true;
    public string Side { get; set; } = "bottom";
    public string? Alignment { get; set; }
    public bool Arrow { get; set; } = true;
    public bool Translucent { get; set; }
    public string? CssClass { get; set; }
}

public sealed class IonToastOptions
{
    public string? Header { get; set; }
    public string? Message { get; set; }
    public string Position { get; set; } = "bottom";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public IReadOnlyList<IonToastButton> Buttons { get; set; } = Array.Empty<IonToastButton>();
    public int Duration { get; set; }
    public string? CssClass { get; set; }
}

public sealed class IonModalController : IonOverlayControllerBase
{
    public IonModalController(IonOverlayRegistry registry) : base(registry) { }

    public Task<IonOverlayReference> CreateAsync(IonModalOptions options)
        => Task.FromResult(CreateReference("modal", overlay => builder =>
        {
            builder.OpenComponent<IonModal>(0);
            builder.AddComponentParameter(1, nameof(IonModal.IsOpen), overlay.IsOpen);
            builder.AddComponentParameter(2, nameof(IonModal.BackdropDismiss), options.BackdropDismiss);
            builder.AddComponentParameter(3, nameof(IonModal.ShowBackdrop), options.ShowBackdrop);
            builder.AddComponentParameter(4, nameof(IonModal.Class), options.CssClass);
            builder.AddComponentParameter(5, nameof(IonModal.Presenting), options.Presenting);
            builder.AddComponentParameter(6, nameof(IonModal.Breakpoints), options.Breakpoints);
            builder.AddComponentParameter(7, nameof(IonModal.InitialBreakpoint), options.InitialBreakpoint);
            builder.AddComponentParameter(8, nameof(IonModal.Handle), options.Handle);
            builder.AddComponentParameter(9, nameof(IonModal.OnDidDismiss), DismissCallback(overlay));
            builder.AddComponentParameter(10, nameof(IonModal.ChildContent), BuildModalContent(options));
            builder.CloseComponent();
        }));

    public Task<IonOverlayReference> CreateAsync<TComponent>(
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

public sealed class IonAlertController : IonOverlayControllerBase
{
    public IonAlertController(IonOverlayRegistry registry) : base(registry) { }

    public Task<IonOverlayReference> CreateAsync(IonAlertOptions options)
        => Task.FromResult(CreateReference("alert", overlay => builder =>
        {
            builder.OpenComponent<IonAlert>(0);
            builder.AddComponentParameter(1, nameof(IonAlert.IsOpen), overlay.IsOpen);
            builder.AddComponentParameter(2, nameof(IonAlert.Header), options.Header);
            builder.AddComponentParameter(3, nameof(IonAlert.SubHeader), options.SubHeader);
            builder.AddComponentParameter(4, nameof(IonAlert.Message), options.Message);
            builder.AddComponentParameter(5, nameof(IonAlert.Buttons), options.Buttons);
            builder.AddComponentParameter(6, nameof(IonAlert.Inputs), options.Inputs);
            builder.AddComponentParameter(7, nameof(IonAlert.BackdropDismiss), options.BackdropDismiss);
            builder.AddComponentParameter(8, nameof(IonAlert.Class), options.CssClass);
            builder.AddComponentParameter(9, nameof(IonAlert.OnDidDismiss),
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args)));
            builder.CloseComponent();
        }));
}

public sealed class IonActionSheetController : IonOverlayControllerBase
{
    public IonActionSheetController(IonOverlayRegistry registry) : base(registry) { }

    public Task<IonOverlayReference> CreateAsync(IonActionSheetOptions options)
        => Task.FromResult(CreateReference("action-sheet", overlay => builder =>
        {
            builder.OpenComponent<IonActionSheet>(0);
            builder.AddComponentParameter(1, nameof(IonActionSheet.IsOpen), overlay.IsOpen);
            builder.AddComponentParameter(2, nameof(IonActionSheet.Header), options.Header);
            builder.AddComponentParameter(3, nameof(IonActionSheet.SubHeader), options.SubHeader);
            builder.AddComponentParameter(4, nameof(IonActionSheet.Buttons), options.Buttons);
            builder.AddComponentParameter(5, nameof(IonActionSheet.BackdropDismiss), options.BackdropDismiss);
            builder.AddComponentParameter(6, nameof(IonActionSheet.Class), options.CssClass);
            builder.AddComponentParameter(7, nameof(IonActionSheet.OnDidDismiss),
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args)));
            builder.CloseComponent();
        }));
}

public sealed class IonLoadingController : IonOverlayControllerBase
{
    public IonLoadingController(IonOverlayRegistry registry) : base(registry) { }

    public Task<IonOverlayReference> CreateAsync(IonLoadingOptions options)
        => Task.FromResult(CreateReference("loading", overlay => builder =>
        {
            builder.OpenComponent<IonLoading>(0);
            builder.AddComponentParameter(1, nameof(IonLoading.IsOpen), overlay.IsOpen);
            builder.AddComponentParameter(2, nameof(IonLoading.Message), options.Message);
            builder.AddComponentParameter(3, nameof(IonLoading.Spinner), options.Spinner);
            builder.AddComponentParameter(4, nameof(IonLoading.Duration), options.Duration);
            builder.AddComponentParameter(5, nameof(IonLoading.BackdropDismiss), options.BackdropDismiss);
            builder.AddComponentParameter(6, nameof(IonLoading.ShowBackdrop), options.ShowBackdrop);
            builder.AddComponentParameter(7, nameof(IonLoading.Class), options.CssClass);
            builder.AddComponentParameter(8, nameof(IonLoading.OnDidDismiss),
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args)));
            builder.CloseComponent();
        }));
}

public sealed class IonPopoverController : IonOverlayControllerBase
{
    public IonPopoverController(IonOverlayRegistry registry) : base(registry) { }

    public Task<IonOverlayReference> CreateAsync(IonPopoverOptions options)
        => Task.FromResult(CreateReference("popover", overlay => builder =>
        {
            builder.OpenComponent<IonPopover>(0);
            builder.AddComponentParameter(1, nameof(IonPopover.IsOpen), overlay.IsOpen);
            builder.AddComponentParameter(2, nameof(IonPopover.Event), options.Event);
            builder.AddComponentParameter(3, nameof(IonPopover.BackdropDismiss), options.BackdropDismiss);
            builder.AddComponentParameter(4, nameof(IonPopover.ShowBackdrop), options.ShowBackdrop);
            builder.AddComponentParameter(5, nameof(IonPopover.Side), options.Side);
            builder.AddComponentParameter(6, nameof(IonPopover.Alignment), options.Alignment);
            builder.AddComponentParameter(7, nameof(IonPopover.Arrow), options.Arrow);
            builder.AddComponentParameter(8, nameof(IonPopover.Translucent), options.Translucent);
            builder.AddComponentParameter(9, nameof(IonPopover.Class), options.CssClass);
            builder.AddComponentParameter(10, nameof(IonPopover.ChildContent), options.Content);
            builder.AddComponentParameter(11, nameof(IonPopover.OnDidDismiss),
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args)));
            builder.CloseComponent();
        }));
}

public sealed class IonToastController : IonOverlayControllerBase
{
    public IonToastController(IonOverlayRegistry registry) : base(registry) { }

    public Task<IonOverlayReference> CreateAsync(IonToastOptions options)
        => Task.FromResult(CreateReference("toast", overlay => builder =>
        {
            builder.OpenComponent<IonToast>(0);
            builder.AddComponentParameter(1, nameof(IonToast.IsOpen), overlay.IsOpen);
            builder.AddComponentParameter(2, nameof(IonToast.Header), options.Header);
            builder.AddComponentParameter(3, nameof(IonToast.Message), options.Message);
            builder.AddComponentParameter(4, nameof(IonToast.Position), options.Position);
            builder.AddComponentParameter(5, nameof(IonToast.Icon), options.Icon);
            builder.AddComponentParameter(6, nameof(IonToast.Color), options.Color);
            builder.AddComponentParameter(7, nameof(IonToast.Buttons), options.Buttons);
            builder.AddComponentParameter(8, nameof(IonToast.Duration), options.Duration);
            builder.AddComponentParameter(9, nameof(IonToast.Class), options.CssClass);
            builder.AddComponentParameter(10, nameof(IonToast.OnDidDismiss),
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args)));
            builder.CloseComponent();
        }));
}
