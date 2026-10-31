using Miko.Common;
using Miko.Events;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;

namespace Miko.Components.Player;

// Showing and hiding the controls, and the gestures on the picture.
public sealed partial class MikoPlayer
{
    /// <summary>A second tap within this window is a double tap; a single tap waits it out.</summary>
    private static readonly TimeSpan DoubleTapWindow = TimeSpan.FromMilliseconds(300);

    // Timers fire on a pool thread and post to the engine's dispatcher. Each arm bumps its
    // generation; a posted callback whose generation is stale was re-armed or cancelled meanwhile.
    private ITimer? _hideTimer, _tapTimer;
    private int _hideGeneration, _tapGeneration;
    private bool _tapPending;
    private long _tapTimestamp;
    private float _rateBeforeLongPress = 1;

    private TimeProvider Time => Clock ?? TimeProvider.System;

    /// <summary>Shows the controls; during playback they hide again after <see cref="ControlsHideDelay"/>.</summary>
    public void ShowControls()
    {
        SetControlsVisible(true);
        ScheduleAutoHide();
    }

    /// <summary>Hides the controls (and the settings pop-up anchored to them) until the picture is tapped.</summary>
    public void HideControls()
    {
        CancelAutoHide();
        if (IsSettingsOpen && !_settingsDrawer) CloseSettings();
        SetControlsVisible(false);
    }

    private void SetControlsVisible(bool visible)
    {
        if (AreControlsVisible == visible) return;
        AreControlsVisible = visible;
        ApplyChromeStyle();
        if (!visible && _tooltip.Style?.Display.ValueOrNull() != Display.None)
            ChangeStyle(_tooltip, s => s.Display = Display.None);
        Repaint();
    }

    /// <summary>
    /// Playing hides the controls after the delay; anything else — paused, ended, failed, loading —
    /// keeps them up, and pausing or stopping brings them back (not behind an open settings drawer,
    /// which took their place).
    /// </summary>
    private void OnPlaybackStateChanged()
    {
        if (Controller.State == VideoSessionState.Playing)
        {
            ScheduleAutoHide();
            return;
        }
        CancelAutoHide();
        if (Controller.State is VideoSessionState.Paused or VideoSessionState.Ended or VideoSessionState.Error
            && !(IsSettingsOpen && _settingsDrawer))
            SetControlsVisible(true);
    }

    private bool CanAutoHide => !_disposed && ControlsHideDelay > TimeSpan.Zero && AreControlsVisible
        && Controller.State == VideoSessionState.Playing && !IsSettingsOpen && !_dragging;

    private void ScheduleAutoHide()
    {
        _hideGeneration++;
        if (!CanAutoHide)
        {
            _hideTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }
        _hideTimer ??= CreateTimer(static state => ((MikoPlayer)state!).OnHideTimer());
        _hideTimer.Change(ControlsHideDelay, Timeout.InfiniteTimeSpan);
    }

    private void CancelAutoHide()
    {
        _hideGeneration++;
        _hideTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Timer thread.</summary>
    private void OnHideTimer()
    {
        int generation = Volatile.Read(ref _hideGeneration);
        PostToRenderThread(() =>
        {
            if (generation != _hideGeneration || !CanAutoHide) return;
            SetControlsVisible(false);
        });
    }

    /// <summary>
    /// A tap or click on the picture (or on the blank parts of the control bar). With an open settings
    /// panel it only closes the panel. A mouse click plays or pauses at once, as on desktop players.
    /// A touch tap waits <see cref="DoubleTapWindow"/>: alone it shows or hides the controls, followed
    /// by a second tap it plays or pauses.
    /// </summary>
    private void OnSurfaceClick(MouseEventArgs e)
    {
        if (IsSettingsOpen)
        {
            CloseSettings();
            Repaint();
            return;
        }
        if (e is not PointerEventArgs { PointerType: PointerType.Touch or PointerType.Pen })
        {
            Controller.TogglePlayback();
            ShowControls();
            Repaint();
            return;
        }

        long now = Time.GetTimestamp();
        if (_tapPending && Time.GetElapsedTime(_tapTimestamp, now) <= DoubleTapWindow)
        {
            CancelPendingTap();
            // Pausing brings the controls up (OnPlaybackStateChanged); resuming leaves them as they are.
            Controller.TogglePlayback();
            Repaint();
            return;
        }
        _tapPending = true;
        _tapTimestamp = now;
        _tapGeneration++;
        _tapTimer ??= CreateTimer(static state => ((MikoPlayer)state!).OnTapTimer());
        _tapTimer.Change(DoubleTapWindow, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Timer thread.</summary>
    private void OnTapTimer()
    {
        int generation = Volatile.Read(ref _tapGeneration);
        PostToRenderThread(() =>
        {
            if (_disposed || !_tapPending || generation != _tapGeneration) return;
            _tapPending = false;
            if (AreControlsVisible) HideControls();
            else ShowControls();
        });
    }

    private void CancelPendingTap()
    {
        _tapPending = false;
        _tapGeneration++;
        _tapTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Holding the picture plays at <see cref="LongPressPlaybackRate"/> until the finger (or mouse
    /// button) lifts, with a hint at the top and a light haptic tick as it starts. The engine raises
    /// <c>longpress</c> only for a press that has not moved, and then suppresses the click.
    /// </summary>
    private void OnSurfaceLongPress(PointerEventArgs e)
    {
        if (IsLongPressActive || IsSettingsOpen || !(LongPressPlaybackRate > 0)
            || Controller.State != VideoSessionState.Playing) return;

        CancelPendingTap();
        float rate = Math.Clamp(LongPressPlaybackRate, .25f, 4f);
        _rateBeforeLongPress = Controller.PlaybackRate;
        IsLongPressActive = true;
        Controller.SetPlaybackRate(rate);
        _hintText.TextContent = FormatRate(rate);
        ApplyHintStyle();
        PlatformService?.Haptics.Impact(HapticImpactStyle.Light);
        Repaint();
    }

    private void EndLongPress()
    {
        if (!IsLongPressActive) return;
        IsLongPressActive = false;
        Controller.SetPlaybackRate(_rateBeforeLongPress);
        ApplyHintStyle();
        Repaint();
    }

    private ITimer CreateTimer(TimerCallback callback)
    {
        // The timer lives as long as the player: it must not capture the ExecutionContext (and its
        // AsyncLocal values) of the event handler that first armed it.
        if (ExecutionContext.IsFlowSuppressed())
            return Time.CreateTimer(callback, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        using (ExecutionContext.SuppressFlow())
            return Time.CreateTimer(callback, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Runs <paramref name="action"/> on the render thread, at the start of the next frame — the
    /// dispatcher also wakes hosts that only draw on demand. Without an engine nothing renders the
    /// player, and the action runs at once.
    /// </summary>
    private void PostToRenderThread(Action action)
    {
        if (Engine is { } engine) engine.Dispatcher.Post(action);
        else action();
    }

    private void DisposeTimers()
    {
        _hideTimer?.Dispose();
        _hideTimer = null;
        _tapTimer?.Dispose();
        _tapTimer = null;
    }
}
