using MikoApp.Media.Api;

// 无窗口自检：生成样例视频并用 FFmpeg 解码，验证文件可播放（H.264）。CI 友好。
if (args.Contains("--verify"))
{
    var clip = TestClip.EnsureSampleClip();
    Console.WriteLine($"Sample clip: {clip}");
    int frames = TestClip.Verify(clip);
    Console.WriteLine($"OK: decoded {frames} frames.");
    return;
}

// 用 FFmpeg HTTP 解复用器解码一个 URL（验证客户端经 http 加载的完整链路，含 Range 支持）。
// 用法：--verify-url http://localhost:5050/assets/sample.mp4 （需 API 已在另一进程运行）。
var verifyUrlIdx = Array.IndexOf(args, "--verify-url");
if (verifyUrlIdx >= 0 && verifyUrlIdx + 1 < args.Length)
{
    var url = args[verifyUrlIdx + 1];
    Console.WriteLine($"Decoding over HTTP: {url}");
    int frames = TestClip.Verify(url);
    Console.WriteLine($"OK: decoded {frames} frames over HTTP.");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// 允许 Miko 客户端跨源调用本 API。
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();

const string baseUrl = "http://localhost:5050";

// 24 条产品记录，每条带一个离线生成的缩略图 URL（http 网络资源，经 Miko 资源管理器加载）。
var names = new[]
{
    "Aurora Lamp", "Nimbus Chair", "Pulse Speaker", "Vertex Desk", "Halo Monitor",
    "Drift Mouse", "Echo Keyboard", "Lumen Bulb", "Cove Sofa", "Strata Shelf",
    "Onyx Mug", "Flux Charger", "Pebble Stand", "Cirrus Fan", "Mosaic Rug",
    "Ridge Backpack", "Vela Bottle", "Quartz Clock", "Nova Notebook", "Tide Planter",
    "Ember Heater", "Glide Footrest", "Loom Blanket", "Spire Vase",
};
var categories = new[] { "Lighting", "Furniture", "Audio", "Accessories", "Home" };

var products = Enumerable.Range(1, names.Length).Select(id => new Product(
    Id: id,
    Name: names[id - 1],
    Price: Math.Round(12.99m + id * 7.5m, 2),
    Category: categories[(id - 1) % categories.Length],
    ThumbnailUrl: $"{baseUrl}/assets/thumb/{id}.png")).ToList();

// 产品列表（≥20 条）。模拟少量网络延迟以演示客户端加载态。
app.MapGet("/api/products", async () =>
{
    await Task.Delay(300);
    return products;
});

// 离线生成的缩略图（SkiaSharp）。
app.MapGet("/assets/thumb/{id}.png", (int id) =>
    Results.File(AssetFactory.Thumbnail(id), "image/png"));

// 离线生成的样例视频（FFmpeg）。
// 必须启用 Range 支持：FFmpeg 的 HTTP 解复用器要 seek 到文件末尾的 moov atom，
// 否则报 "partial file / Stream ends prematurely"。PhysicalFile + enableRangeProcessing
// 让 Kestrel 正确响应 206 Partial Content（带 Accept-Ranges / Content-Range）。
app.MapGet("/assets/sample.mp4", () =>
    Results.File(AssetFactory.SampleVideoPath, "video/mp4", enableRangeProcessing: true));

Console.WriteLine($"Miko Media API listening on {baseUrl}");
app.Run(baseUrl);

record Product(int Id, string Name, decimal Price, string Category, string ThumbnailUrl);
