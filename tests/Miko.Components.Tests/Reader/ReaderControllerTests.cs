using Miko.Components.Reader;
using Shouldly;

namespace Miko.Components.Tests.Reader;

public sealed class ReaderControllerTests
{
    internal sealed class Source(int count) : IReaderSource
    {
        public ReaderDocument Document { get; } = new("A book", Enumerable.Range(0, count).Select(i => new ReaderEntry($"{i}", $"Page {i}")).ToArray());
        public List<string> Reads { get; } = [];
        public int DirectoryRequests { get; private set; }
        public Func<ReaderEntry, CancellationToken, Task<ReaderContent>>? Read { get; set; }
        public Task<ReaderDocument> GetDocumentAsync(CancellationToken cancellationToken = default) { DirectoryRequests++; return Task.FromResult(Document); }
        public Task<ReaderContent> ReadAsync(ReaderEntry entry, CancellationToken cancellationToken = default)
        {
            Reads.Add(entry.Id);
            return Read?.Invoke(entry, cancellationToken) ?? Task.FromResult<ReaderContent>(new ReaderTextContent(entry.Id));
        }
    }

    [Fact]
    public async Task Loads_Only_Requested_Pages_And_Reuses_Cached_Pages()
    {
        var source = new Source(10);
        using var controller = new ReaderController();
        await controller.OpenAsync(source);
        source.Reads.ShouldBe(["0"]);
        await controller.GoToAsync(5);
        await controller.PreviousAsync();
        await controller.NextAsync();
        source.DirectoryRequests.ShouldBe(1);
        source.Reads.ShouldBe(["0", "5", "4"]);
        controller.Snapshot.Index.ShouldBe(5);
    }

    [Fact]
    public async Task Double_Spread_Ends_On_An_Unpaired_Page_And_Goes_Back_By_Two()
    {
        var source = new Source(5);
        using var controller = new ReaderController();
        await controller.OpenAsync(source, pageSize: 2);
        controller.Snapshot.Content.Count.ShouldBe(2);
        await controller.NextAsync(); await controller.NextAsync(); await controller.NextAsync();
        controller.Snapshot.Index.ShouldBe(4);
        controller.Snapshot.Content.Count.ShouldBe(1);
        await controller.PreviousAsync();
        controller.Snapshot.Index.ShouldBe(2);
        await controller.GoToAsync(999);
        controller.Snapshot.Index.ShouldBe(4);
    }

    [Fact]
    public async Task Late_Content_From_An_Old_Source_Cannot_Replace_The_New_Source()
    {
        var pending = new TaskCompletionSource<ReaderContent>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken cancelled = default;
        var old = new Source(1) { Read = (_, ct) => { cancelled = ct; return pending.Task; } };
        var current = new Source(2);
        using var controller = new ReaderController();
        var loading = controller.OpenAsync(old);
        await controller.OpenAsync(current, 1);
        cancelled.IsCancellationRequested.ShouldBeTrue();
        pending.SetResult(new ReaderTextContent("stale"));
        await loading;
        controller.Snapshot.Index.ShouldBe(1);
        controller.Snapshot.Content.Single().ShouldBe(new ReaderTextContent("1"));
    }

    [Fact]
    public async Task Late_Directory_And_Late_Navigation_Are_Ignored()
    {
        var directory = new TaskCompletionSource<ReaderDocument>(TaskCreationOptions.RunContinuationsAsynchronously);
        var late = new DelegateReaderSource(_ => directory.Task, (_, _) => throw new Exception("Must not read the old directory"));
        using var controller = new ReaderController();
        var loading = controller.OpenAsync(late);
        var source = new Source(3);
        await controller.OpenAsync(source);
        directory.SetResult(new("Old", [new("old", "Old")]));
        await loading;
        var page = new TaskCompletionSource<ReaderContent>(TaskCreationOptions.RunContinuationsAsynchronously);
        source.Read = (entry, _) => entry.Id == "1" ? page.Task : Task.FromResult<ReaderContent>(new ReaderTextContent(entry.Id));
        var moving = controller.GoToAsync(1);
        await controller.GoToAsync(2);
        page.SetResult(new ReaderTextContent("old page"));
        await moving;
        controller.Snapshot.Content.Single().ShouldBe(new ReaderTextContent("2"));
    }

    [Fact]
    public async Task Cancellation_Restores_Last_Page_And_Disposal_Suppresses_Completion()
    {
        using var controller = new ReaderController();
        var source = new Source(2);
        await controller.OpenAsync(source);
        source.Read = async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return new ReaderTextContent(""); };
        using var cancellation = new CancellationTokenSource();
        var loading = controller.GoToAsync(1, cancellationToken: cancellation.Token);
        cancellation.Cancel();
        await loading;
        controller.Snapshot.State.ShouldBe(ReaderState.Ready);
        controller.Snapshot.Index.ShouldBe(0);
        var pending = new TaskCompletionSource<ReaderContent>(TaskCreationOptions.RunContinuationsAsynchronously);
        source.Read = (_, _) => pending.Task;
        var next = controller.GoToAsync(1);
        int changes = 0;
        controller.Changed += () => changes++;
        controller.Dispose();
        pending.SetResult(new ReaderTextContent("too late"));
        await next;
        changes.ShouldBe(0);
        controller.Snapshot.State.ShouldBe(ReaderState.Idle);
    }

    [Fact]
    public async Task Errors_Are_Retryable_And_Cache_Eviction_Respects_Bytes_And_Recency()
    {
        var source = new Source(4);
        using var controller = new ReaderController(cacheEntries: 2, cacheBytes: 4);
        await controller.OpenAsync(source);
        await controller.GoToAsync(1);
        await controller.GoToAsync(0);
        await controller.GoToAsync(2);
        await controller.GoToAsync(1);
        source.Reads.ShouldBe(["0", "1", "2", "1"]);
        source.Read = (_, _) => throw new IOException("offline");
        await controller.GoToAsync(3);
        controller.Snapshot.State.ShouldBe(ReaderState.Error);
        controller.Snapshot.Index.ShouldBe(3);
        source.Read = null;
        await controller.RetryAsync();
        controller.Snapshot.State.ShouldBe(ReaderState.Ready);
        controller.Snapshot.Content.Single().ShouldBe(new ReaderTextContent("3"));
    }

    [Fact]
    public async Task Empty_Directory_And_No_Source_Are_Valid_States()
    {
        using var controller = new ReaderController();
        var source = new Source(0);
        await controller.OpenAsync(source, 100);
        controller.Snapshot.State.ShouldBe(ReaderState.Ready);
        controller.Snapshot.Index.ShouldBe(0);
        await controller.NextAsync(); await controller.PreviousAsync();
        source.Reads.ShouldBeEmpty();
        await controller.OpenAsync(null);
        controller.Snapshot.ShouldBe(ReaderSnapshot.Empty);
    }

    [Fact]
    public async Task Publishes_Directory_While_First_Page_Is_Still_Loading_And_Retry_Refetches_Content()
    {
        var pending = new TaskCompletionSource<ReaderContent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new Source(2) { Read = (_, _) => pending.Task };
        using var controller = new ReaderController();
        bool sawDirectory = false;
        controller.Changed += () => sawDirectory |= controller.Snapshot.State == ReaderState.Loading && controller.Snapshot.Document != null;
        var loading = controller.OpenAsync(source);
        sawDirectory.ShouldBeTrue();
        pending.SetResult(new ReaderTextContent("old"));
        await loading;
        source.Read = (_, _) => Task.FromResult<ReaderContent>(new ReaderTextContent("corrected"));
        await controller.RetryAsync();
        source.Reads.ShouldBe(["0", "0"]);
        controller.Snapshot.Content.Single().ShouldBe(new ReaderTextContent("corrected"));
    }

    [Fact]
    public async Task Invalid_Directory_Is_Reported_Without_Loading_Content()
    {
        var source = new DelegateReaderSource(_ => Task.FromResult(new ReaderDocument("Bad", [new("x", "One"), new("x", "Two")])), (_, _) => throw new Exception("Must not read"));
        using var controller = new ReaderController();
        await controller.OpenAsync(source);
        controller.Snapshot.Error.ShouldBeOfType<InvalidDataException>();
    }
}
