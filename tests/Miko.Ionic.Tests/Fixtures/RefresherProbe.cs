namespace Miko.Ionic.Tests.Fixtures;

/// <summary>
/// Stands in for the network request behind <see cref="RefresherPageFixture"/>'s refresh handler:
/// counts the refreshes and holds each one open until the test calls <see cref="Release"/>, so a
/// test can inspect the refresher while it is still refreshing.
/// </summary>
public sealed class RefresherProbe
{
    private TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Refreshes { get; set; }

    public Task WaitForRelease() => _release.Task;

    /// <summary>Lets the pending refresh finish; the next one waits for another release.</summary>
    public void Release()
    {
        var current = _release;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        current.TrySetResult();
    }
}
