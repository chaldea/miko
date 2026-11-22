using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Miko.Components.Map;

/// <summary>Supplies encoded, north-up images covering the requested camera and pixel dimensions.</summary>
public interface IMapProvider
{
    string Attribution { get; }
    Task<byte[]> GetImageAsync(MapImageRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Loads the stored Beijing sample from Miko.MockServer.</summary>
public sealed class MikoProvider : IMapProvider
{
    public const string DefaultBaseUrl = "http://127.0.0.1:8080";
    private readonly Uri _endpoint;
    private readonly HttpClient _http;

    public MikoProvider(string baseUrl = DefaultBaseUrl, HttpClient? http = null)
    {
        var root = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        if (root.Scheme is not ("http" or "https")) throw new ArgumentException("Expected an HTTP(S) address.", nameof(baseUrl));
        _endpoint = new(root, "api/map");
        _http = http ?? MapServiceExtensions.SharedClient;
    }

    public string Attribution => "Miko · Beijing demo map";

    public Task<byte[]> GetImageAsync(MapImageRequest request, CancellationToken cancellationToken = default)
    {
        var center = request.Camera.Center;
        var query = FormattableString.Invariant($"?longitude={center.Longitude:R}&latitude={center.Latitude:R}&zoom={request.Camera.Zoom}&width={request.Width}&height={request.Height}");
        return _http.GetByteArrayAsync(new Uri(_endpoint, query), cancellationToken);
    }
}

/// <summary>AMap's static map Web Service. Use GCJ-02 coordinates and an AMap Web Service key.</summary>
public sealed class AMapProvider : IMapProvider
{
    private readonly HttpClient _http;
    private readonly string _key;

    public AMapProvider(string key, HttpClient? http = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _key = Uri.EscapeDataString(key);
        _http = http ?? MapServiceExtensions.SharedClient;
    }

    public string Attribution => "© 高德地图";

    public async Task<byte[]> GetImageAsync(MapImageRequest request, CancellationToken cancellationToken = default)
    {
        var center = request.Camera.Center;
        // The static service spans 512 pixels at level zero. Miko uses 256-pixel XYZ zoom levels.
        int serviceZoom = request.Camera.Zoom - 1;
        var uri = string.Create(CultureInfo.InvariantCulture,
            $"https://restapi.amap.com/v3/staticmap?location={center.Longitude:F6},{center.Latitude:F6}&zoom={serviceZoom}&size={request.Width}*{request.Height}&scale=1&key={_key}");
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        // Authentication and quota failures can arrive as JSON with an HTTP 200.
        if (response.Content.Headers.ContentType?.MediaType == "application/json" || bytes.FirstOrDefault() == '{')
        {
            using var json = JsonDocument.Parse(bytes);
            string? info = json.RootElement.TryGetProperty("info", out var value) ? value.ToString() : null;
            throw new InvalidDataException($"AMap: {info ?? "map request failed"}");
        }
        return bytes;
    }
}

public static class MapServiceExtensions
{
    internal static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(20) };

    public static IServiceCollection AddMikoMap(this IServiceCollection services, string baseUrl = MikoProvider.DefaultBaseUrl)
    {
        services.TryAddSingleton<IMapProvider>(sp => new MikoProvider(baseUrl, sp.GetService<HttpClient>()));
        return services;
    }

    public static IServiceCollection AddAMap(this IServiceCollection services, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        services.Replace(ServiceDescriptor.Singleton<IMapProvider>(sp => new AMapProvider(key, sp.GetService<HttpClient>())));
        return services;
    }
}
