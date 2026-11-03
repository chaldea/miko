using Android.Graphics;
using Android.Opengl;
using Android.OS;
using Android.Views;
using Miko.Graphics;
using Miko.Platform.Video;

namespace Miko.Android.Camera;

/// <summary>
/// Camera preview frames: Camera2 writes into a <see cref="SurfaceTexture"/> whose OES texture is
/// wrapped as an <see cref="Image"/> on the render thread — zero copy, as for video
/// (<see cref="Video.AndroidVideoFrameSource"/>).
/// <para>
/// Unlike the video source, the SurfaceTexture is created <b>detached</b> (no GL context needed), so
/// the camera can start before the element is first painted; the render thread attaches it to a
/// texture of its own context on the first <see cref="AcquireCurrentFrame"/>, and again on a new
/// device after an EGL context loss.
/// </para>
/// <para>
/// The texture is wrapped with a top-left origin and the SurfaceTexture transform matrix is not
/// applied, so frames are the raw sensor buffer: not rotated by the sensor orientation and not
/// mirrored for front cameras. The session reports both (<c>Rotation</c>, <c>IsMirrored</c>) and the
/// engine applies them when it paints.
/// </para>
/// </summary>
internal sealed class AndroidCameraFrameSource : IVideoFrameSource, IDisposable
{
    /// <summary><c>GL_TEXTURE_EXTERNAL_OES</c>.</summary>
    private const uint GlTextureExternalOes = 0x8D65;

    /// <summary><c>GL_RGBA8</c>: an external texture samples as RGB.</summary>
    private const uint GlRgba8 = 0x8058;

    /// <summary>At most this many queued buffers are latched per frame (the latest one is shown).</summary>
    private const int MaxLatchPerFrame = 4;

    private readonly object _lock = new();
    private readonly SurfaceTexture _surfaceTexture;
    private readonly int _width;
    private readonly int _height;
    private GraphicsDevice? _attachedDevice;
    private int _textureId;
    private int _pendingFrames;
    private Image? _currentImage;
    private bool _disposed;

    /// <param name="width">Preview buffer width (sensor orientation).</param>
    /// <param name="height">Preview buffer height.</param>
    /// <param name="frameHandler">Thread the frame-available callback runs on.</param>
    /// <param name="frameAvailable">Called for every frame the camera queues.</param>
    public AndroidCameraFrameSource(int width, int height, Handler frameHandler, Action frameAvailable)
    {
        _width = width;
        _height = height;
        // Detached mode (API 26): no texture yet, attached on the render thread later.
        _surfaceTexture = new SurfaceTexture(false);
        _surfaceTexture.SetDefaultBufferSize(width, height);
        _surfaceTexture.SetOnFrameAvailableListener(new FrameListener(this, frameAvailable), frameHandler);
        Surface = new Surface(_surfaceTexture);
    }

    /// <summary>The camera's preview output.</summary>
    public Surface Surface { get; }

    /// <inheritdoc />
    public Image? AcquireCurrentFrame(GraphicsDevice device)
    {
        if (device.Backend != GraphicsBackend.OpenGl) return null;

        lock (_lock)
        {
            if (_disposed) return null;

            if (!ReferenceEquals(_attachedDevice, device))
            {
                if (!Attach(device)) return null;
                // Whatever is queued is latched into the new texture below.
                _pendingFrames = Math.Max(_pendingFrames, 1);
            }

            if (_pendingFrames == 0) return _currentImage;

            int latch = Math.Min(_pendingFrames, MaxLatchPerFrame);
            _pendingFrames = 0;
            try
            {
                // Each call latches the next queued buffer; latching all of them shows the newest and
                // gives the others back to the camera, which would otherwise run out of buffers.
                for (int i = 0; i < latch; i++)
                    _surfaceTexture.UpdateTexImage();
            }
            catch (Exception)
            {
                // Context lost mid-frame: keep the last image, try again next frame.
                return _currentImage;
            }

            // Rewrapped every frame: the backend caches image contents, reusing one image would
            // freeze the picture on its first frame.
            var image = device.WrapGlTexture(GlTextureExternalOes, (uint)_textureId, GlRgba8, _width, _height, ImagePixelFormat.Rgbx8888);
            if (image != null)
            {
                _currentImage?.Dispose();
                _currentImage = image;
            }
            return _currentImage;
        }
    }

    /// <inheritdoc />
    public void ReleaseCurrentFrame()
    {
        // The SurfaceTexture owns the buffer until the next UpdateTexImage.
    }

    /// <summary>Attaches the SurfaceTexture to a new texture of <paramref name="device"/>'s context. Render thread only.</summary>
    private bool Attach(GraphicsDevice device)
    {
        if (_attachedDevice != null)
        {
            // A different device means the old EGL context was lost (MikoGLRenderer recreates the
            // device then); its texture went with it. Detaching would delete it in the current
            // context, so only the SurfaceTexture's attachment is dropped, errors included.
            try { _surfaceTexture.DetachFromGLContext(); } catch (Exception) { }
            _currentImage?.Dispose();
            _currentImage = null;
            _attachedDevice = null;
            _textureId = 0;
        }

        var ids = new int[1];
        GLES20.GlGenTextures(1, ids, 0);
        if (ids[0] == 0) return false;
        try
        {
            // AttachToGLContext binds the texture to GL_TEXTURE_EXTERNAL_OES with linear filtering
            // and clamp-to-edge, which is all an external texture supports.
            _surfaceTexture.AttachToGLContext(ids[0]);
        }
        catch (Exception)
        {
            GLES20.GlDeleteTextures(1, ids, 0);
            return false;
        }
        _textureId = ids[0];
        _attachedDevice = device;
        return true;
    }

    private void OnFrameAvailable(Action frameAvailable)
    {
        lock (_lock)
        {
            if (_disposed) return;
            _pendingFrames++;
        }
        frameAvailable();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _currentImage?.Dispose();
            _currentImage = null;
            if (_attachedDevice != null)
            {
                // Deletes the texture; needs the attached context to be current, which it is when the
                // engine releases the session during a frame (the render thread). From another thread
                // the texture goes with the context instead.
                try { _surfaceTexture.DetachFromGLContext(); } catch (Exception) { }
                _attachedDevice = null;
            }
            Surface.Release();
            _surfaceTexture.Release();
        }
    }

    private sealed class FrameListener(AndroidCameraFrameSource owner, Action frameAvailable)
        : Java.Lang.Object, SurfaceTexture.IOnFrameAvailableListener
    {
        public void OnFrameAvailable(SurfaceTexture? surfaceTexture) => owner.OnFrameAvailable(frameAvailable);
    }
}
