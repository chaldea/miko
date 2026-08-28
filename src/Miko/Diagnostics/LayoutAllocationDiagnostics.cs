namespace Miko.Diagnostics;

/// <summary>
/// Per-thread allocation counters for layout investigations. Counters are disabled by default;
/// normal rendering only executes the disabled guard at each instrumented allocation site.
/// </summary>
internal static class LayoutAllocationDiagnostics
{
    [ThreadStatic] private static bool t_enabled;
    [ThreadStatic] private static long t_computedStyleRentNew;
    [ThreadStatic] private static long t_computedStyleRentReused;
    [ThreadStatic] private static long t_computedStyleRentReusedGen0;
    [ThreadStatic] private static long t_computedStyleRentReusedGen1;
    [ThreadStatic] private static long t_computedStyleRentReusedGen2;
    [ThreadStatic] private static long t_computedStyleReturnAccepted;
    [ThreadStatic] private static long t_computedStyleReturnRejectedCapacity;
    [ThreadStatic] private static long t_computedStyleReturnAlreadyPooled;
    [ThreadStatic] private static long t_layoutBoxCreated;
    [ThreadStatic] private static long t_pseudoComputedStyleCreated;
    [ThreadStatic] private static long t_elementCreated;
    [ThreadStatic] private static long t_styleCreated;
    [ThreadStatic] private static long t_startAllocatedBytes;
    [ThreadStatic] private static int t_startGen0Collections;
    [ThreadStatic] private static int t_startGen1Collections;
    [ThreadStatic] private static int t_startGen2Collections;

    internal static void Begin()
    {
        t_computedStyleRentNew = 0;
        t_computedStyleRentReused = 0;
        t_computedStyleRentReusedGen0 = 0;
        t_computedStyleRentReusedGen1 = 0;
        t_computedStyleRentReusedGen2 = 0;
        t_computedStyleReturnAccepted = 0;
        t_computedStyleReturnRejectedCapacity = 0;
        t_computedStyleReturnAlreadyPooled = 0;
        t_layoutBoxCreated = 0;
        t_pseudoComputedStyleCreated = 0;
        t_elementCreated = 0;
        t_styleCreated = 0;
        t_startAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
        t_startGen0Collections = GC.CollectionCount(0);
        t_startGen1Collections = GC.CollectionCount(1);
        t_startGen2Collections = GC.CollectionCount(2);
        t_enabled = true;
    }

    internal static LayoutAllocationSnapshot End()
    {
        var snapshot = new LayoutAllocationSnapshot(
            Environment.CurrentManagedThreadId,
            t_computedStyleRentNew,
            t_computedStyleRentReused,
            t_computedStyleRentReusedGen0,
            t_computedStyleRentReusedGen1,
            t_computedStyleRentReusedGen2,
            t_computedStyleReturnAccepted,
            t_computedStyleReturnRejectedCapacity,
            t_computedStyleReturnAlreadyPooled,
            t_layoutBoxCreated,
            t_pseudoComputedStyleCreated,
            t_elementCreated,
            t_styleCreated,
            GC.GetAllocatedBytesForCurrentThread() - t_startAllocatedBytes,
            GC.CollectionCount(0) - t_startGen0Collections,
            GC.CollectionCount(1) - t_startGen1Collections,
            GC.CollectionCount(2) - t_startGen2Collections);
        t_enabled = false;
        return snapshot;
    }

    internal static void RecordComputedStyleRentNew()
    {
        if (t_enabled) t_computedStyleRentNew++;
    }

    internal static void RecordComputedStyleRentReused(object style)
    {
        if (!t_enabled) return;

        t_computedStyleRentReused++;
        switch (GC.GetGeneration(style))
        {
            case 0: t_computedStyleRentReusedGen0++; break;
            case 1: t_computedStyleRentReusedGen1++; break;
            default: t_computedStyleRentReusedGen2++; break;
        }
    }

    internal static void RecordComputedStyleReturnAccepted()
    {
        if (t_enabled) t_computedStyleReturnAccepted++;
    }

    internal static void RecordComputedStyleReturnRejectedCapacity()
    {
        if (t_enabled) t_computedStyleReturnRejectedCapacity++;
    }

    internal static void RecordComputedStyleReturnAlreadyPooled()
    {
        if (t_enabled) t_computedStyleReturnAlreadyPooled++;
    }

    internal static void RecordLayoutBoxCreated()
    {
        if (t_enabled) t_layoutBoxCreated++;
    }

    internal static void RecordPseudoComputedStyleCreated()
    {
        if (t_enabled) t_pseudoComputedStyleCreated++;
    }

    internal static void RecordElementCreated()
    {
        if (t_enabled) t_elementCreated++;
    }

    internal static void RecordStyleCreated()
    {
        if (t_enabled) t_styleCreated++;
    }

    /// <summary>
    /// Forces a compacting full collection and captures the surviving managed heap. This is
    /// intentionally explicit and is only used by retention tests and interactive diagnostics.
    /// </summary>
    internal static GCHeapSnapshot ForceFullCollectionAndCaptureHeap()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        var info = GC.GetGCMemoryInfo(GCKind.FullBlocking);
        var generations = info.GenerationInfo;
        return new GCHeapSnapshot(
            GC.GetTotalMemory(forceFullCollection: false),
            info.HeapSizeBytes,
            info.FragmentedBytes,
            GetSizeAfter(generations, 0),
            GetSizeAfter(generations, 1),
            GetSizeAfter(generations, 2),
            GetSizeAfter(generations, 3),
            GetSizeAfter(generations, 4));
    }

    private static long GetSizeAfter(ReadOnlySpan<GCGenerationInfo> generations, int index)
        => index < generations.Length ? generations[index].SizeAfterBytes : 0;
}

internal readonly record struct LayoutAllocationSnapshot(
    int ThreadId,
    long ComputedStyleRentNew,
    long ComputedStyleRentReused,
    long ComputedStyleRentReusedGen0,
    long ComputedStyleRentReusedGen1,
    long ComputedStyleRentReusedGen2,
    long ComputedStyleReturnAccepted,
    long ComputedStyleReturnRejectedCapacity,
    long ComputedStyleReturnAlreadyPooled,
    long LayoutBoxCreated,
    long PseudoComputedStyleCreated,
    long ElementCreated,
    long StyleCreated,
    long AllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections);

internal readonly record struct GCHeapSnapshot(
    long ManagedBytes,
    long HeapSizeBytes,
    long FragmentedBytes,
    long Gen0SizeBytes,
    long Gen1SizeBytes,
    long Gen2SizeBytes,
    long LargeObjectHeapSizeBytes,
    long PinnedObjectHeapSizeBytes);
