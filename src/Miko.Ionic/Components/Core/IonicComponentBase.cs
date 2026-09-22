using Microsoft.Extensions.Options;
using Miko.Components;
using Miko.Core;
using Miko.Platform;
using Miko.Routing;
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
    /// The app's navigation manager, used by components with default navigation behavior
    /// (e.g. <see cref="IonItem"/> with <c>Href</c>, <see cref="IonBackButton"/>'s nav-pop,
    /// <see cref="IonTabs"/> root switching). Null when no service scope is available
    /// (bare unit tests) — default navigation is then skipped and only <c>OnClick</c> fires.
    /// </summary>
    [Inject] protected NavigationManager? Navigation { get; set; }

    /// <summary>Shared registry that attaches the current component family's rules on demand.</summary>
    [Inject] protected IonicStyleRegistry? IonicStyleRegistry { get; set; }

    /// <summary>Application-level theme configuration supplied by <see cref="IonicExtensions.AddIonic"/>.</summary>
    [Inject] protected IOptions<IonicOptions>? IonicOptions { get; set; }

    /// <summary>Local partial theme provided by an ancestor <see cref="ConfigProvider"/>.</summary>
    [CascadingParameter] protected IonicTheme? CascadingTheme { get; set; }

    /// <summary>
    /// The active Ionic mode class for the current platform: <c>"ios"</c> on iOS, <c>"md"</c>
    /// otherwise. Prepend this to a component's root class (see the per-component
    /// <c>OnParametersSet</c>) so the mode-scoped stylesheet rules take effect.
    /// </summary>
    protected string Mode => EffectiveMode == IonicMode.Ios ? "ios" : "md";

    private IonicMode EffectiveMode => IonicOptions?.Value switch
    {
        { Platform: HostPlatform.Ios } => IonicMode.Ios,
        { Platform: not null } => IonicMode.Md,
        { Mode: IonicMode.Ios } => IonicMode.Ios,
        { Theme.Mode: IonicMode.Ios } => IonicMode.Ios,
        _ => IonicModeResolver.Resolve(PlatformInfo),
    };

    /// <summary>
    /// Builds the component and attaches the resolved theme scope to its root. Keeping this at
    /// the common base makes lazy registration apply to every Ionic component consistently.
    /// </summary>
    public override Element Build()
    {
        return ApplyThemeScope(base.Build());
    }

    /// <summary>
    /// Applies the theme scope when a component refreshes its attached root through
    /// <see cref="ComponentBase.StateHasChanged"/>. That path builds directly from the Razor
    /// render tree and therefore does not call <see cref="Build"/>.
    /// </summary>
    protected override Element BuildNew()
    {
        return ApplyThemeScope(base.BuildNew());
    }

    private Element ApplyThemeScope(Element root)
    {
        if (IonicStyleRegistry == null)
            return root;

        var mode = EffectiveMode;
        var resolvedTheme = ResolveTheme(mode);
        var scope = IonicStyleRegistry.Register(GetType(), mode, resolvedTheme);
        if (!string.IsNullOrWhiteSpace(scope))
            root.Class = string.IsNullOrWhiteSpace(root.Class) ? scope : $"{root.Class} {scope}";

        return root;
    }

    // Resolved themes, keyed by the three inputs that determine one: the app-level theme, the
    // mode, and the ancestor ConfigProvider's partial theme (ISSUE-145).
    //
    // Resolving is expensive and was happening once per component per build: ResolveForMode
    // constructs a complete mode theme from scratch (a few hundred token assignments, ~38 KB)
    // and then copies every specified value over it. On a page with 63 Ionic components that was
    // ~2.3 MB and ~1.0 ms per build — 84% of the build stage's allocation — to produce, almost
    // always, a value identical to the one the previous component just computed. The build stage
    // runs on every route navigation, which is exactly the frame users perceive as the page
    // switch delay.
    //
    // The inputs are reference-compared rather than value-compared: themes are mutable, so two
    // equal-looking instances may diverge later, and identity is what callers actually hold
    // stable (AddIonic keeps one options theme; a ConfigProvider keeps one partial theme).
    // A ConditionalWeakTable-like keying is unnecessary — the entry count is bounded by the
    // number of distinct (theme, mode, cascading theme) triples an app uses, which is tiny.
    private static readonly Dictionary<ThemeKey, IonicTheme> ResolvedThemes = new();
    private static readonly object ResolvedThemesGate = new();

    private readonly record struct ThemeKey(IonicTheme? Source, IonicMode Mode, IonicTheme? Cascading);

    private IonicTheme ResolveTheme(IonicMode mode)
    {
        var source = IonicOptions?.Value.Theme;
        var key = new ThemeKey(source, mode, CascadingTheme);

        lock (ResolvedThemesGate)
        {
            if (ResolvedThemes.TryGetValue(key, out var cached))
                return cached;
        }

        var resolved = (source ?? new IonicTheme()).ResolveForMode(mode);
        CascadingTheme?.ApplySpecifiedValuesTo(resolved);
        // This instance is now shared by every component resolving the same key and is never
        // written to again, so it may memoize its style keys.
        resolved.MarkImmutable();

        lock (ResolvedThemesGate)
        {
            // Another thread may have resolved the same key meanwhile; either instance is
            // equally valid, so keep whichever landed first and let this one be collected.
            if (ResolvedThemes.TryGetValue(key, out var raced))
                return raced;
            ResolvedThemes[key] = resolved;
        }

        return resolved;
    }

    /// <summary>
    /// Drops the resolved-theme cache. Only needed when a theme instance is mutated in place
    /// after components have already resolved against it — the cache keys on instance identity,
    /// so an in-place edit is otherwise invisible to it. Tests that mutate a shared theme between
    /// renders call this; applications configure a theme once and never need it.
    /// </summary>
    public static void InvalidateResolvedThemes()
    {
        lock (ResolvedThemesGate)
        {
            ResolvedThemes.Clear();
        }
    }

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
