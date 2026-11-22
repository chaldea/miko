using System.Text.RegularExpressions;

namespace Miko.MockServer;

public static class MockApplication
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (builder.Configuration["urls"] == null) builder.WebHost.UseUrls("http://0.0.0.0:8080");
        var options = new MockOptions();
        builder.Configuration.GetSection("Mock").Bind(options);
        options.Validate();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<MediaLibrary>();
        builder.Services.AddSingleton<MapFixture>();
        builder.Services.AddSingleton<VncServer>();
        builder.Services.AddSingleton<RtspServer>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<VncServer>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<RtspServer>());
        builder.Services.AddSingleton<DanmakuStore>();
        // Public, credential-free demo resources must be readable by a separately hosted WASM app.
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
            .WithExposedHeaders("Content-Range", "Accept-Ranges")));
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNameCaseInsensitive = true);
        builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 16 * 1024);
        var app = builder.Build();
        app.UseCors();
        app.UseWebSockets();
        app.MapGet("/vnc", VncWebSocket.HandleAsync);
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/", () => Results.Ok(new { map = "/api/map?longitude=116.397&latitude=39.916&zoom=14&width=640&height=480", video = "/media/demo.mp4", webcamHls = "/media/webcam.m3u8", vncWebSocket = "/vnc", danmaku = "/api/danmaku", vncPort = options.VncPort, rtspPort = options.RtspPort, rtspPath = "/webcam" }));
        app.MapMethods("/media/webcam.m3u8", ["GET", "HEAD"], () =>
            Results.File(Path.Combine(options.MediaDirectory, "webcam.m3u8"), "application/vnd.apple.mpegurl"));
        app.MapMethods("/media/{segment:regex(^webcam-[0-9]+\\.ts$)}", ["GET", "HEAD"], (string segment) =>
            Results.File(Path.Combine(options.MediaDirectory, segment), "video/mp2t", enableRangeProcessing: true));
        app.MapMethods("/media/demo.mp4", ["GET", "HEAD"], (MediaLibrary media) =>
            Results.File(media.Mp4Path, "video/mp4", enableRangeProcessing: true));
        app.MapGet("/api/map", (double longitude, double latitude, int zoom, int width, int height, MapFixture map) =>
            map.Render(longitude, latitude, zoom, width, height));
        app.MapGet("/api/danmaku", (double? from, double? to, DanmakuStore store) =>
        {
            if (from is { } f && (!double.IsFinite(f) || f < 0) || to is { } t && (!double.IsFinite(t) || t < 0) || from > to)
                return Results.BadRequest(new { error = "Expected 0 <= from <= to (seconds)." });
            return Results.Ok(store.Get(from ?? 0, to ?? double.MaxValue));
        });
        app.MapGet("/api/danmaku/{id:long}", (long id, DanmakuStore store) =>
            store.GetById(id) is { } item ? Results.Ok(item) : Results.NotFound());
        app.MapPost("/api/danmaku", (DanmakuInput input, DanmakuStore store) =>
        {
            if (!double.IsFinite(input.Time) || input.Time is < 0 or > MediaLibrary.DurationSeconds ||
                string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 200 ||
                input.Mode is not ("scroll" or "top" or "bottom") ||
                input.Color == null || !Regex.IsMatch(input.Color, "^#[0-9a-fA-F]{6}$"))
                return Results.BadRequest(new { error = "Use time 0..12 seconds, text 1..200 characters, mode scroll/top/bottom and color #RRGGBB." });
            var item = store.Add(input);
            return Results.Created($"/api/danmaku/{item.Id}", item);
        });
        return app;
    }
}

public sealed class MockOptions
{
    public string ListenAddress { get; set; } = "0.0.0.0";
    public int VncPort { get; set; } = 5900;
    public int RtspPort { get; set; } = 8554;
    public int MaxConnections { get; set; } = 64;
    public string MediaDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Media");

    internal void Validate()
    {
        if (!System.Net.IPAddress.TryParse(ListenAddress, out _) || VncPort is < 0 or > 65535 || RtspPort is < 0 or > 65535 || MaxConnections is < 1 or > 1024)
            throw new ArgumentException("Invalid Mock listener address, port or connection limit.");
    }
}

public sealed record DanmakuInput(double Time, string Text, string Mode = "scroll", string Color = "#ffffff");
public sealed record DanmakuComment(long Id, double Time, string Text, string Mode, string Color);

/// <summary>Bounded, shared demo timeline. IDs and insertion are atomic; restart resets it.</summary>
public sealed class DanmakuStore
{
    private readonly object _gate = new();
    private readonly Queue<DanmakuComment> _items = new();
    private long _id;

    public DanmakuStore()
    {
        Add(new(1, "Welcome to Miko", "top", "#67e8f9"));
        Add(new(3, "Local mock video + danmaku"));
        Add(new(6, "Send your own comment!", "bottom", "#fbbf24"));
    }

    public DanmakuComment Add(DanmakuInput input)
    {
        lock (_gate)
        {
            var item = new DanmakuComment(++_id, input.Time, input.Text.Trim(), input.Mode, input.Color);
            if (_items.Count == 1000) _items.Dequeue();
            _items.Enqueue(item);
            return item;
        }
    }

    public DanmakuComment[] Get(double from, double to)
    {
        lock (_gate) return _items.Where(x => x.Time >= from && x.Time <= to).OrderBy(x => x.Time).ThenBy(x => x.Id).ToArray();
    }

    public DanmakuComment? GetById(long id) { lock (_gate) return _items.FirstOrDefault(x => x.Id == id); }
}
