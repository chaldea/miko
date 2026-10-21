using System.Net;
using Miko.Platform.Resources;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// ISSUE-154 BUG-05：失败与取消的加载结果不得永久占用缓存；单个消费者取消不得让共享同一加载的
/// 其他消费者失败。
///
/// <para>此前 <see cref="ResourceManager"/> 把每个路径第一次的加载任务原样缓存：失败得到的 null
/// 一直留着，文件出现、网络恢复之后同一路径也永远拿不到图；第一个调用方的取消令牌被传给共享加载，
/// 它一取消，所有等待同一路径的调用方都拿到 null。</para>
/// </summary>
public class ResourceManagerRetryTests
{
    private static byte[] EncodePng(int w, int h)
    {
        using var bmp = new SKBitmap(w, h);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public async Task A_missing_file_should_load_once_it_appears()
    {
        var path = Path.Combine(Path.GetTempPath(), $"miko-retry-{Guid.NewGuid():N}.png");
        var rm = new ResourceManager();
        try
        {
            (await rm.LoadAsync(path)).ShouldBeNull();
            rm.GetCachedResources().Single().State.ShouldBe(CachedResourceState.Failed);

            await File.WriteAllBytesAsync(path, EncodePng(8, 6));

            var bitmap = await rm.LoadAsync(path);
            bitmap.ShouldNotBeNull();
            bitmap!.Width.ShouldBe(8);
            rm.GetCachedResources().Single().State.ShouldBe(CachedResourceState.Loaded);

            // 成功的结果照旧缓存：同一路径再次请求拿到同一个位图。
            (await rm.LoadAsync(path)).ShouldBeSameAs(bitmap);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task A_failed_network_load_should_be_retried_after_the_network_recovers()
    {
        var handler = new GatedHandler { Fail = true };
        var rm = new ResourceManager(new HttpClient(handler));
        handler.Release();

        (await rm.LoadAsync("https://example.test/a.png")).ShouldBeNull();

        handler.Fail = false;
        var bitmap = await rm.LoadAsync("https://example.test/a.png");

        bitmap.ShouldNotBeNull();
        handler.Requests.ShouldBe(2);
    }

    [Fact]
    public async Task One_consumer_cancelling_should_not_fail_the_shared_load()
    {
        var handler = new GatedHandler();
        var rm = new ResourceManager(new HttpClient(handler));
        using var cancelled = new CancellationTokenSource();

        var first = rm.LoadAsync("https://example.test/b.png", cancelled.Token);
        var second = rm.LoadAsync("https://example.test/b.png");
        cancelled.Cancel();
        handler.Release();

        await Should.ThrowAsync<OperationCanceledException>(() => first);
        var bitmap = await second;
        bitmap.ShouldNotBeNull();
        handler.Requests.ShouldBe(1, "the two consumers share one request");
    }

    [Fact]
    public async Task Cancelling_every_consumer_should_drop_the_entry_so_the_next_request_loads_again()
    {
        var handler = new GatedHandler();
        var rm = new ResourceManager(new HttpClient(handler));
        using var a = new CancellationTokenSource();
        using var b = new CancellationTokenSource();

        var first = rm.LoadAsync("https://example.test/c.png", a.Token);
        var second = rm.LoadAsync("https://example.test/c.png", b.Token);
        a.Cancel();
        b.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => first);
        await Should.ThrowAsync<OperationCanceledException>(() => second);
        await handler.WaitForCancellationAsync();

        // The abandoned load unwinds on the thread pool; its entry leaves the cache once it has.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (rm.GetCachedResources().Count > 0 && DateTime.UtcNow < deadline)
            await Task.Delay(10);
        rm.GetCachedResources().ShouldBeEmpty();

        handler.Release();
        var bitmap = await rm.LoadAsync("https://example.test/c.png");
        bitmap.ShouldNotBeNull();
        handler.Requests.ShouldBe(2);
    }

    [Fact]
    public async Task An_already_cancelled_token_should_not_start_a_load()
    {
        var handler = new GatedHandler();
        var rm = new ResourceManager(new HttpClient(handler));

        await Should.ThrowAsync<OperationCanceledException>(
            () => rm.LoadAsync("https://example.test/d.png", new CancellationToken(canceled: true)));

        handler.Requests.ShouldBe(0);
        rm.GetCachedResources().ShouldBeEmpty();
    }

    /// <summary>按测试的节奏放行请求的 HTTP 处理器；返回一张 PNG，或模拟网络错误。</summary>
    private sealed class GatedHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requests;

        public bool Fail { get; set; }
        public int Requests => Volatile.Read(ref _requests);

        public void Release() => _gate.TrySetResult();

        public Task WaitForCancellationAsync() => _cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            try
            {
                await _gate.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _cancelled.TrySetResult();
                throw;
            }

            if (Fail) throw new HttpRequestException("network unreachable");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(EncodePng(4, 4)) };
        }
    }
}
