using System.Runtime.CompilerServices;

namespace Miko.Components.Tests;

/// <summary>
/// Keeps the thread pool from starving the networked tests on machines with few cores.
/// </summary>
/// <remarks>
/// Every VNC connection parks threads of the pool in blocking loops: the client's receive loop
/// (it may use the blocking compressed decoders) and the fake server's read loop. The pool starts
/// with one thread per core and adds more only about every half second, so on a 2-4 core CI runner
/// the client's send loop sometimes waits longer than <c>FakeVncServer.Drain</c>'s 250 ms window
/// for a thread, and <c>Sent(...)</c> returns an empty list although the message is on its way.
/// Raising the minimum makes the pool create threads on demand instead. The VNC tests were flaky
/// without it (limited to two cores, they failed in most runs of an unmodified <c>main</c>).
/// </remarks>
internal static class ThreadPoolSetup
{
    private const int MinWorkerThreads = 64;

    [ModuleInitializer]
    internal static void Initialize()
    {
        ThreadPool.GetMinThreads(out var worker, out var completionPort);
        if (worker < MinWorkerThreads) ThreadPool.SetMinThreads(MinWorkerThreads, completionPort);
    }
}
