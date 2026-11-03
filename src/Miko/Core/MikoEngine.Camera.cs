using Microsoft.Extensions.Logging;
using Miko.Core.DomElements;
using Miko.Platform.Camera;

namespace Miko.Core;

public partial class MikoEngine
{
    /// <summary>
    /// Camera backend, injected by DI; the core default <see cref="NullCameraBackend"/> opens no sessions
    /// and a <c>&lt;camera&gt;</c> shows only its background. May be replaced at run time, like
    /// <see cref="VideoBackend"/>.
    /// </summary>
    public ICameraBackend? CameraBackend { get; set; }

    private bool HasCameraBackend => CameraBackend is not null and not NullCameraBackend;

    // Open sessions by element, as for video. The collections are reused: SyncCameraSessions runs
    // every frame once a backend is registered.
    private readonly Dictionary<CameraElement, ICameraSession> _cameraSessions = new();
    private readonly HashSet<CameraElement> _presentCameras = new();
    private readonly List<CameraElement> _releasedCameras = new();

    // Sessions paused because the app went to the background (not those paused for other reasons).
    private readonly HashSet<ICameraSession> _suspendedCameraSessions = new();
    private bool _cameraSuspended;

    /// <summary>
    /// Matches camera sessions to the tree after every layout, like <see cref="SyncVideoSessions"/>:
    /// releases the sessions of elements that left the tree, were deactivated, ask for another camera or
    /// want a restart; opens sessions for the rest; pushes torch and zoom to running sessions.
    /// </summary>
    private void SyncCameraSessions(Element root)
    {
        if (_cameraSessions.Count == 0 && !HasCameraBackend) return;

        _presentCameras.Clear();
        CollectCameraElements(root, _presentCameras);
        // The leaving page of a navigation transition is still painted (ISSUE-108).
        if (_navLeavingRoot != null)
            CollectCameraElements(_navLeavingRoot, _presentCameras);

        if (_cameraSessions.Count > 0)
        {
            foreach (var (camera, session) in _cameraSessions)
            {
                if (_presentCameras.Contains(camera) && camera.Active && !camera.RestartRequested
                    && camera.SessionOptions is { } opened && camera.Matches(opened))
                    continue;
                ReleaseCameraSession(camera, session);
                _releasedCameras.Add(camera);
            }
            foreach (var camera in _releasedCameras)
                _cameraSessions.Remove(camera);
            _releasedCameras.Clear();
        }

        if (HasCameraBackend)
        {
            foreach (var camera in _presentCameras)
            {
                // A failed attempt is not retried every frame: only when the element asks for another
                // camera or calls Restart().
                if (camera.Session == null && camera.Active
                    && (camera.RestartRequested || camera.SessionOptions is not { } last || !camera.Matches(last)))
                    CreateCameraSession(camera);
                if (camera.Session is { } session)
                    ApplyCameraControls(camera, session);
            }
        }
        _presentCameras.Clear();
    }

    private void CreateCameraSession(CameraElement camera)
    {
        var options = camera.CreateOptions();
        camera.SessionOptions = options;
        camera.RestartRequested = false;
        try
        {
            var session = CameraBackend!.CreateSession(options);
            camera.Session = session;
            _cameraSessions[camera] = session;

            camera.SessionHandler = evt =>
            {
                // Frames only invalidate paint; no UI closure per frame. The no-op post wakes hosts
                // that draw on demand (Android), which only render when something is posted.
                if (evt is CameraSessionEvent.FrameAvailable)
                {
                    if (!ReferenceEquals(camera.Session, session)) return;
                    PostInvalidate(camera);
                    _dispatcher.Post(s_wakeHost);
                    return;
                }
                _dispatcher.Post(() =>
                {
                    if (!ReferenceEquals(camera.Session, session)) return;
                    OnCameraSessionEvent(camera, session, evt);
                    camera.NotifySessionEvent(evt);
                });
            };
            session.Event += camera.SessionHandler;
            if (camera.Analyzer is { } analyzer)
                session.SetAnalyzer(analyzer);
            // Opened while the app is in the background: start paused, resume with the others.
            if (_cameraSuspended)
            {
                session.Pause();
                _suspendedCameraSessions.Add(session);
            }
            camera.NotifySessionChanged();
            _logger.LogDebug("Camera session created ({Facing}, {DeviceId})", options.Facing, options.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open the camera ({Facing}, {DeviceId})", options.Facing, options.DeviceId);
            camera.NotifySessionEvent(new CameraSessionEvent.Error(CameraErrorKind.Failed, ex.Message, ex));
        }
    }

    private void ReleaseCameraSession(CameraElement camera, ICameraSession session)
    {
        if (camera.SessionHandler != null) session.Event -= camera.SessionHandler;
        camera.SessionHandler = null;
        camera.Session = null;
        camera.SessionOptions = null;
        camera.IntrinsicWidth = null;
        camera.IntrinsicHeight = null;
        _suspendedCameraSessions.Remove(session);
        camera.NotifySessionChanged();
        try
        {
            session.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Camera session failed to close");
        }
        PostInvalidate(camera);
    }

    private void OnCameraSessionEvent(CameraElement camera, ICameraSession session, CameraSessionEvent evt)
    {
        switch (evt)
        {
            case CameraSessionEvent.Started:
                UpdateCameraIntrinsicSize(camera, session);
                ApplyCameraControls(camera, session);
                break;
            case CameraSessionEvent.Error error:
                _logger.LogWarning(error.Cause, "Camera error ({Kind}): {Message}", error.Kind, error.Message);
                break;
        }
        PostInvalidate(camera);
    }

    /// <summary>Pushes the element's torch and zoom to a running session (a no-op when they already match).</summary>
    private void ApplyCameraControls(CameraElement camera, ICameraSession session)
    {
        if (session.State != CameraSessionState.Running) return;
        if (session.HasTorch && session.IsTorchOn != camera.Torch)
            session.SetTorch(camera.Torch);
        if (session.MaxZoom > session.MinZoom)
        {
            float zoom = Math.Clamp(float.IsFinite(camera.Zoom) ? camera.Zoom : 1, session.MinZoom, session.MaxZoom);
            if (Math.Abs(session.Zoom - zoom) > 0.001f)
                session.SetZoom(zoom);
        }
        UpdateCameraIntrinsicSize(camera, session);
    }

    /// <summary>
    /// The intrinsic size is the upright frame size; it follows the display orientation. A layout input
    /// (for auto-sized elements), so a change relayouts without restyling, as video's <c>Loaded</c>.
    /// </summary>
    private void UpdateCameraIntrinsicSize(CameraElement camera, ICameraSession session)
    {
        if (session.FrameWidth <= 0 || session.FrameHeight <= 0) return;
        bool sideways = session.Rotation % 180 != 0;
        int width = sideways ? session.FrameHeight : session.FrameWidth;
        int height = sideways ? session.FrameWidth : session.FrameHeight;
        if (camera.IntrinsicWidth == width && camera.IntrinsicHeight == height) return;
        camera.IntrinsicWidth = width;
        camera.IntrinsicHeight = height;
        _mutations.BumpContent();
        PostInvalidate(camera);
    }

    private static void CollectCameraElements(Element element, HashSet<CameraElement> sink)
    {
        if (element is CameraElement camera)
            sink.Add(camera);
        foreach (var child in element.Children)
            CollectCameraElements(child, sink);
    }

    /// <summary>Closes every camera session. Platform hosts call it (through <see cref="DisposeMediaSessions"/>) on shutdown.</summary>
    public void DisposeCameraSessions()
    {
        foreach (var (camera, session) in _cameraSessions)
            ReleaseCameraSession(camera, session);
        _cameraSessions.Clear();
        _suspendedCameraSessions.Clear();
    }

    /// <summary>Closes every video and camera session. Platform hosts call it on shutdown.</summary>
    public void DisposeMediaSessions()
    {
        DisposeVideoSessions();
        DisposeCameraSessions();
    }

    /// <summary>
    /// The app went to the background: release the cameras (other apps may want them, and the OS
    /// stops background capture anyway) and remember which ones to reopen.
    /// </summary>
    private void SuspendCameras()
    {
        _cameraSuspended = true;
        foreach (var session in _cameraSessions.Values)
        {
            if (session.State is not (CameraSessionState.Running or CameraSessionState.Starting)) continue;
            session.Pause();
            _suspendedCameraSessions.Add(session);
        }
    }

    private void ResumeCameras()
    {
        _cameraSuspended = false;
        if (_suspendedCameraSessions.Count == 0) return;
        foreach (var session in _cameraSessions.Values)
        {
            if (_suspendedCameraSessions.Contains(session))
                session.Resume();
        }
        _suspendedCameraSessions.Clear();
    }
}
