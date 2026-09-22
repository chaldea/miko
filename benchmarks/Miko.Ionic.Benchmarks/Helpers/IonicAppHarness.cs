using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;

namespace Miko.Ionic.Benchmarks.Helpers;

/// <summary>
/// Builds component pages the way a real app does: under a service scope carrying the Ionic
/// registry and host platform, against the real Ionic stylesheet.
///
/// <para>The stylesheet matters more than the tree. <see cref="IonicStyleRegistry"/> attaches a
/// component's rule set the first time an instance of it is built, so the sheet a page resolves
/// against only reaches its true size after that page has been built once — which is why
/// <see cref="Create"/> builds a warm-up instance before handing back a harness. Benchmarking
/// against a cold registry would measure a sheet a fraction of the real one's size and report a
/// frame cost the app never sees.</para>
/// </summary>
internal sealed class IonicAppHarness : IDisposable
{
    private readonly ServiceProvider _services;

    private IonicAppHarness(ServiceProvider services, List<StyleSheet> styleSheets)
    {
        _services = services;
        StyleSheets = styleSheets;
    }

    /// <summary>The sheets to pass to <c>LayoutEngine.Layout</c>, in app order.</summary>
    public List<StyleSheet> StyleSheets { get; }

    /// <summary>
    /// Creates a harness whose Ionic registry is already warm for <typeparamref name="TPage"/>.
    /// </summary>
    /// <param name="platform">Host platform; drives the Ionic mode (iOS → ios, else → md).</param>
    public static IonicAppHarness Create<TPage>(HostPlatform platform = HostPlatform.Android)
        where TPage : ComponentBase, new()
    {
        var registry = new IonicStyleRegistry();
        var services = new ServiceCollection();
        services.AddOptions<IonicOptions>();
        services.AddSingleton(registry);
        services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        services.AddSingleton<IonOverlayRegistry>();
        services.AddSingleton<IonOverlayManager>();

        var provider = services.BuildServiceProvider();
        var harness = new IonicAppHarness(provider, [registry.StyleSheet]);

        // Warm the lazy per-component rule registration (see the type remarks).
        harness.Build<TPage>();
        return harness;
    }

    /// <summary>Builds a fresh instance of <typeparamref name="TPage"/> under the service scope.</summary>
    public Element Build<TPage>() where TPage : ComponentBase, new()
    {
        using (ComponentServiceScope.Push(_services))
        {
            return new TPage().Build();
        }
    }

    /// <summary>Builds <paramref name="page"/>, an already-parameterized instance.</summary>
    public Element Build(ComponentBase page)
    {
        using (ComponentServiceScope.Push(_services))
        {
            return page.Build();
        }
    }

    /// <summary>Total rule count across all sheets — the number the style stage scales with.</summary>
    public int RuleCount => StyleSheets.Sum(sheet =>
        sheet.Rules.Count
        + sheet.MediaRules.Sum(media => media.Rules.Count)
        + sheet.PseudoElementRules.Count);

    public void Dispose() => _services.Dispose();
}
