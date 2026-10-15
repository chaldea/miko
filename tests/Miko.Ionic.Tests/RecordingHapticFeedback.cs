using Miko.Platform;

namespace Miko.Ionic.Tests;

/// <summary>
/// An <see cref="IHapticFeedback"/> that records each call as a short token (<c>"start"</c>,
/// <c>"changed"</c>, <c>"end"</c>, <c>"impact:Light"</c>), in order. Thread-safe: components end
/// selections from timer threads.
/// </summary>
public sealed class RecordingHapticFeedback : IHapticFeedback
{
    private readonly List<string> _calls = new();

    /// <summary>A snapshot of the calls so far.</summary>
    public IReadOnlyList<string> Calls
    {
        get { lock (_calls) return _calls.ToArray(); }
    }

    /// <summary>How many times <paramref name="call"/> was recorded.</summary>
    public int Count(string call)
    {
        lock (_calls) return _calls.Count(c => c == call);
    }

    public void Clear()
    {
        lock (_calls) _calls.Clear();
    }

    public void SelectionStart() => Record("start");
    public void SelectionChanged() => Record("changed");
    public void SelectionEnd() => Record("end");
    public void Impact(HapticImpactStyle style = HapticImpactStyle.Medium) => Record($"impact:{style}");

    private void Record(string call)
    {
        lock (_calls) _calls.Add(call);
    }
}
