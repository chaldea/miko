using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using static Miko.Windowing.Video.Linux.GStreamerInterop;

namespace Miko.Windowing.Video.Linux;

/// <summary>
/// GStreamer 播放会话。pipeline 为 <c>uridecodebin ! videoconvert ! appsink</c>：
/// <c>uridecodebin</c> 自动挑选解复用器与解码器 —— 系统装有 VAAPI/V4L2 插件时
/// 自动走 GPU 硬解，否则退到软解，这是 GStreamer 的既有协商逻辑，无需我们判断。
///
/// <para>
/// 帧格式向 appsink 协商 NV12（硬解原生输出，且避免 videoconvert 做多余的 CPU 转换），
/// 由 <see cref="Nv12FrameComposer"/> 在 GPU 上转 RGB。
/// </para>
///
/// <para>
/// 时序：由 GStreamer 自身的时钟负责节流 —— <c>appsink</c> 默认同步到 pipeline 时钟，
/// 因此 <c>try_pull_sample</c> 天然按帧率返回，不需要我们再做墙钟等待。
/// </para>
///
/// <para><b>未经真机运行验证</b>：本机为 Windows，此实现仅通过编译验证。</para>
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class GStreamerVideoSession : IVideoSession
{
    // appsink 拉帧超时：100ms。足够短以便及时响应停止/暂停，又不至于空转过密。
    private const ulong PullTimeoutNanos = 100_000_000;

    private readonly VideoSourceDescriptor _source;
    private readonly VideoSessionOptions _options;
    private readonly ILogger _logger;
    private readonly GStreamerFrameSource _frameSource = new();

    private IntPtr _pipeline;
    private IntPtr _appsink;

    private Thread? _pullThread;
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

    public GStreamerVideoSession(VideoSourceDescriptor source, VideoSessionOptions options, ILogger? logger)
    {
        _source = source;
        _options = options;
        _logger = logger ?? NullLogger.Instance;
        _loop = options.Loop;
        _playRequested = options.AutoPlay;
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
        _pullThread = new Thread(PullLoop)
        {
            IsBackground = true,
            Name = "miko-video-gst",
        };
        _pullThread.Start();
    }

    // ---- 控制 --------------------------------------------------------------

    // 控制方法只设置意图标志，真正的 gst_element_set_state 由拉帧线程执行。
    // 不能在主线程直接碰 _pipeline：它由拉帧线程创建并在 TeardownPipeline 中释放，
    // 「判空后再使用」不是原子的 —— 释放恰好插在两步之间就会对已 free 的指针发调用（原生崩溃）。

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

    // 音频轨未接入 pipeline（只取视频），保持 no-op 与其它后端一致。
    public void SetVolume(float volume) { }
    public void SetMuted(bool muted) { }
    public void SetPlaybackRate(float rate) { }

    // ---- pipeline ----------------------------------------------------------

    private void PullLoop()
    {
        try
        {
            if (!gst_is_initialized())
                gst_init(IntPtr.Zero, IntPtr.Zero);

            BuildPipeline();

            // 先进 PAUSED 完成 preroll（协商 caps、拿到首帧与时长），再按需播放。
            if (gst_element_set_state(_pipeline, GstState.Paused) == GstStateChangeReturn.Failure)
                throw new InvalidOperationException("GStreamer pipeline failed to reach PAUSED.");

            // 等待状态切换完成（异步），超时 5 秒。
            gst_element_get_state(_pipeline, out _, out _, 5_000_000_000UL);

            ReadDuration();

            _state = _playRequested ? VideoSessionState.Playing : VideoSessionState.Ready;
            if (_playRequested)
                gst_element_set_state(_pipeline, GstState.Playing);

            RunPullLoop();
        }
        catch (Exception ex)
        {
            _state = VideoSessionState.Error;
            _logger.LogError(ex, "GStreamer playback failed for {Uri}", _source.Uri);
            Raise(new VideoSessionEvent.Error(ex.Message, ex));
        }
        finally
        {
            TeardownPipeline();
        }
    }

    /// <summary>
    /// 构建 pipeline。<c>max-buffers=2 drop=true</c> 让 appsink 只保留最近帧，
    /// 渲染慢于解码时丢帧而非阻塞解码器（与帧源的"只留最近一帧"策略一致）。
    /// </summary>
    private void BuildPipeline()
    {
        // uridecodebin 需要合法 URI：先按 Miko 约定归一化相对路径，再转成 file:// 形式。
        string uri = ToUri(_source.ResolveForBackend());

        string description =
            $"uridecodebin uri=\"{uri}\" ! videoconvert ! " +
            "video/x-raw,format=NV12 ! appsink name=miko-sink max-buffers=2 drop=true sync=true";

        _pipeline = gst_parse_launch(description, out var error);
        string? errorMessage = TakeErrorMessage(error);

        if (_pipeline == IntPtr.Zero)
            throw new InvalidOperationException(
                $"gst_parse_launch failed: {errorMessage ?? "unknown error"}");

        _appsink = gst_bin_get_by_name(_pipeline, "miko-sink");
        if (_appsink == IntPtr.Zero)
            throw new InvalidOperationException("appsink 'miko-sink' not found in pipeline.");

        gst_app_sink_set_max_buffers(_appsink, 2);
        gst_app_sink_set_drop(_appsink, true);
    }

    /// <summary>本地路径转 file:// URI；已带 scheme 的原样返回。</summary>
    private static string ToUri(string source)
    {
        if (source.Contains("://", StringComparison.Ordinal))
            return source;

        // 已由 ResolveForBackend 归一化为绝对路径（或确认无法归一化）。
        return new Uri(Path.GetFullPath(source)).AbsoluteUri;
    }

    private void ReadDuration()
    {
        if (gst_element_query_duration(_pipeline, GstFormat.Time, out long nanos) && nanos > 0)
            _duration = FromGstTime(nanos);
    }

    private void RunPullLoop()
    {
        bool loadedRaised = false;
        // 跟踪已下发给 pipeline 的状态，只在意图变化时调用 set_state（该调用不便每轮重复）。
        bool pipelinePlaying = _playRequested;

        while (!_stopRequested)
        {
            HandlePendingSeek();

            if (!_playRequested)
            {
                // 在本线程切换 pipeline 状态：_pipeline 的创建与释放都在这里，无竞态。
                if (pipelinePlaying)
                {
                    gst_element_set_state(_pipeline, GstState.Paused);
                    pipelinePlaying = false;
                }

                if (_state == VideoSessionState.Playing) _state = VideoSessionState.Paused;
                Thread.Sleep(15);
                continue;
            }

            if (!pipelinePlaying)
            {
                gst_element_set_state(_pipeline, GstState.Playing);
                pipelinePlaying = true;
            }

            if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing;

            IntPtr sample = gst_app_sink_try_pull_sample(_appsink, PullTimeoutNanos);
            if (sample == IntPtr.Zero)
            {
                if (gst_app_sink_is_eos(_appsink) && !HandleEndOfStream())
                    return;
                continue;
            }

            try
            {
                var frame = ReadSample(sample, out int width, out int height);
                if (frame == null) continue;

                // 首帧确定内禀尺寸后才能上报 Loaded（caps 在 preroll 后才完整）。
                if (!loadedRaised)
                {
                    loadedRaised = true;
                    _videoWidth = width;
                    _videoHeight = height;
                    Raise(new VideoSessionEvent.Loaded(width, height, _duration));
                }

                _frameSource.PushFrame(frame);

                if (gst_element_query_position(_pipeline, GstFormat.Time, out long posNanos) && posNanos >= 0)
                    Interlocked.Exchange(ref _positionTicks, FromGstTime(posNanos).Ticks);

                Raise(new VideoSessionEvent.FrameAvailable(frame.Pts));
            }
            finally
            {
                gst_sample_unref(sample);
            }
        }
    }

    private void HandlePendingSeek()
    {
        long seekTicks;
        lock (_controlLock) { seekTicks = _seekRequestTicks; _seekRequestTicks = -1; }
        if (seekTicks < 0) return;

        gst_element_seek_simple(
            _pipeline, GstFormat.Time,
            GstSeekFlags.Flush | GstSeekFlags.KeyUnit,
            ToGstTime(TimeSpan.FromTicks(seekTicks)));

        Interlocked.Exchange(ref _positionTicks, seekTicks);
        if (_state == VideoSessionState.Ended) _state = VideoSessionState.Playing;
    }

    /// <summary>返回 true 表示继续循环（已 loop 回起点），false 表示会话结束。</summary>
    private bool HandleEndOfStream()
    {
        if (_loop)
        {
            gst_element_seek_simple(_pipeline, GstFormat.Time, GstSeekFlags.Flush | GstSeekFlags.KeyUnit, 0);
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

    /// <summary>
    /// 从 sample 拷出 NV12 两个平面。GStreamer 的 NV12 缓冲把 Y 与 UV 前后相接，
    /// stride 对齐到 4 字节边界（<c>GST_ROUND_UP_4</c>）。
    /// </summary>
    private static VideoFrameBuffer? ReadSample(IntPtr sample, out int width, out int height)
    {
        width = 0;
        height = 0;

        IntPtr caps = gst_sample_get_caps(sample);
        if (caps == IntPtr.Zero) return null;

        IntPtr structure = gst_caps_get_structure(caps, 0);
        if (structure == IntPtr.Zero) return null;
        if (!gst_structure_get_int(structure, "width", out width)) return null;
        if (!gst_structure_get_int(structure, "height", out height)) return null;
        if (width <= 0 || height <= 0) return null;

        IntPtr buffer = gst_sample_get_buffer(sample);
        if (buffer == IntPtr.Zero) return null;

        // 读取帧的 PTS（Presentation Time Stamp），单位纳秒，转为 100ns ticks。
        ulong ptsNanos = gst_buffer_get_pts(buffer);
        TimeSpan pts = ptsNanos == ulong.MaxValue  // GST_CLOCK_TIME_NONE
            ? TimeSpan.Zero
            : TimeSpan.FromTicks((long)(ptsNanos / 100));

        if (!gst_buffer_map(buffer, out var mapInfo, GstMapFlags.Read))
            return null;

        try
        {
            // GStreamer 的 NV12 stride 向上取整到 4 的倍数。
            int stride = (width + 3) & ~3;
            int uvHeight = (height + 1) / 2;

            long required = (long)stride * height + (long)stride * uvHeight;
            if ((long)mapInfo.Size < required)
                return null;    // 缓冲小于预期布局，跳过该帧而不是越界读取

            // UV 起始偏移由缓冲总长反推：解码器可能把 Y 平面行数向上对齐到宏块边界
            // （如 180 → 192），此时 UV 并不从 stride*height 开始。按未对齐高度定位会把
            // Y 的填充行读成色度，表现为画面顶部绿边（U=V=0 即纯绿）。
            int totalRows = (int)((long)mapInfo.Size / stride);
            int alignedYHeight = totalRows * 2 / 3;
            if (alignedYHeight < height)
                alignedYHeight = height;

            int uvOffset = stride * alignedYHeight;
            if (uvOffset + stride * uvHeight > (long)mapInfo.Size)
                uvOffset = stride * height;   // 反推不成立时退回紧凑布局

            var yPlane = new byte[stride * height];
            var uvPlane = new byte[stride * uvHeight];

            Marshal.Copy(mapInfo.Data, yPlane, 0, yPlane.Length);
            Marshal.Copy(mapInfo.Data + uvOffset, uvPlane, 0, uvPlane.Length);

            return VideoFrameBuffer.FromCpuPlanes(
                width, height, VideoPixelFormat.Nv12, pts,
                [yPlane, uvPlane], [stride, stride]);
        }
        finally
        {
            gst_buffer_unmap(buffer, ref mapInfo);
        }
    }

    private void TeardownPipeline()
    {
        if (_appsink != IntPtr.Zero)
        {
            g_object_unref(_appsink);
            _appsink = IntPtr.Zero;
        }

        if (_pipeline != IntPtr.Zero)
        {
            gst_element_set_state(_pipeline, GstState.Null);
            g_object_unref(_pipeline);
            _pipeline = IntPtr.Zero;
        }
    }

    private void Raise(VideoSessionEvent evt) => Event?.Invoke(evt);

    public void Dispose()
    {
        _stopRequested = true;
        _pullThread?.Join(1000);
        _frameSource.Dispose();
    }
}
