using AVFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;

namespace Miko.iOS.Video;

/// <summary>
/// iOS 播放会话：<c>AVPlayer</c> 负责解复用、VideoToolbox 硬解、A/V 同步与网络缓冲，
/// <c>AVPlayerItemVideoOutput</c> 按显示时刻交出 <c>CVPixelBuffer</c>（IOSurface 支撑）。
///
/// <para>
/// 输出格式请求 NV12（<c>420v</c>，VideoToolbox 原生），由
/// <see cref="IosVideoFrameSource"/> 经纹理缓存零拷贝映射为 GL 纹理，
/// 再用 SkSL shader 在 GPU 上转 RGB。
/// </para>
///
/// <para>
/// 线程模型：取帧轮询在自有线程，但 <c>CVPixelBuffer</c> → GL 纹理的映射必须在
/// 渲染线程（GL 上下文所属线程）完成，故由帧源在 <c>AcquireCurrentFrame</c> 中处理。
/// </para>
///
/// <para><b>未经真机运行验证</b>：本机无 iOS 设备/模拟器，此实现仅通过编译验证。</para>
/// </summary>
internal sealed class IosVideoSession : IVideoSession
{
    private readonly VideoSourceDescriptor _source;
    private readonly ILogger _logger;
    private readonly IosVideoFrameSource _frameSource = new();

    private AVPlayer? _player;
    private AVPlayerItem? _playerItem;
    private AVPlayerItemVideoOutput? _videoOutput;

    private Thread? _pollThread;
    private volatile bool _stopRequested;
    private volatile VideoSessionState _state = VideoSessionState.Idle;

    private volatile bool _playRequested;
    private volatile bool _loop;
    private volatile bool _muted;
    private volatile float _volume = 1.0f;
    private volatile float _playbackRate = 1.0f;
    private long _seekRequestTicks = -1;
    private readonly object _controlLock = new();

    private TimeSpan _duration;
    private long _positionTicks;
    private int _videoWidth;
    private int _videoHeight;

    public IosVideoSession(VideoSourceDescriptor source, VideoSessionOptions options, ILogger? logger)
    {
        _source = source;
        _logger = logger ?? NullLogger.Instance;
        _loop = options.Loop;
        _playRequested = options.AutoPlay;
        _muted = options.Muted;
        _volume = Math.Clamp(options.InitialVolume, 0f, 1f);
    }

    public IVideoFrameSource FrameSource => _frameSource;
    public VideoSessionState State => _state;
    public TimeSpan Duration => _duration;
    public TimeSpan Position => TimeSpan.FromTicks(Interlocked.Read(ref _positionTicks));
    public int VideoWidth => _videoWidth;
    public int VideoHeight => _videoHeight;

    public event Action<VideoSessionEvent>? Event;

    public void Start()
    {
        _state = VideoSessionState.Loading;
        _pollThread = new Thread(PollLoop)
        {
            IsBackground = true,
            Name = "miko-video-avplayer",
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

    // AVPlayer 自带音频输出，这三项真实生效。只记录意图，由轮询线程应用 ——
    // _player 由该线程创建并在 Teardown 中释放，主线程直接改属性会与释放竞态。
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

            if (_playerItem != null && _playerItem.Duration.IsNumeric)
                _duration = TimeSpan.FromSeconds(_playerItem.Duration.Seconds);

            _state = _playRequested ? VideoSessionState.Playing : VideoSessionState.Ready;
            RunFramePolling();
        }
        catch (Exception ex)
        {
            _state = VideoSessionState.Error;
            _logger.LogError(ex, "AVPlayer playback failed for {Uri}", _source.Uri);
            Raise(new VideoSessionEvent.Error(ex.Message, ex));
        }
        finally
        {
            Teardown();
        }
    }

    private void BuildPlayer()
    {
        // 按 Miko 约定归一化相对路径（AppContext.BaseDirectory，而非进程 CWD）。
        NSUrl? url = CreateUrl(_source.ResolveForBackend());
        if (url == null)
            throw new InvalidOperationException($"Could not build NSUrl for '{_source.Uri}'.");

        _playerItem = AVPlayerItem.FromUrl(url)
            ?? throw new InvalidOperationException($"AVPlayerItem creation failed for '{_source.Uri}'.");

        // 请求 NV12（VideoToolbox 原生输出），避免系统插入多余的色彩转换。
        var attributes = new CVPixelBufferAttributes
        {
            PixelFormatType = CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange,
        };
        _videoOutput = new AVPlayerItemVideoOutput(attributes);
        _playerItem.AddOutput(_videoOutput);

        _player = new AVPlayer(_playerItem)
        {
            Muted = _muted,
            Volume = _volume,
        };
    }

    private static NSUrl? CreateUrl(string source)
    {
        // 网络源用绝对 URL；本地裸路径用 FromFilename（正确处理空格等字符）。
        if (source.Contains("://", StringComparison.Ordinal))
            return new NSUrl(source);

        // ResolveForBackend 已尽力归一化；仍为相对路径时回退到 app bundle
        // （iOS 上资源随 bundle 打包，BaseDirectory 与 bundle 路径可能不同）。
        string path = Path.IsPathRooted(source)
            ? source
            : Path.Combine(NSBundle.MainBundle.BundlePath, source);

        return NSUrl.FromFilename(path);
    }

    /// <summary>等待 item 就绪。网络源需缓冲，最多等 10 秒。</summary>
    private void WaitUntilReady()
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();

        while (!_stopRequested && deadline.Elapsed < TimeSpan.FromSeconds(10))
        {
            switch (_playerItem!.Status)
            {
                case AVPlayerItemStatus.ReadyToPlay:
                    return;
                case AVPlayerItemStatus.Failed:
                    throw new InvalidOperationException(
                        _playerItem.Error?.LocalizedDescription ?? "AVPlayerItem failed to load the media.");
            }

            Thread.Sleep(20);
        }

        if (!_stopRequested)
            throw new TimeoutException($"AVPlayerItem did not become ready within 10s for '{_source.Uri}'.");
    }

    /// <summary>
    /// 轮询取帧。AVPlayer 自己按媒体时钟推进，这里只需以略高于帧率的频率询问是否有新帧。
    /// </summary>
    private void RunFramePolling()
    {
        bool loadedRaised = false;
        bool playing = false;

        while (!_stopRequested)
        {
            HandlePendingSeek();

            if (!_playRequested)
            {
                if (playing)
                {
                    _player!.Pause();
                    playing = false;
                }
                if (_state == VideoSessionState.Playing) _state = VideoSessionState.Paused;
                Thread.Sleep(15);
                continue;
            }

            if (!playing)
            {
                _player!.Play();
                playing = true;
                if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing;
            }

            // 在本线程应用音量/静音/速率的待定意图（控制方法只写标志，见上）。
            ApplyPendingAudioSettings();

            CMTime itemTime = _player!.CurrentTime;

            if (_videoOutput!.HasNewPixelBufferForItemTime(itemTime))
            {
                // 绑定要求 ref 传出显示时刻；本实现以 itemTime 为准，不使用该值。
                CMTime displayTime = default;
                CVPixelBuffer? pixelBuffer = _videoOutput.CopyPixelBuffer(itemTime, ref displayTime);

                if (pixelBuffer != null)
                {
                    var pts = TimeSpan.FromSeconds(itemTime.IsNumeric ? itemTime.Seconds : 0);

                    int width = (int)pixelBuffer.Width;
                    int height = (int)pixelBuffer.Height;

                    if (!loadedRaised && width > 0 && height > 0)
                    {
                        loadedRaised = true;
                        _videoWidth = width;
                        _videoHeight = height;
                        Raise(new VideoSessionEvent.Loaded(width, height, _duration));
                    }

                    // 交给帧源：像素缓冲的映射与释放由其在渲染线程完成。
                    _frameSource.PushPixelBuffer(pixelBuffer, pts);

                    Interlocked.Exchange(ref _positionTicks, pts.Ticks);
                    Raise(new VideoSessionEvent.FrameAvailable(pts));
                }
            }

            if (CheckEnded(itemTime))
                continue;

            Thread.Sleep(8);
        }
    }

    /// <summary>
    /// 把控制方法记录的音频/速率意图写入播放器。只在轮询线程调用，
    /// 仅在值变化时赋值以避免每轮都碰原生属性。
    /// </summary>
    private void ApplyPendingAudioSettings()
    {
        if (_player == null) return;

        if (Math.Abs(_player.Volume - _volume) > 0.001f)
            _player.Volume = _volume;

        if (_player.Muted != _muted)
            _player.Muted = _muted;

        // Rate 兼作播放/暂停开关，只在播放中且与目标不一致时调整。
        if (_playRequested && Math.Abs(_player.Rate - _playbackRate) > 0.001f)
            _player.Rate = _playbackRate;
    }

    private void HandlePendingSeek()
    {
        long seekTicks;
        lock (_controlLock) { seekTicks = _seekRequestTicks; _seekRequestTicks = -1; }
        if (seekTicks < 0) return;

        _player!.Seek(CMTime.FromSeconds(TimeSpan.FromTicks(seekTicks).TotalSeconds, 600));
        Interlocked.Exchange(ref _positionTicks, seekTicks);
        if (_state == VideoSessionState.Ended) _state = VideoSessionState.Playing;
    }

    /// <summary>返回 true 表示本轮已处理（loop 回到起点或停在结尾）。</summary>
    private bool CheckEnded(CMTime itemTime)
    {
        if (_duration <= TimeSpan.Zero || !itemTime.IsNumeric) return false;
        if (itemTime.Seconds < _duration.TotalSeconds - 0.05) return false;

        if (_loop)
        {
            _player!.Seek(CMTime.Zero);
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

    private void Teardown()
    {
        _player?.Pause();

        if (_playerItem != null && _videoOutput != null)
            _playerItem.RemoveOutput(_videoOutput);

        _videoOutput?.Dispose();
        _videoOutput = null;

        _playerItem?.Dispose();
        _playerItem = null;

        _player?.Dispose();
        _player = null;
    }

    private void Raise(VideoSessionEvent evt) => Event?.Invoke(evt);

    public void Dispose()
    {
        _stopRequested = true;
        _pollThread?.Join(1000);
        _frameSource.Dispose();
    }
}
