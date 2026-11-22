using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components.Map;
using Shouldly;

namespace Miko.Components.Tests.Map;

public sealed class MapTests
{
    [Fact]
    public void Should_wrap_the_dateline_and_preserve_the_zoom_anchor()
    {
        var camera = new MapCamera(new(179.9, 40), 8);
        var location = camera.PointAt(90, -70);
        location.Longitude.ShouldBeLessThan(0);
        var zoomed = camera.ZoomAt(10, 90, -70);
        var offset = zoomed.OffsetOf(location);
        offset.X.ShouldBe(90, .00001);
        offset.Y.ShouldBe(-70, .00001);
        var panned = camera.PanBy(-90, 70);
        panned.Center.ShouldBe(location);
        camera.PointAt(0, -1e9).Latitude.ShouldBe(MapPoint.MaxLatitude, .000001);
    }

    [Fact]
    public void Should_cover_a_large_viewport_within_the_provider_image_limit()
    {
        var camera = new MapCamera(new(116.397, 39.916), 14);
        var request = MapImageRequest.CoverViewport(camera, 1601, 1000).Single();
        request.Camera.Zoom.ShouldBe(13);
        request.Width.ShouldBe(801);
        request.Height.ShouldBe(500);
        var polar = MapImageRequest.CoverViewport(new MapCamera(new(0, 90), 3), 2048, 2048);
        polar.Length.ShouldBe(4);
        polar.All(r => r.Width == 1024 && r.Height == 1024).ShouldBeTrue();
        var world = new MapCamera(new(0, 0), 3);
        world.OffsetOf(polar[0].Camera.Center).Y.ShouldBe(-512, .00001);
        world.OffsetOf(polar[3].Camera.Center).X.ShouldBe(512, .00001);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_keep_the_selected_provider_regardless_of_default_registration_order(bool defaultFirst)
    {
        var services = new ServiceCollection();
        if (defaultFirst) services.AddMikoMap();
        services.AddAMap("test-key");
        services.AddMikoMap();
        using var container = services.BuildServiceProvider();
        container.GetRequiredService<IMapProvider>().ShouldBeOfType<AMapProvider>();
        using var defaults = new ServiceCollection().AddMikoMap().BuildServiceProvider();
        defaults.GetRequiredService<IMapProvider>().ShouldBeOfType<MikoProvider>();
    }

    [Fact]
    public async Task Should_use_the_web_service_key_and_report_amap_service_errors()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            request.RequestUri!.Host.ShouldBe("restapi.amap.com");
            request.RequestUri.AbsolutePath.ShouldBe("/v3/staticmap");
            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            query["key"].ShouldBe("test&key");
            query["location"].ShouldBe("116.397000,39.916000");
            query["size"].ShouldBe("400*280");
            query["zoom"].ShouldBe("13");
            return new(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"0","info":"INVALID_USER_KEY","infocode":"10001"}""",
                    System.Text.Encoding.UTF8, "application/json")
            };
        }));
        var provider = new AMapProvider("test&key", http);
        var error = await Should.ThrowAsync<InvalidDataException>(() =>
            provider.GetImageAsync(new(new(new(116.397, 39.916), 14), 400, 280)));
        error.Message.ShouldContain("INVALID_USER_KEY");
        error.Message.ShouldNotContain("test&key");
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(respond(request));
    }
}
