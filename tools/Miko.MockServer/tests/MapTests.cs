using System.Net;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components.Map;
using Miko.Hosting;
using Shouldly;

namespace Miko.MockServer.Tests;

public sealed class MapTests
{
    [Fact]
    public async Task Should_serve_the_seeded_map_at_the_requested_position_and_scale()
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http = new HttpClient { BaseAddress = new Uri(address) };
        var provider = new MikoProvider(address, http);
        var camera = MapCamera.Beijing;
        var graphics = new MikoEngineBuilder().Build().Graphics;
        using var surface = graphics.CreateSurface(300, 200);

        async Task<Color> Draw(MapCamera view, int x, int y)
        {
            var bytes = await provider.GetImageAsync(new(view, 300, 200));
            using var image = graphics.DecodeImage(bytes);
            image.ShouldNotBeNull();
            surface.Context.DrawImage(image, new(0, 0, 300, 200));
            return surface.GetPixel(x, y);
        }

        // Inside the palace grounds. The same patch of ground must follow the camera.
        var palace = await Draw(camera, 156, 117);
        palace.ShouldBe(Color.FromHex("#e6d0ac"));
        (await Draw(camera.PanBy(61, -30), 217, 87)).ShouldBe(palace);
        (await Draw(camera.ZoomAt(15), 162, 134)).ShouldBe(palace);

        using var response = await http.GetAsync("/api/map?longitude=116&latitude=40&zoom=0&width=2048&height=0");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var outside = await provider.GetImageAsync(new(new(new(0, 0), 14), 300, 200));
        XDocument.Load(new MemoryStream(outside)).Root!.Value.ShouldContain("Outside the Beijing sample area");
        await app.StopAsync();
    }
}
