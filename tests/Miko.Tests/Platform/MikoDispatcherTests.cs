using Miko.Platform;
using Shouldly;

namespace Miko.Tests.Platform;

public class MikoDispatcherTests
{
    [Fact]
    public void Post_ShouldEnqueueAction()
    {
        var dispatcher = new MikoDispatcher();
        var executed = false;

        dispatcher.Post(() => executed = true);

        executed.ShouldBeFalse(); // Not executed until Drain
        dispatcher.HasPendingActions.ShouldBeTrue();
    }

    [Fact]
    public void Drain_ShouldExecuteAllQueuedActions()
    {
        var dispatcher = new MikoDispatcher();
        var count = 0;

        dispatcher.Post(() => count++);
        dispatcher.Post(() => count++);
        dispatcher.Post(() => count++);

        dispatcher.Drain();

        count.ShouldBe(3);
        dispatcher.HasPendingActions.ShouldBeFalse();
    }

    [Fact]
    public void Drain_ShouldExecuteActionsInOrder()
    {
        var dispatcher = new MikoDispatcher();
        var order = new List<int>();

        dispatcher.Post(() => order.Add(1));
        dispatcher.Post(() => order.Add(2));
        dispatcher.Post(() => order.Add(3));

        dispatcher.Drain();

        order.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Drain_ShouldHandleExceptionsWithoutCrashing()
    {
        var dispatcher = new MikoDispatcher();
        var executed = false;

        dispatcher.Post(() => throw new InvalidOperationException("Test exception"));
        dispatcher.Post(() => executed = true);

        // Should not throw; exception is caught and logged
        Should.NotThrow(() => dispatcher.Drain());

        // Second action should still execute
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Post_FromMultipleThreads_ShouldBeThreadSafe()
    {
        var dispatcher = new MikoDispatcher();
        var count = 0;
        var threads = 10;
        var actionsPerThread = 100;

        var tasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < actionsPerThread; i++)
            {
                dispatcher.Post(() => Interlocked.Increment(ref count));
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        dispatcher.Drain();

        count.ShouldBe(threads * actionsPerThread);
    }
}
