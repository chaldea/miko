using System.Diagnostics.CodeAnalysis;
using Miko.Components;
using Miko.Events;

namespace Miko.Ionic.Components;

public sealed class IonModalOptions
{
    /// <summary>
    /// 以动态类型呈现的 modal 内容组件。它的构造函数、参数属性与注入/级联属性都要在裁剪后存活
    /// ——<see cref="ComponentParameters"/> 是按<b>名字</b>反射写入的，裁剪器看不见那次查找
    /// （ISSUE-140）。直接赋值本属性时请确保来源类型同样带有该标注；
    /// 走 <see cref="IonModalController.CreateAsync{TComponent}"/> 则由泛型参数自动满足。
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
    /// 以强类型的内容组件创建 modal。<typeparamref name="TComponent"/> 的标注把「成员必须保留」
    /// 的需求从这个调用点传到 <see cref="IonModalOptions.Component"/>，再传到
    /// <c>RenderTreeBuilder.OpenComponent(int, Type)</c> 与按名字反射写参数的
    /// <c>AddComponentParameter</c>——链条上任一环缺标注，裁剪后传入的参数就会静默保持默认值
    /// （ISSUE-140）。
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

public sealed class IonAlertController : IonOverlayControllerBase
{
    public IonAlertController(IonOverlayRegistry registry) : base(registry) { }

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

public sealed class IonActionSheetController : IonOverlayControllerBase
{
    public IonActionSheetController(IonOverlayRegistry registry) : base(registry) { }

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

public sealed class IonLoadingController : IonOverlayControllerBase
{
    public IonLoadingController(IonOverlayRegistry registry) : base(registry) { }

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

public sealed class IonPopoverController : IonOverlayControllerBase
{
    public IonPopoverController(IonOverlayRegistry registry) : base(registry) { }

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
            __c5.Translucent = options.Translucent;
            __c5.Class = options.CssClass;
            __c5.ChildContent = options.Content;
            __c5.OnDidDismiss = 
                EventCallback.Factory.Create<IonOverlayDismissEventArgs>(this,
                    args => CompleteDismissAsync(overlay, args));
            builder.CloseComponent();
        }));
}

public sealed class IonToastController : IonOverlayControllerBase
{
    public IonToastController(IonOverlayRegistry registry) : base(registry) { }

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
