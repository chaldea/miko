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
    /// The component-facing platform service, supplying the system back-button chain, orientation
    /// control (ISSUE-138) and haptics (ISSUE-149). Null when the component is built without an ambient
    /// service scope (e.g. a bare unit test); back handling is then simply not registered.
    /// </summary>
    [Inject] protected IPlatformService? PlatformService { get; set; }

    private static readonly IHapticFeedback NoHaptics = new NullHapticFeedback();

    /// <summary>
    /// Haptic feedback for interaction cues such as picker ticks and reorder drags (ISSUE-149).
    /// Never null: without a platform service (bare unit tests) or on a platform without haptics
    /// (desktop) it is the inert implementation, so components call it unconditionally.
    /// </summary>
    protected IHapticFeedback Haptics => PlatformService?.Haptics ?? NoHaptics;

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

    /// <summary>
    /// The theme this component's stylesheet rules are generated from: the app theme
    /// (<c>AddIonic</c>), resolved for <see cref="Mode"/>, with an ancestor <see cref="ConfigProvider"/>'s
    /// values applied. For values a component computes in code — inline styles, keyframes — so they
    /// follow the app's theme like its stylesheet rules do. Shared and read-only.
    /// </summary>
    protected IonicTheme ResolvedTheme => ResolveTheme(EffectiveMode);

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
        var scope = ThemeScopeClass;
        if (!string.IsNullOrWhiteSpace(scope) && !root.HasClass(scope))
            root.Class = string.IsNullOrWhiteSpace(root.Class) ? scope : $"{root.Class} {scope}";

        return root;
    }

    /// <summary>
    /// The class that scopes this component's registered Ionic rules (<c>ion-theme-…</c>), or null
    /// when no style registry is available. It is normally added to the component's root by
    /// <see cref="Build"/>. A component whose host element is drawn somewhere else — an overlay
    /// portaled into <see cref="IonApp"/>'s layer, where its root is only the empty placeholder
    /// left in place — must put it on that host element itself, or none of its rules match there
    /// (ISSUE-153).
    /// </summary>
    /// <summary>Joins class names, skipping null or empty ones.</summary>
    protected static string Join(params string?[] classes)
        => string.Join(" ", classes.Where(c => !string.IsNullOrWhiteSpace(c)));

    protected string? ThemeScopeClass
    {
        get
        {
            if (IonicStyleRegistry == null) return null;
            var mode = EffectiveMode;
            return IonicStyleRegistry.Register(GetType(), mode, ResolveTheme(mode));
        }
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

    // The component's slot in the shared back-button chain, if any. One slot per component (not a
    // list): a component either responds to the back button or it does not. The registered thunk
    // forwards to _backHandler, so a later render can swap the handler without giving up the slot.
    private IDisposable? _backRegistration;
    private Func<bool>? _backHandler;
    private int _backPriority;

    /// <summary>
    /// Makes <paramref name="handler"/> this component's system back-button handler (ISSUE-138).
    /// The handler returns <c>true</c> when it consumed the back action (e.g. it closed itself);
    /// <c>false</c> passes it to the next handler — or to the platform default (exit the app).
    ///
    /// <para>
    /// Call it from <see cref="ComponentBase.OnParametersSet"/> with the current state; passing
    /// <c>null</c> releases the slot. Calling it again with a handler at the same priority only
    /// swaps the handler and keeps the slot, so re-rendering does not move the component ahead
    /// of handlers registered after it. The slot is released automatically on dispose.
    /// </para>
    /// </summary>
    /// <param name="handler">The handler, or null to stop responding to back.</param>
    /// <param name="priority">See <see cref="BackButtonPriority"/>: overlays outrank the menu,
    /// which outranks page navigation.</param>
    protected void RegisterBackHandler(Func<bool>? handler, int priority = BackButtonPriority.Navigation)
    {
        if (handler is null)
        {
            ClearBackHandler();
            return;
        }

        _backHandler = handler;
        if (_backRegistration is not null && _backPriority == priority)
            return;

        _backRegistration?.Dispose();
        _backPriority = priority;
        _backRegistration = PlatformService?.Back.Register(() => _backHandler?.Invoke() ?? false, priority);
    }

    /// <summary>
    /// Releases the <see cref="RegisterBackHandler"/> slot immediately. Overlays call this as soon
    /// as they start dismissing, before awaiting anything: the next back press must reach whatever
    /// is beneath them, even if the host never re-renders them closed.
    /// </summary>
    protected void ClearBackHandler()
    {
        _backRegistration?.Dispose();
        _backRegistration = null;
        _backHandler = null;
    }

    protected override void OnDispose()
    {
        ClearBackHandler();
        base.OnDispose();
    }

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
    /// Components should use this in <c>OnParametersSet</c> to construct their class attribute.
    /// </summary>
    protected ClassMapper ClassMapper { get; } = new();
}
