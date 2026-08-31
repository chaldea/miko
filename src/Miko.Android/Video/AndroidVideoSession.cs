using Android.Graphics;
using Android.Media;
using Java.Nio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;

namespace Miko.Android.Video;

/// <summary>
/// Android 播放会话：<c>MediaExtractor</c> 读取视频轨的压缩样本，喂给 <c>MediaCodec</c>
/// 硬件解码器，解码输出直接渲染到 <c>SurfaceTexture</c>（OES 外部纹理）。
///
/// <para>
/// **零拷贝**：<c>MediaCodec.ReleaseOutputBuffer(index, render: true)</c> 让解码器把帧
/// 写进 Surface，CPU 完全不接触 YUV 数据。渲染线程调用
/// <c>SurfaceTexture.UpdateTexImage()</c> 后，该 OES 纹理即为最新帧，
/// 经 <c>GRGlTextureInfo(GL_TEXTURE_EXTERNAL_OES, ...)</c> 包成 <c>SKImage</c>。
/// </para>
///
/// <para>
/// 线程模型：解复用与送解码在自有线程；<c>SurfaceTexture</c> 的创建与
/// <c>UpdateTexImage</c> 必须在持有 GL 上下文的渲染线程执行，
/// 因此纹理相关操作全部委托给 <see cref="AndroidVideoFrameSource"/> 在
/// <c>AcquireCurrentFrame</c> 时完成。
/// </para>
///
/// <para><b>未经真机运行验证</b>：本机无 Android 设备/模拟器，此实现仅通过编译验证。</para>
/// </summary>
internal sealed class AndroidVideoSession : IVideoSession
{
    private const int DequeueTimeoutUs = 10_000;    // 10ms：足够短以响应停止请求

    private readonly VideoSourceDescriptor _source;
    private readonly VideoSessionOptions _options;
    private readonly ILogger _logger;
    private readonly AndroidVideoFrameSource _frameSource;

    private Thread? _decodeThread;
    private volatile bool _stopRequested;
    private volatile VideoSessionState _state = VideoSessionState.Idle;

    private volatile bool _playRequested;
    private volatile bool _loop;
    private long _seekRequestTicks = -1;
    private readonly object _controlLock = new();

    private TimeSpan _duration;
    private long _positionTicks;
    private int _videoWidth;
    private int _videoHeight;

    public AndroidVideoSession(VideoSourceDescriptor source, VideoSessionOptions options, ILogger? logger)
    {
        _source = source;
        _options = options;
        _logger = logger ?? NullLogger.Instance;
        _loop = options.Loop;
        _playRequested = options.AutoPlay;
        _frameSource = new AndroidVideoFrameSource();
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
        _decodeThread = new Thread(DecodeLoop)
        {
            IsBackground = true,
            Name = "miko-video-mediacodec",
        };
        _decodeThread.Start();
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

    // 音轨未接入（只解视频轨），与其它后端一致保持 no-op。
    public void SetVolume(float volume) { }
    public void SetMuted(bool muted) { }
    public void SetPlaybackRate(float rate) { }

    // ---- 解码循环 ----------------------------------------------------------

    private void DecodeLoop()
    {
        MediaExtractor? extractor = null;
        MediaCodec? codec = null;

        try
        {
            extractor = new MediaExtractor();
            // 按 Miko 约定归一化相对路径（AppContext.BaseDirectory，而非进程 CWD）。
            SetDataSource(extractor, _source.ResolveForBackend());

            int trackIndex = SelectVideoTrack(extractor, out MediaFormat format, out string mimeType);
            if (trackIndex < 0)
                throw new NotSupportedException($"No video track found in '{_source.Uri}'.");

            extractor.SelectTrack(trackIndex);

            _videoWidth = format.GetInteger(MediaFormat.KeyWidth);
            _videoHeight = format.GetInteger(MediaFormat.KeyHeight);
            if (format.ContainsKey(MediaFormat.KeyDuration))
                _duration = TimeSpan.FromTicks(format.GetLong(MediaFormat.KeyDuration) * 10); // µs → 100ns ticks

            // Surface 必须在渲染线程创建（需 GL 上下文）；这里等待其就绪。
            var surface = _frameSource.WaitForSurface(TimeSpan.FromSeconds(5));
            if (surface == null)
                throw new TimeoutException(
                    "Video output surface was not created; the render thread never presented a frame.");

            codec = MediaCodec.CreateDecoderByType(mimeType)
                ?? throw new NotSupportedException($"No decoder available for '{mimeType}'.");

            // 传入 Surface 即启用「解码直出 GPU」：输出缓冲不再回到 CPU。
            codec.Configure(format, surface, null, MediaCodecConfigFlags.None);
            codec.Start();

            _state = _playRequested ? VideoSessionState.Playing : VideoSessionState.Ready;
            Raise(new VideoSessionEvent.Loaded(_videoWidth, _videoHeight, _duration));
            _logger.LogInformation(
                "MediaCodec session ready: {Width}x{Height}, {Duration}, mime={Mime}",
                _videoWidth, _videoHeight, _duration, mimeType);

            RunDecodeLoop(extractor, codec);
        }
        catch (Exception ex)
        {
            _state = VideoSessionState.Error;
            _logger.LogError(ex, "MediaCodec decode failed for {Uri}", _source.Uri);
            Raise(new VideoSessionEvent.Error(ex.Message, ex));
        }
        finally
        {
            try { codec?.Stop(); } catch { /* 已处于错误态时 Stop 可能抛出，忽略 */ }
            codec?.Release();
            extractor?.Release();
        }
    }

    /// <summary>本地路径直接设置，网络 URL 走 setDataSource(String) 的 http 支持。</summary>
    private static void SetDataSource(MediaExtractor extractor, string uri)
    {
        // MediaExtractor 接受 http(s) URL 与本地文件路径；file:// 前缀需去掉。
        string path = uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
            ? uri["file://".Length..].TrimStart('/')
            : uri;

        extractor.SetDataSource(path);
    }

    private static int SelectVideoTrack(MediaExtractor extractor, out MediaFormat format, out string mimeType)
    {
        for (int i = 0; i < extractor.TrackCount; i++)
        {
            MediaFormat trackFormat = extractor.GetTrackFormat(i);
            string? mime = trackFormat.GetString(MediaFormat.KeyMime);

            if (mime != null && mime.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                format = trackFormat;
                mimeType = mime;
                return i;
            }
        }

        format = null!;
        mimeType = string.Empty;
        return -1;
    }

    /// <summary>
    /// 标准 MediaCodec 循环：把 extractor 的样本填进输入缓冲，取出解码好的输出缓冲并
    /// 以 <c>render: true</c> 提交到 Surface。按 PTS 与墙钟节流以获得正确播放速度。
    /// </summary>
    private void RunDecodeLoop(MediaExtractor extractor, MediaCodec codec)
    {
        var bufferInfo = new MediaCodec.BufferInfo();
        var playbackClock = System.Diagnostics.Stopwatch.StartNew();
        double clockOriginSeconds = 0;
        bool clockStarted = false;
        bool inputDone = false;

        while (!_stopRequested)
        {
            if (TryHandleSeek(extractor, codec, ref clockStarted, ref inputDone))
                continue;

            if (!_playRequested)
            {
                if (_state == VideoSessionState.Playing) _state = VideoSessionState.Paused;
                Thread.Sleep(15);
                continue;
            }

            if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing;

            if (!inputDone)
                inputDone = FeedInput(extractor, codec);

            int outputIndex = codec.DequeueOutputBuffer(bufferInfo, DequeueTimeoutUs);

            if (outputIndex >= 0)
            {
                bool endOfStream = (bufferInfo.Flags & MediaCodecBufferFlags.EndOfStream) != 0;
                double frameSeconds = bufferInfo.PresentationTimeUs / 1_000_000.0;

                if (!clockStarted)
                {
                    clockStarted = true;
                    clockOriginSeconds = frameSeconds;
                    playbackClock.Restart();
                }

                double waitSeconds = (frameSeconds - clockOriginSeconds) - playbackClock.Elapsed.TotalSeconds;
                if (waitSeconds > 0.001)
                    Thread.Sleep((int)Math.Min(waitSeconds * 1000, 1000));

                // render: true → 帧写入 Surface（OES 纹理），全程不经 CPU。
                codec.ReleaseOutputBuffer(outputIndex, render: true);

                var pts = TimeSpan.FromTicks(bufferInfo.PresentationTimeUs * 10);
                Interlocked.Exchange(ref _positionTicks, pts.Ticks);

                // 通知帧源有新帧待取（实际 UpdateTexImage 在渲染线程完成）。
                _frameSource.NotifyFrameAvailable(pts, _videoWidth, _videoHeight);
                Raise(new VideoSessionEvent.FrameAvailable(pts));

                if (endOfStream)
                {
                    if (!HandleEndOfStream(extractor, codec, ref clockStarted, ref inputDone))
                        return;
                }
            }
            else if (outputIndex == (int)MediaCodecInfoState.TryAgainLater)
            {
                // 解码器暂无输出：继续循环（超时已在 DequeueOutputBuffer 中等待过）。
            }
        }
    }

    /// <summary>把一个样本送进解码器。返回 true 表示输入流已结束。</summary>
    private static bool FeedInput(MediaExtractor extractor, MediaCodec codec)
    {
        int inputIndex = codec.DequeueInputBuffer(DequeueTimeoutUs);
        if (inputIndex < 0) return false;

        ByteBuffer? inputBuffer = codec.GetInputBuffer(inputIndex);
        if (inputBuffer == null) return false;

        int sampleSize = extractor.ReadSampleData(inputBuffer, 0);
        if (sampleSize < 0)
        {
            // 无更多样本：送入 EOS 标记让解码器冲刷剩余帧。
            codec.QueueInputBuffer(inputIndex, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
            return true;
        }

        codec.QueueInputBuffer(inputIndex, 0, sampleSize, extractor.SampleTime, MediaCodecBufferFlags.None);
        extractor.Advance();
        return false;
    }

    private bool TryHandleSeek(
        MediaExtractor extractor, MediaCodec codec, ref bool clockStarted, ref bool inputDone)
    {
        long seekTicks;
        lock (_controlLock) { seekTicks = _seekRequestTicks; _seekRequestTicks = -1; }
        if (seekTicks < 0) return false;

        // MediaExtractor 以微秒定位；SeekTo 到最近的同步帧。
        long seekUs = seekTicks / 10;
        extractor.SeekTo(seekUs, MediaExtractorSeekTo.PreviousSync);
        codec.Flush();

        clockStarted = false;
        inputDone = false;
        Interlocked.Exchange(ref _positionTicks, seekTicks);
        if (_state == VideoSessionState.Ended) _state = VideoSessionState.Playing;
        return true;
    }

    /// <summary>返回 true 表示继续循环，false 表示会话结束。</summary>
    private bool HandleEndOfStream(
        MediaExtractor extractor, MediaCodec codec, ref bool clockStarted, ref bool inputDone)
    {
        if (_loop)
        {
            extractor.SeekTo(0, MediaExtractorSeekTo.PreviousSync);
            codec.Flush();
            clockStarted = false;
            inputDone = false;
            return true;
        }

        _state = VideoSessionState.Ended;
        Raise(new VideoSessionEvent.Ended());

        while (!_stopRequested && _state == VideoSessionState.Ended)
        {
            lock (_controlLock) { if (_seekRequestTicks >= 0) return true; }
            Thread.Sleep(15);
        }

        return !_stopRequested;
    }

    private void Raise(VideoSessionEvent evt) => Event?.Invoke(evt);

    public void Dispose()
    {
        _stopRequested = true;
        _decodeThread?.Join(1000);
        _frameSource.Dispose();
    }
}
