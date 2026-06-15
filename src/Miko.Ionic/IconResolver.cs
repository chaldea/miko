using System.Collections.Concurrent;
using Miko.Common;

namespace Miko.Ionic;

/// <summary>
/// Resolves Ionicons SVG icon names to <see cref="BackgroundImage"/> instances backed by
/// the SVG resources embedded in this assembly (<c>Resources/svg/&lt;name&gt;.svg</c>).
/// Results are cached so the same SVG is only decoded once.
/// </summary>
public static class IconResolver
{
    private const string ResourcePrefix = "Miko.Ionic.Resources.svg.";

    private static readonly ConcurrentDictionary<string, BackgroundImage?> _cache = new();

    /// <summary>
    /// Loads the icon with the given kebab-case name (e.g. <c>"triangle"</c>, see
    /// <see cref="Ionicons"/> for strongly-typed constants). Returns <c>null</c> if no
    /// matching SVG resource is embedded.
    /// </summary>
    public static BackgroundImage? Load(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return null;

        return _cache.GetOrAdd(iconName, static name =>
        {
            var resourceName = ResourcePrefix + name + ".svg";
            try
            {
                return BackgroundImage.FromResource(typeof(IconResolver).Assembly, resourceName);
            }
            catch (InvalidOperationException)
            {
                // Unknown icon name — resource not found.
                return null;
            }
        });
    }
}
