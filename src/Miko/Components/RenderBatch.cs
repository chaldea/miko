namespace Miko.Components;

/// <summary>
/// Defers the re-renders requested while an event is being handled until its synchronous part has
/// finished, then performs them in request order (ISSUE-154).
///
/// <para>A parent re-render reuses child component instances whose identity is unchanged, so it
/// assigns the parent's current values to the very instance whose handler is still running. Rendered
/// synchronously, the first callback a handler invokes would re-render the parent with values the
/// handler has not reported yet: <c>IonCheckbox</c> sets <c>Checked</c>, raises
/// <c>IndeterminateChanged</c>, and the parent's immediate re-render puts the old <c>Checked</c> back
/// before <c>CheckedChanged</c> is raised. Batching — as Blazor does while dispatching an event —
/// renders the parent once, after every callback has updated it.</para>
///
/// <para>A queued render is skipped when the component has rendered since it was queued (a parent
/// render rebuilt it, or it was already flushed) or has been disposed. That also keeps a child from
/// re-rendering on its own right after its parent rebuilt it, which would drop what the parent's build
/// post-pass added to the child's root.</para>
///
/// <para>Only the synchronous part is batched. Continuations after an <c>await</c> run later and
/// render immediately, as in Blazor.</para>
/// </summary>
internal static class RenderBatch
{
    [ThreadStatic] private static int t_depth;
    [ThreadStatic] private static List<(ComponentBase Component, int Version)>? t_pending;

    /// <summary>Starts (or nests in) a batch. Must be paired with <see cref="Exit"/>.</summary>
    public static void Enter() => t_depth++;

    /// <summary>Ends a batch; the outermost exit performs the queued renders.</summary>
    public static void Exit()
    {
        if (t_depth > 1)
        {
            t_depth--;
            return;
        }

        // Still "inside" while flushing: a render that requests further renders appends them to the
        // queue being drained instead of recursing.
        try
        {
            var pending = t_pending;
            if (pending is null) return;
            for (int i = 0; i < pending.Count; i++)
            {
                var (component, version) = pending[i];
                if (component.RenderVersion == version) component.RenderNow();
            }
        }
        finally
        {
            t_pending?.Clear();
            t_depth = 0;
        }
    }

    /// <summary>Queues a render of <paramref name="component"/> when a batch is active.</summary>
    public static bool TryDefer(ComponentBase component)
    {
        if (t_depth == 0) return false;
        (t_pending ??= new()).Add((component, component.RenderVersion));
        return true;
    }
}
