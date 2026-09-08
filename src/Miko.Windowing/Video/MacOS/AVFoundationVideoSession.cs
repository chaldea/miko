using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using static Miko.Windowing.Video.MacOS.AVFoundationInterop;

namespace Miko.Windowing.Video.MacOS;

/// <summary>
/// macOS AVFoundation 播放会话。用 <c>AVPlayer</c> + <c>AVPlayerItemVideoOutput</c>：
/// 解码由 VideoToolbox 硬件完成，A/V 同步、HLS、网络缓冲全部由系统负责，
/// 因此本类只需按渲染节奏取出 <c>CVPixelBuffer</c>。
///
/// <para>
/// 因桌面项目为纯 net10.0（无 macOS 托管绑定），所有 AVFoundation 调用均经
/// Objective-C runtime 手工发消息（见 <see cref="AVFoundationInterop"/>）。
/// </para>
///
/// <para>
/// 输出格式请求 NV12（<c>420v</c>，VideoToolbox 原生），交由 SkSL shader 在 GPU 上转 RGB。
/// </para>
///
/// <para><b>未经真机运行验证</b>：本机为 Windows，此实现仅通过编译验证。</para>
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class AVFoundationVideoSession : IVideoSession
{
    private readonly VideoSourceDescriptor _source;
    private readonly ILogger _logger;
    private readonly AVFoundationFrameSource _frameSource = new();

    // Objective-C 对象（retain 计数由本类管理）。
    private IntPtr _player;
    private IntPtr _playerItem;
    private IntPtr _videoOutput;

    private Thread? _pollThread;
    private volatile bool _stopRequested;
    private volatile VideoSessionState _state = VideoSessionState.Idle;

    private volatile bool _playRequested;
    private volatile bool _loop;
    private volatile bool _buffering;
    private volatile bool _muted;
    private volatile float _volume = 1.0f;
    private volatile float _playbackRate = 1.0f;
    private long _seekRequestTicks = -1;
    private readonly object _controlLock = new();

    private TimeSpan _duration;
    private long _positionTicks;
    private int _videoWidth;
    private int _videoHeight;

    public AVFoundationVideoSession(VideoSourceDescriptor source, VideoSessionOptions options, ILogger? logger)
    {
        _source = source;
        _logger = logger ?? NullLogger.Instance;
        _loop = options.Loop;
        _playRequested = options.AutoPlay;
        _muted = options.Muted;
        _volume = options.InitialVolume;
    }

    public IVideoFrameSource FrameSource => _frameSource;
    public VideoSessionState State => _state;
    public TimeSpan Duration => _duration;
    public TimeSpan Position => TimeSpan.FromTicks(Interlocked.Read(ref _positionTicks));
    public int VideoWidth => _videoWidth;
    public int VideoHeight => _videoHeight;
    public bool IsBuffering => _state == VideoSessionState.Loading || _buffering;

    public event Action<VideoSessionEvent>? Event;

    public void Start()
    {
        _state = VideoSessionState.Loading;
        _pollThread = new Thread(PollLoop)
        {
            IsBackground = true,
            Name = "miko-video-avf",
        };
        _pollThread.Start();
    }

    // ---- 控制 --------------------------------------------------------------

    public void Play()
    {
        _playRequested = true;
        if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing;
    }

    public void Pause()
    {
        _playRequested = false;
        if (_state == VideoSessionState.Playing) _state = VideoSessionState.Paused;
    }

    public void Seek(TimeSpan position)
    {
        lock (_controlLock) { _seekRequestTicks = Math.Max(0, position.Ticks); }
    }

    public void SetLoop(bool loop) => _loop = loop;

    // AVPlayer 自带音频输出，这三项可真实生效（由轮询线程应用，避免跨线程发消息）。
    public void SetVolume(float volume) => _volume = Math.Clamp(volume, 0f, 1f);
    public void SetMuted(bool muted) => _muted = muted;
    public void SetPlaybackRate(float rate) => _playbackRate = rate <= 0 ? 1.0f : rate;

    // ---- 播放循环 ----------------------------------------------------------

    private void PollLoop()
    {
        try
        {
            BuildPlayer();
            WaitUntilReady();
            ReadDuration();

            _state = _playRequested ? VideoSessionState.Playing : VideoSessionState.Ready;
            RunFramePolling();
        }
        catch (Exception ex)
        {
            _state = VideoSessionState.Error;
            _logger.LogError(ex, "AVFoundation playback failed for {Uri}", _source.Uri);
            Raise(new VideoSessionEvent.Error(ex.Message, ex));
        }
        finally
        {
            Teardown();
        }
    }

    /// <summary>
    /// 构造 AVPlayer 链路：URL → AVPlayerItem → AVPlayer，并挂上请求 NV12 的
    /// AVPlayerItemVideoOutput。
    /// </summary>
    private void BuildPlayer()
    {
        // 按 Miko 约定归一化相对路径（AppContext.BaseDirectory，而非进程 CWD）。
        IntPtr nsUrl = CreateUrl(_source.ResolveForBackend());
        if (nsUrl == IntPtr.Zero)
            throw new InvalidOperationException($"Could not build NSURL for '{_source.Uri}'.");

        IntPtr itemClass = GetClass("AVPlayerItem");
        _playerItem = SendPtr(itemClass, GetSelector("playerItemWithURL:"), nsUrl);
        if (_playerItem == IntPtr.Zero)
            throw new InvalidOperationException("AVPlayerItem creation failed.");
        _playerItem = CFRetain(_playerItem);

        _videoOutput = CreateVideoOutput();
        SendVoid(_playerItem, GetSelector("addOutput:"), _videoOutput);

        IntPtr playerClass = GetClass("AVPlayer");
        _player = SendPtr(playerClass, GetSelector("playerWithPlayerItem:"), _playerItem);
        if (_player == IntPtr.Zero)
            throw new InvalidOperationException("AVPlayer creation failed.");
        _player = CFRetain(_player);

        ApplyAudioSettings();
    }

    private static IntPtr CreateUrl(string source)
    {
        IntPtr urlClass = GetClass("NSURL");

        // 网络源用 URLWithString:，本地路径用 fileURLWithPath:（后者会正确处理空格等字符）。
        if (source.Contains("://", StringComparison.Ordinal))
        {
            IntPtr urlString = CreateNSString(source);
            return SendPtr(urlClass, GetSelector("URLWithString:"), urlString);
        }

        // 路径已由 ResolveForBackend 归一化。
        IntPtr path = CreateNSString(source);
        return SendPtr(urlClass, GetSelector("fileURLWithPath:"), path);
    }

    /// <summary>
    /// 创建 <c>AVPlayerItemVideoOutput</c>，并通过像素属性字典请求 NV12。
    /// 键取 CoreVideo 导出的 <c>kCVPixelBufferPixelFormatTypeKey</c>（真实 CFString，
    /// 不是字面量），值为 NSNumber 包装的 FourCC。
    /// </summary>
    private static IntPtr CreateVideoOutput()
    {
        IntPtr outputClass = GetClass("AVPlayerItemVideoOutput");
        IntPtr formatKey = GetPixelFormatTypeKey();

        // 拿不到键符号时退到无属性初始化：系统给默认格式（通常仍是 NV12），
        // 帧读取端按 planeCount 自适应，因此不会因此失败。
        if (formatKey == IntPtr.Zero)
        {
            return SendPtr(
                SendPtr(outputClass, GetSelector("alloc")),
                GetSelector("initWithPixelBufferAttributes:"),
                IntPtr.Zero);
        }

        IntPtr numberClass = GetClass("NSNumber");
        IntPtr formatNumber = SendPtr(
            numberClass, GetSelector("numberWithUnsignedInt:"), (IntPtr)PixelFormatNv12VideoRange);

        IntPtr dictClass = GetClass("NSDictionary");
        IntPtr attributes = SendPtr(
            dictClass, GetSelector("dictionaryWithObject:forKey:"), formatNumber, formatKey);

        return SendPtr(
            SendPtr(outputClass, GetSelector("alloc")),
            GetSelector("initWithPixelBufferAttributes:"),
            attributes);
    }

    private void ApplyAudioSettings()
    {
        SendVoid(_player, GetSelector("setVolume:"), _volume);
        SendVoid(_player, GetSelector("setMuted:"), _muted);
    }

    /// <summary>
    /// 等待 <c>AVPlayerItem</c> 就绪（status == ReadyToPlay）。网络源需要缓冲，最多等 10 秒。
    /// </summary>
    private void WaitUntilReady()
    {
        IntPtr statusSelector = GetSelector("status");
        var deadline = Stopwatch.StartNew();

        while (!_stopRequested && deadline.Elapsed < TimeSpan.FromSeconds(10))
        {
            nint status = SendNInt(_playerItem, statusSelector);
            if (status == StatusReadyToPlay) return;
            if (status == StatusFailed)
                throw new InvalidOperationException("AVPlayerItem failed to load the media.");

            Thread.Sleep(20);
        }

        if (!_stopRequested)
            throw new TimeoutException($"AVPlayerItem did not become ready within 10s for '{_source.Uri}'.");
    }

    private void ReadDuration()
    {
        var duration = SendCMTime(_playerItem, GetSelector("duration"));
        if (duration.IsValid && duration.Seconds > 0)
            _duration = TimeSpan.FromSeconds(duration.Seconds);
    }

    /// <summary>
    /// 轮询取帧。<c>AVPlayer</c> 自己按媒体时钟推进，因此这里只需以略高于帧率的频率
    /// 询问「当前时刻是否有新帧」，无需自行做 PTS 节流。
    /// </summary>
    private void RunFramePolling()
    {
        IntPtr hasNewFrameSelector = GetSelector("hasNewPixelBufferForItemTime:");
        IntPtr copyPixelBufferSelector = GetSelector("copyPixelBufferForItemTime:itemTimeForDisplay:");
        IntPtr currentTimeSelector = GetSelector("currentTime");
        IntPtr playSelector = GetSelector("play");
        IntPtr pauseSelector = GetSelector("pause");

        bool loadedRaised = false;
        bool playing = false;

        while (!_stopRequested)
        {
            HandlePendingSeek();
            bool buffering = _playRequested && SendBool(_playerItem, GetSelector("isPlaybackBufferEmpty"));
            if (_buffering != buffering)
            {
                _buffering = buffering;
                Raise(new VideoSessionEvent.Buffering(buffering));
            }

            if (!_playRequested)
            {
                if (playing)
                {
                    SendVoid(_player, pauseSelector);
                    playing = false;
                }
                if (_state == VideoSessionState.Playing) _state = VideoSessionState.Paused;
                Thread.Sleep(15);
                continue;
            }

            if (!playing)
            {
                SendVoid(_player, playSelector);
                SendVoid(_player, GetSelector("setRate:"), _playbackRate);
                playing = true;
                if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing;
            }

            // 在本线程应用音量/静音的待定意图。控制方法只写标志（不能跨线程发消息），
            // 因此必须每轮同步一次，否则构造后的 SetVolume/SetMuted 永远不生效。
            ApplyAudioSettings();

            var itemTime = SendCMTime(_player, currentTimeSelector);

            if (SendBool(_videoOutput, hasNewFrameSelector, itemTime))
            {
                IntPtr pixelBuffer = SendPtrWithCMTime(
                    _videoOutput, copyPixelBufferSelector, itemTime, IntPtr.Zero);

                if (pixelBuffer != IntPtr.Zero)
                {
                    try
                    {
                        var frame = ReadPixelBuffer(pixelBuffer, TimeSpan.FromSeconds(itemTime.Seconds));
                        if (frame != null)
                        {
                            if (!loadedRaised)
                            {
                                loadedRaised = true;
                                _videoWidth = frame.Width;
                                _videoHeight = frame.Height;
                                Raise(new VideoSessionEvent.Loaded(frame.Width, frame.Height, _duration));
                            }

                            _frameSource.PushFrame(frame);
                            Interlocked.Exchange(ref _positionTicks, frame.Pts.Ticks);
                            Raise(new VideoSessionEvent.FrameAvailable(frame.Pts));
                        }
                    }
                    finally
                    {
                        // copyPixelBuffer 返回 +1 引用，必须释放。
                        CFRelease(pixelBuffer);
                    }
                }
            }

            if (CheckEnded(itemTime))
                continue;

            // 略快于 60fps 的轮询：既不漏帧也不过度占用 CPU。
            Thread.Sleep(8);
        }
    }

    private void HandlePendingSeek()
    {
        long seekTicks;
        lock (_controlLock) { seekTicks = _seekRequestTicks; _seekRequestTicks = -1; }
        if (seekTicks < 0) return;

        var target = CMTime.FromSeconds(TimeSpan.FromTicks(seekTicks).TotalSeconds);
        SendVoidWithCMTime(_player, GetSelector("seekToTime:"), target);
        Interlocked.Exchange(ref _positionTicks, seekTicks);

        if (_state == VideoSessionState.Ended) _state = VideoSessionState.Playing;
    }

    /// <summary>到达结尾时按 loop 回到起点或进入 Ended。返回 true 表示本轮已处理。</summary>
    private bool CheckEnded(CMTime itemTime)
    {
        if (_duration <= TimeSpan.Zero) return false;
        if (itemTime.Seconds < _duration.TotalSeconds - 0.05) return false;

        if (_loop)
        {
            SendVoidWithCMTime(_player, GetSelector("seekToTime:"), CMTime.FromSeconds(0));
            return true;
        }

        if (_state != VideoSessionState.Ended)
        {
            _state = VideoSessionState.Ended;
            Raise(new VideoSessionEvent.Ended());
        }

        Thread.Sleep(15);
        return true;
    }

    /// <summary>
    /// 从 <c>CVPixelBuffer</c> 拷出像素。NV12 为双平面（Y + 交错 UV），
    /// 各平面的 stride 由 <c>GetBytesPerRowOfPlane</c> 给出（通常有对齐填充）。
    /// </summary>
    private static VideoFrameBuffer? ReadPixelBuffer(IntPtr pixelBuffer, TimeSpan pts)
    {
        int width = (int)CVPixelBufferGetWidth(pixelBuffer);
        int height = (int)CVPixelBufferGetHeight(pixelBuffer);
        if (width <= 0 || height <= 0) return null;

        // 只读锁定：flags = kCVPixelBufferLock_ReadOnly (1)。
        const ulong readOnly = 1;
        if (CVPixelBufferLockBaseAddress(pixelBuffer, readOnly) != 0)
            return null;

        try
        {
            nuint planeCount = CVPixelBufferGetPlaneCount(pixelBuffer);

            // 双平面 NV12。
            if (planeCount >= 2)
            {
                var yPlane = CopyPlane(pixelBuffer, 0, out int yStride);
                var uvPlane = CopyPlane(pixelBuffer, 1, out int uvStride);
                if (yPlane == null || uvPlane == null) return null;

                return VideoFrameBuffer.FromCpuPlanes(
                    width, height, VideoPixelFormat.Nv12, pts,
                    [yPlane, uvPlane], [yStride, uvStride]);
            }

            // 单平面：BGRA 回退（planeCount == 0 表示非平面格式）。
            IntPtr baseAddress = CVPixelBufferGetBaseAddressOfPlane(pixelBuffer, 0);
            if (baseAddress == IntPtr.Zero) return null;

            int stride = (int)CVPixelBufferGetBytesPerRowOfPlane(pixelBuffer, 0);
            var pixels = new byte[stride * height];
            Marshal.Copy(baseAddress, pixels, 0, pixels.Length);

            return VideoFrameBuffer.FromCpuPlanes(
                width, height, VideoPixelFormat.Bgra8888, pts, [pixels], [stride]);
        }
        finally
        {
            CVPixelBufferUnlockBaseAddress(pixelBuffer, readOnly);
        }
    }

    private static byte[]? CopyPlane(IntPtr pixelBuffer, nuint planeIndex, out int stride)
    {
        IntPtr baseAddress = CVPixelBufferGetBaseAddressOfPlane(pixelBuffer, planeIndex);
        stride = (int)CVPixelBufferGetBytesPerRowOfPlane(pixelBuffer, planeIndex);
        int planeHeight = (int)CVPixelBufferGetHeightOfPlane(pixelBuffer, planeIndex);

        if (baseAddress == IntPtr.Zero || stride <= 0 || planeHeight <= 0)
            return null;

        var plane = new byte[stride * planeHeight];
        Marshal.Copy(baseAddress, plane, 0, plane.Length);
        return plane;
    }

    private void Teardown()
    {
        if (_player != IntPtr.Zero)
        {
            SendVoid(_player, GetSelector("pause"));
            CFRelease(_player);
            _player = IntPtr.Zero;
        }

        if (_playerItem != IntPtr.Zero)
        {
            if (_videoOutput != IntPtr.Zero)
                SendVoid(_playerItem, GetSelector("removeOutput:"), _videoOutput);

            CFRelease(_playerItem);
            _playerItem = IntPtr.Zero;
        }

        if (_videoOutput != IntPtr.Zero)
        {
            SendVoid(_videoOutput, GetSelector("release"));
            _videoOutput = IntPtr.Zero;
        }
    }

    private void Raise(VideoSessionEvent evt) => Event?.Invoke(evt);

    public void Dispose()
    {
        _stopRequested = true;
        _pollThread?.Join(1000);
        _frameSource.Dispose();
    }
}
