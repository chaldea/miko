using System.Diagnostics;
using Miko.Core;

namespace Miko.Platform;

/// <summary>Platform hook for translating wheel and gesture deltas into scrolling.</summary>
public interface IScrollBehavior
{
    bool ScrollBy(MikoEngine engine, float x, float y, float deltaX, float deltaY);
}

/// <summary>Optional gesture lifecycle used by behaviors that keep state between frames.</summary>
public interface IScrollGestureBehavior : IScrollBehavior
{
    void PointerDown(float x, float y);
    void PointerUp(float x, float y);
    void PointerCancel();
    void Update(MikoEngine engine, float deltaTime);
    bool HasPendingWork { get; }
}

/// <summary>Default scroll behavior used when a host does not provide one.</summary>
public sealed class DefaultScrollBehavior : IScrollBehavior
{
    public bool ScrollBy(MikoEngine engine, float x, float y, float deltaX, float deltaY)
        => engine.ScrollBy(x, y, deltaX, deltaY);
}

/// <summary>
/// Touch scrolling with exponentially decaying post-release motion (fling).
/// <para>
/// Release velocity is estimated over a sliding time window rather than from the single most
/// recent move, mirroring Android's <c>VelocityTracker</c>. Both properties of that window are
/// load-bearing:
/// </para>
/// <list type="bullet">
/// <item>Hosts deliver moves in <i>batches</i> — Android coalesces queued <c>MotionEvent</c>s and
/// high-refresh panels fire several samples per millisecond. Dividing one delta by the gap to the
/// previous sample makes velocity a function of how the host batched the input rather than of how
/// fast the finger moved; summing deltas over a real elapsed span is batch-invariant.</item>
/// <item>Samples <i>expire</i>. A finger that drags fast and then holds still has zero velocity,
/// so releasing must not fling. Keeping the last observed velocity around instead flings on a
/// release the user experiences as stationary.</item>
/// </list>
/// <para>
/// Timing goes through <see cref="TimeProvider"/>, whose default resolves to
/// <see cref="Stopwatch"/> rather than <see cref="Environment.TickCount64"/>: the latter advances
/// in ~16 ms steps on Windows, which is coarser than the sample interval being measured and
/// collapses a whole batch onto one instant. Tests substitute a fake provider — asserting on
/// release velocity with real sleeps makes the outcome a function of scheduler accuracy, and a
/// loaded machine that oversleeps past <see cref="VelocityHorizon"/> expires every sample, so the
/// release reads as stationary.
/// </para>
/// </summary>
public sealed class InertialScrollBehavior : IScrollGestureBehavior
{
    /// <summary>Speed (px/s) below which the fling is considered finished.</summary>
    private const float MinVelocity = 8f;

    /// <summary>Upper bound on release speed (px/s), matching Android's maximum fling velocity.</summary>
    private const float MaxVelocity = 8000f;

    /// <summary>
    /// Sliding window (seconds) over which release velocity is estimated. 100 ms is Android's
    /// <c>VelocityTracker</c> horizon — long enough to span a batch, short enough that a finger
    /// coming to rest reads as stopped.
    /// </summary>
    private const float VelocityHorizon = 0.1f;

    /// <summary>
    /// Floor (seconds) on the measured window span, so a lone sample taken microseconds before
    /// release cannot divide a normal delta into an enormous velocity. ~8 ms is one 120 Hz frame.
    /// </summary>
    private const float MinVelocitySpan = 0.008f;

    /// <summary>Enough slots to cover <see cref="VelocityHorizon"/> at 120 Hz with headroom.</summary>
    private const int SampleCapacity = 16;

    private readonly record struct Sample(long Timestamp, float DeltaX, float DeltaY);

    private readonly Sample[] _samples = new Sample[SampleCapacity];
    private int _sampleHead;
    private int _sampleCount;

    private readonly float _friction;
    private readonly TimeProvider _timeProvider;
    private float _x, _y, _velocityX, _velocityY;
    private bool _dragging, _inertia;

    /// <param name="friction">
    /// Exponential decay rate (per second) applied to fling velocity. The default 2.0 matches
    /// iOS <c>UIScrollViewDecelerationRateNormal</c> (0.998 per millisecond, i.e.
    /// <c>-ln(0.998) * 1000 ≈ 2.0</c>); hosts wanting a shorter, Android-style glide pass a
    /// larger value.
    /// </param>
    /// <param name="timeProvider">
    /// Clock used to timestamp move samples; defaults to <see cref="TimeProvider.System"/>, whose
    /// timestamps are <see cref="Stopwatch"/>-based. Supply a fake to drive velocity estimation
    /// deterministically in tests.
    /// </param>
    public InertialScrollBehavior(float friction = 2.0f, TimeProvider? timeProvider = null)
    {
        if (friction <= 0f)
            throw new ArgumentOutOfRangeException(nameof(friction), friction, "Friction must be positive.");
        _friction = friction;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool HasPendingWork => _inertia;

    public void PointerDown(float x, float y)
    {
        _x = x; _y = y; _velocityX = 0; _velocityY = 0;
        _sampleCount = 0; _sampleHead = 0;
        _dragging = true; _inertia = false;
    }

    public void PointerUp(float x, float y)
    {
        _x = x; _y = y; _dragging = false;
        // Estimate from the window as it stands at release: samples that have aged out are gone,
        // so a finger held still since the last move yields zero and does not fling.
        (_velocityX, _velocityY) = EstimateVelocity();
        _inertia = MathF.Abs(_velocityX) >= MinVelocity || MathF.Abs(_velocityY) >= MinVelocity;
        _sampleCount = 0; _sampleHead = 0;
    }

    public void PointerCancel()
    {
        _dragging = false; _inertia = false; _velocityX = 0; _velocityY = 0;
        _sampleCount = 0; _sampleHead = 0;
    }

    public bool ScrollBy(MikoEngine engine, float x, float y, float deltaX, float deltaY)
    {
        _x = x; _y = y;
        // Only finger-driven moves feed the estimator. Wheel notches arrive through this same
        // entry point but carry no gesture, and fling frames must not resample their own output.
        if (_dragging)
            AddSample(deltaX, deltaY);

        var changed = engine.ScrollBy(x, y, deltaX, deltaY);
        // A drag the scroll container refused (already at its boundary) carries no velocity to
        // release with, so drop the history instead of flinging off the end.
        if (!changed && _dragging)
        {
            _sampleCount = 0; _sampleHead = 0;
            _velocityX = 0; _velocityY = 0;
        }
        return changed;
    }

    public void Update(MikoEngine engine, float deltaTime)
    {
        if (!_inertia) return;
        var dt = Math.Clamp(deltaTime, 0f, 0.1f);
        if (dt <= 0f) return;

        var changed = ScrollBy(engine, _x, _y, _velocityX * dt, _velocityY * dt);
        var decay = MathF.Exp(-_friction * dt);
        _velocityX *= decay; _velocityY *= decay;
        if (!changed || (MathF.Abs(_velocityX) < MinVelocity && MathF.Abs(_velocityY) < MinVelocity))
        {
            _inertia = false; _velocityX = 0; _velocityY = 0;
        }
    }

    private void AddSample(float deltaX, float deltaY)
    {
        _samples[_sampleHead] = new Sample(_timeProvider.GetTimestamp(), deltaX, deltaY);
        _sampleHead = (_sampleHead + 1) % SampleCapacity;
        if (_sampleCount < SampleCapacity) _sampleCount++;
    }

    /// <summary>
    /// Sums the deltas still inside <see cref="VelocityHorizon"/> and divides by the real elapsed
    /// span they cover. Summing (rather than taking the newest delta alone) is what makes the
    /// estimate independent of how the host batched the moves.
    /// </summary>
    private (float x, float y) EstimateVelocity()
    {
        if (_sampleCount == 0) return (0f, 0f);

        var now = _timeProvider.GetTimestamp();
        var frequency = (float)_timeProvider.TimestampFrequency;
        float sumX = 0, sumY = 0;
        long oldest = now;
        int included = 0;

        // Walk newest-first so the first out-of-window sample ends the scan.
        for (int i = 1; i <= _sampleCount; i++)
        {
            var sample = _samples[(_sampleHead - i + SampleCapacity) % SampleCapacity];
            var age = (now - sample.Timestamp) / frequency;
            if (age > VelocityHorizon) break;

            sumX += sample.DeltaX;
            sumY += sample.DeltaY;
            oldest = sample.Timestamp;
            included++;
        }

        if (included == 0) return (0f, 0f);

        var span = MathF.Max((now - oldest) / frequency, MinVelocitySpan);
        return (
            Math.Clamp(sumX / span, -MaxVelocity, MaxVelocity),
            Math.Clamp(sumY / span, -MaxVelocity, MaxVelocity));
    }
}
