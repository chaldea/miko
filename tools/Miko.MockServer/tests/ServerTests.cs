using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components.Vnc.Protocol;
using Miko.Components.Webcam.Streaming;
using Shouldly;

namespace Miko.MockServer.Tests;

public sealed class ServerTests
{
    [Fact]
    public async Task Should_stop_with_a_websocket_waiting_for_an_incremental_frame()
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = await VncClient.ConnectAsync(new(address.Replace("http://", "ws://") + "/vnc"));
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.FramebufferUpdated += () => frame.TrySetResult();
        client.Disconnected += _ => closed.TrySetResult();
        client.Start();
        await frame.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_serve_independent_interactive_vnc_sessions(bool webSocket)
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        var port = app.Services.GetRequiredService<VncServer>().Port;
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var host = webSocket ? address.Replace("http://", "ws://") + "/vnc" : "127.0.0.1";
        using var first = await VncClient.ConnectAsync(new(host, port));
        using var second = await VncClient.ConnectAsync(new(host, port));
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.FramebufferUpdated += () => changed.TrySetResult();
        second.FramebufferUpdated += () => secondReady.TrySetResult();
        first.Start();
        second.Start();
        await Task.WhenAll(changed.Task, secondReady.Task).WaitAsync(TimeSpan.FromSeconds(5));
        var before = first.Framebuffer.Pixels.ToArray();
        var secondBefore = second.Framebuffer.Pixels.ToArray();
        first.SendPointer(120, 200, 1);
        first.SendPointer(120, 200, 0);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (first.Framebuffer.Pixels.SequenceEqual(before)) await Task.Delay(20, timeout.Token);
        second.Framebuffer.Pixels.ShouldNotBe(first.Framebuffer.Pixels);
        second.Framebuffer.Pixels.ShouldBe(secondBefore);
        var clipboard = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        first.ClipboardReceived += text => clipboard.TrySetResult(text);
        first.SendClipboard("WebSocket clipboard");
        (await clipboard.Task.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBe("WebSocket clipboard");
        first.Dispose();
        second.Dispose();
        await app.StopAsync();
    }

    [Fact]
    public async Task Should_serve_hls_playlist_and_segments_with_cross_origin_headers()
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http = new HttpClient { BaseAddress = new Uri(address) };
        http.DefaultRequestHeaders.Add("Origin", "https://docs.example.com");
        using var playlist = await http.GetAsync("/media/webcam.m3u8");
        playlist.EnsureSuccessStatusCode();
        playlist.Content.Headers.ContentType!.MediaType.ShouldBe("application/vnd.apple.mpegurl");
        playlist.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe("*");
        var manifest = await playlist.Content.ReadAsStringAsync();
        manifest.ShouldContain("#EXT-X-ENDLIST");
        foreach (var file in manifest.Split('\n').Select(s => s.Trim()).Where(s => s.Length > 0 && !s.StartsWith('#')))
        {
            using var segment = await http.GetAsync("/media/" + file);
            segment.EnsureSuccessStatusCode();
            segment.Content.Headers.ContentType!.MediaType.ShouldBe("video/mp2t");
            var bytes = await segment.Content.ReadAsByteArrayAsync();
            bytes.Length.ShouldBeGreaterThan(188);
            bytes[0].ShouldBe((byte)0x47);
        }
        (await http.GetAsync("/vnc")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await app.StopAsync();
    }

    [Fact]
    public async Task Should_stream_h264_to_multiple_clients_with_monotonic_loop_timestamps()
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        var port = app.Services.GetRequiredService<RtspServer>().Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        async Task Read()
        {
            await using var source = new RtspVideoSource(new Uri($"rtsp://127.0.0.1:{port}/webcam"));
            await source.OpenAsync(timeout.Token);
            TimeSpan? previous = null;
            for (int i = 0; i < 65; i++)
            {
                var frame = (await source.ReadFrameAsync(timeout.Token))!.Value;
                frame.Data.Length.ShouldBeGreaterThan(0);
                if (previous != null) frame.Timestamp.ShouldBeGreaterThan(previous.Value);
                previous = frame.Timestamp;
            }
        }
        await Task.WhenAll(Read(), Read());
        await app.StopAsync();
    }

    [Fact]
    public async Task Should_serve_range_requests_and_validate_concurrent_danmaku_posts()
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http = new HttpClient { BaseAddress = new Uri(address) };
        using var request = new HttpRequestMessage(HttpMethod.Get, "/media/demo.mp4");
        request.Headers.Range = new(0, 31);
        using var response = await http.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        (await response.Content.ReadAsByteArrayAsync()).Length.ShouldBe(32);
        var posts = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => http.PostAsJsonAsync("/api/danmaku", new { time = i / 2.0, text = $"Comment {i}", mode = "scroll", color = "#ffffff" })));
        foreach (var post in posts) { post.StatusCode.ShouldBe(HttpStatusCode.Created); post.Dispose(); }
        using var invalid = await http.PostAsJsonAsync("/api/danmaku", new { time = -1, text = "bad" });
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var comments = (await http.GetFromJsonAsync<DanmakuComment[]>("/api/danmaku"))!;
        comments.Select(c => c.Id).Distinct().Count().ShouldBe(comments.Length);
        await app.StopAsync();
    }
}
