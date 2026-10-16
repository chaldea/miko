namespace Miko.Ionic.Tests.Fixtures;

/// <summary>
/// Stands in for the network request behind <see cref="InfiniteScrollPageFixture"/>'s load handler:
/// counts the loads and holds each one open until the test calls <see cref="Release"/>, so a test
/// can scroll while the infinite scroll is still loading and finish the load on demand.
/// </summary>
public sealed class InfiniteScrollProbe
{
    private TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Loads { get; set; }

    /// <summary>Rows on the page when it opens (the demo starts with 15).</summary>
    public int InitialRows { get; set; } = 15;

    /// <summary>Rows appended per load (the demo adds 5).</summary>
    public int BatchSize { get; set; } = 5;

    public Task WaitForRelease() => _release.Task;

    /// <summary>Lets the pending load finish; the next one waits for another release.</summary>
    public void Release()
    {
        var current = _release;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        current.TrySetResult();
    }
}
