using Miko.Components;
using Miko.Platform;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Base class for all Ionic components. Mirrors the role of Bootstrap's component base —
/// a single hook point shared by the ported Ionic components.
/// <para>
/// Carries the active Ionic visual mode (<c>"md"</c> / <c>"ios"</c>) resolved from the injected
/// <see cref="IPlatformInfo"/> (the host platform). Components prepend <see cref="Mode"/> to their
/// root element's class so the mode-scoped stylesheet rules apply (e.g. <c>class="md ion-header"</c>).
/// When no platform service is available (e.g. a bare unit test with no service scope), the mode
/// falls back to Material Design, matching Ionic's default.
/// </para>
/// </summary>
public abstract class IonicComponentBase : ComponentBase
{
    /// <summary>
    /// The host platform, supplied by the platform implementation. May be null when the
    /// component is built without an ambient service scope (e.g. a bare unit test); the
    /// <see cref="Mode"/> accessor then falls back to Material Design.
    /// </summary>
    [Inject] protected IPlatformInfo? PlatformInfo { get; set; }

    /// <summary>
    /// The active Ionic mode class for the current platform: <c>"ios"</c> on iOS, <c>"md"</c>
    /// otherwise. Prepend this to a component's root class (see the per-component
    /// <c>OnParametersSet</c>) so the mode-scoped stylesheet rules take effect.
    /// </summary>
    protected string Mode => IonicModeResolver.ResolveClass(PlatformInfo);

    /// <summary>
    /// Additional CSS class names to apply to the component's root element.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Inline styles to apply to the component's root element.
    /// </summary>
    [Parameter] public Style? Style { get; set; }

    /// <summary>
    /// Utility for building CSS class names dynamically.
    /// Components should use this in <see cref="OnParametersSet"/> to construct their class attribute.
    /// </summary>
    protected ClassMapper ClassMapper { get; } = new();
}
