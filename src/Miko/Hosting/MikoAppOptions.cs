using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Miko.Common;
using Miko.Core;
using Miko.Platform;
using Miko.Routing;
using Miko.Styling;

namespace Miko.Hosting;

public class MikoAppOptions
{
    public string Title { get; set; } = "Miko Application";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public Func<Element>? RootComponentFactory { get; set; }
    public List<StyleSheet> StyleSheets { get; set; } = new();
    public Assembly[]? RouteAssemblies { get; set; }
    public Action<Router>? RouteConfigurator { get; set; }

    [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)]
    public Type? DefaultLayout { get; set; }

    public List<FontRegistration> Fonts { get; set; } = new();
    public List<Func<MikoKey, bool>> GlobalKeyDownHandlers { get; set; } = new();
    public List<Action<IServiceProvider>> PostInitHooks { get; set; } = new();
    public bool EnableHotReload { get; set; } = false;

    /// <summary>
    /// Per-platform overrides of the scrollbar presentation (ISSUE-156). Platforms not listed use
    /// <see cref="PlatformScrollbars.GetDefault"/>. Set through <c>MikoAppBuilder.UseScrollbarStyle</c>.
    /// </summary>
    public Dictionary<HostPlatform, ScrollbarStyle> ScrollbarStyles { get; set; } = new();

    /// <summary>The scrollbar presentation this app uses on <paramref name="platform"/>.</summary>
    public ScrollbarStyle ResolveScrollbarStyle(HostPlatform platform)
        => ScrollbarStyles.TryGetValue(platform, out var style) ? style : PlatformScrollbars.GetDefault(platform);
}

public class FontRegistration
{
    public required string FamilyName { get; init; }
    public required Assembly Assembly { get; init; }
    public required string ResourceName { get; init; }
}
