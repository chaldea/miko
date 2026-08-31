using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using static Miko.Windowing.Video.Windows.MediaFoundationInterop;

namespace Miko.Windowing.Video.Windows;

/// <summary>
/// Media Foundation 播放会话。用 <c>IMFSourceReader</c> 解复用 + 解码一路视频轨，
/// 解码由系统选择的硬件 MFT 完成（读取器开启了硬件变换与高级视频处理）。
///
/// <para>
/// 帧交付：向读取器请求 NV12（硬解器原生输出，避免读取器内部再插一次色彩转换），
/// 取到 <c>IMF2DBuffer</c> 后按真实 stride 逐平面拷出，交给
/// <see cref="MediaFoundationFrameSource"/>，由其经 SkSL shader 在 GPU 上转 RGB。
/// </para>
///
/// <para>
/// 关于零拷贝：完整的 D3D11 纹理零拷贝需要 <c>WGL_NV_DX_interop2</c> 把解码输出的
/// <c>ID3D11Texture2D</c> 注册进 GL 上下文。该扩展依赖驱动支持，且必须在渲染线程
/// 持有 GL 上下文时操作；当前实现走「系统硬解 + NV12 平面上传 + GPU 侧色彩转换」，
/// CPU 只搬运一次 NV12（约为 RGBA 的 3/8 体积），色彩转换仍在 GPU。
/// 见 <see cref="MediaFoundationFrameSource"/> 的说明。
/// </para>
///
/// <para>线程模型：控制方法在主线程调用；读取与解码在自有线程（<see cref="DecodeLoop"/>）。</para>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MediaFoundationVideoSession : IVideoSession
{
    private readonly VideoSourceDescriptor _source;
    private readonly VideoSessionOptions _options;
    private readonly ILogger _logger;
    private readonly MediaFoundationFrameSource _frameSource = new();

    private Thread? _decodeThread;
    private volatile bool _stopRequested;
    private volatile VideoSessionState _state = VideoSessionState.Idle;

    // 播放控制（解码线程读取）。
    private volatile bool _playRequested;
    private volatile bool _loop;
    private volatile float _playbackRate = 1.0f;
    private long _seekRequestTicks = -1;             // -1 表示无请求
    private readonly object _controlLock = new();

    private TimeSpan _duration;
    private long _positionTicks;
    private int _videoWidth;
    private int _videoHeight;

    public MediaFoundationVideoSession(VideoSourceDescriptor source, VideoSessionOptions options, ILogger? logger)
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

    /// <summary>启动解码线程。由后端在创建后立即调用。</summary>
    public void Start()
    {
        _state = VideoSessionState.Loading;
        _decodeThread = new Thread(DecodeLoop)
        {
            IsBackground = true,
            Name = "miko-video-mf",
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

    public void SetPlaybackRate(float rate) => _playbackRate = rate <= 0 ? 1.0f : rate;

    // 音频未接入本会话（SourceReader 只选了视频轨）：与 FFmpeg 基线一致保持 no-op。
    public void SetVolume(float volume) { }
    public void SetMuted(bool muted) { }

    // ---- 解码循环 ----------------------------------------------------------

    private void DecodeLoop()
    {
        IMFSourceReader? reader = null;
        bool mfStarted = false;

        try
        {
            ThrowIfFailed(MFStartup(MF_VERSION, MFSTARTUP_NOSOCKET), "MFStartup");
            mfStarted = true;

            reader = CreateReader();
            ConfigureVideoStream(reader, out bool isNv12);
            ReadDuration(reader);

            _state = _playRequested ? VideoSessionState.Playing : VideoSessionState.Ready;
            Raise(new VideoSessionEvent.Loaded(_videoWidth, _videoHeight, _duration));
            _logger.LogInformation(
                "Media Foundation session ready: {Width}x{Height}, {Duration}, format={Format}",
                _videoWidth, _videoHeight, _duration, isNv12 ? "NV12" : "RGB32");

            RunPlaybackLoop(reader, isNv12);
        }
        catch (Exception ex)
        {
            _state = VideoSessionState.Error;
            _logger.LogError(ex, "Media Foundation decode failed for {Uri}", _source.Uri);
            Raise(new VideoSessionEvent.Error(ex.Message, ex));
        }
        finally
        {
            if (reader != null) Marshal.ReleaseComObject(reader);
            if (mfStarted) MFShutdown();
        }
    }

    /// <summary>
    /// 创建读取器并开启硬件解码。<c>ENABLE_ADVANCED_VIDEO_PROCESSING</c> 让读取器在
    /// 解码器输出格式与请求格式不一致时自动插入转换器，从而可以稳定地请求 NV12/RGB32。
    /// </summary>
    private IMFSourceReader CreateReader()
    {
        ThrowIfFailed(MFCreateAttributes(out var attributes, 4), "MFCreateAttributes");
        try
        {
            attributes.SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);
            attributes.SetUINT32(MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING, 1);

            // MF 把相对路径解析到进程 CWD，而 Miko 约定基于 AppContext.BaseDirectory，
            // 故统一先归一化（见 VideoSourceDescriptor.ResolveForBackend）。
            string url = _source.ResolveForBackend();

            ThrowIfFailed(
                MFCreateSourceReaderFromURL(url, attributes, out var reader),
                $"MFCreateSourceReaderFromURL({url})");
            return reader;
        }
        finally
        {
            Marshal.ReleaseComObject(attributes);
        }
    }

    /// <summary>
    /// 只选视频轨并协商输出格式：优先 NV12（硬解原生，带宽最省），
    /// 失败则退到 RGB32（读取器内部转换，兼容性最好）。同时读出帧尺寸。
    /// </summary>
    private void ConfigureVideoStream(IMFSourceReader reader, out bool isNv12)
    {
        // 只选视频轨：不选音频可避免读取器为未消费的音频样本堆积缓冲。
        reader.SetStreamSelection(MF_SOURCE_READER_MEDIASOURCE, false);
        ThrowIfFailed(
            reader.SetStreamSelection(MF_SOURCE_READER_FIRST_VIDEO_STREAM, true),
            "SetStreamSelection(video)");

        isNv12 = TrySetOutputFormat(reader, MFVideoFormat_NV12);
        if (!isNv12 && !TrySetOutputFormat(reader, MFVideoFormat_RGB32))
            throw new NotSupportedException(
                "Media Foundation could not deliver NV12 or RGB32 for this source; " +
                "the codec may not be installed on this system.");

        ThrowIfFailed(
            reader.GetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, out var currentType),
            "GetCurrentMediaType");
        try
        {
            // MF_MT_FRAME_SIZE 把宽高打包进一个 64 位值：高 32 位宽、低 32 位高。
            ThrowIfFailed(currentType.GetUINT64(MF_MT_FRAME_SIZE, out ulong packedSize), "GetUINT64(FRAME_SIZE)");
            _videoWidth = (int)(packedSize >> 32);
            _videoHeight = (int)(packedSize & 0xFFFFFFFF);

            if (_videoWidth <= 0 || _videoHeight <= 0)
                throw new InvalidOperationException($"Invalid frame size {_videoWidth}x{_videoHeight} from Media Foundation.");
        }
        finally
        {
            Marshal.ReleaseComObject(currentType);
        }
    }

    private static bool TrySetOutputFormat(IMFSourceReader reader, Guid subtype)
    {
        if (MFCreateMediaType(out var mediaType) < 0) return false;
        try
        {
            mediaType.SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
            mediaType.SetGUID(MF_MT_SUBTYPE, subtype);
            return reader.SetCurrentMediaType(
                MF_SOURCE_READER_FIRST_VIDEO_STREAM, IntPtr.Zero, mediaType) >= 0;
        }
        finally
        {
            Marshal.ReleaseComObject(mediaType);
        }
    }

    /// <summary>读取媒体总时长。取不到时保持 <see cref="TimeSpan.Zero"/>（直播流即如此）。</summary>
    private void ReadDuration(IMFSourceReader reader)
    {
        if (reader.GetPresentationAttribute(MF_SOURCE_READER_MEDIASOURCE, MF_PD_DURATION, out var variant) < 0)
            return;

        try
        {
            // MF 时长单位为 100ns，与 TimeSpan.Ticks 一致。
            if (variant.Type is PropVariant.VT_UI8 or PropVariant.VT_I8 && variant.Value > 0)
                _duration = TimeSpan.FromTicks(variant.Value);
        }
        finally
        {
            variant.Clear();
        }
    }

    /// <summary>
    /// 主播放循环：按墙钟节流读取样本。时钟对齐到首帧 PTS，之后每帧等到其呈现时刻，
    /// 与 FFmpeg 基线相同的策略（无音轨时以视频 PTS 为主时钟）。
    /// </summary>
    private void RunPlaybackLoop(IMFSourceReader reader, bool isNv12)
    {
        var playbackClock = Stopwatch.StartNew();
        double clockOriginSeconds = 0;
        bool clockStarted = false;

        while (!_stopRequested)
        {
            if (TryHandleSeek(reader, ref clockStarted))
                continue;

            if (!_playRequested)
            {
                if (_state == VideoSessionState.Playing) _state = VideoSessionState.Paused;
                Thread.Sleep(15);
                continue;
            }

            if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing;

            int hr = reader.ReadSample(
                MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0,
                out _, out uint streamFlags, out long timestamp, out var sample);

            if (hr < 0)
            {
                ThrowIfFailed(hr, "ReadSample");
                return;
            }

            try
            {
                if ((streamFlags & MF_SOURCE_READERF_ENDOFSTREAM) != 0)
                {
                    if (HandleEndOfStream(reader, ref clockStarted))
                        continue;
                    return;
                }

                if (sample == null)
                    continue;   // 无样本但未结束（格式变更等），继续读

                // MF 时间戳单位 100ns，与 Ticks 一致。
                var pts = TimeSpan.FromTicks(timestamp);
                double frameSeconds = pts.TotalSeconds;

                if (!clockStarted)
                {
                    clockStarted = true;
                    clockOriginSeconds = frameSeconds;
                    playbackClock.Restart();
                }

                // 按速率缩放的墙钟节流：rate=2 时同样的媒体时间只等一半墙钟。
                double targetElapsed = (frameSeconds - clockOriginSeconds) / _playbackRate;
                double waitSeconds = targetElapsed - playbackClock.Elapsed.TotalSeconds;
                if (waitSeconds > 0.001)
                    Thread.Sleep((int)Math.Min(waitSeconds * 1000, 1000));

                DeliverSample(sample, pts, isNv12);

                Interlocked.Exchange(ref _positionTicks, pts.Ticks);
                Raise(new VideoSessionEvent.FrameAvailable(pts));
            }
            finally
            {
                if (sample != null) Marshal.ReleaseComObject(sample);
            }
        }
    }

    /// <summary>处理挂起的 seek 请求。返回 true 表示本轮已处理，调用方应重新开始循环。</summary>
    private bool TryHandleSeek(IMFSourceReader reader, ref bool clockStarted)
    {
        long seekTicks;
        lock (_controlLock) { seekTicks = _seekRequestTicks; _seekRequestTicks = -1; }
        if (seekTicks < 0) return false;

        var position = PropVariant.FromLong(seekTicks);
        try
        {
            // 空 GUID 表示默认时间格式（100ns）。
            int hr = reader.SetCurrentPosition(Guid.Empty, position);
            if (hr < 0)
                _logger.LogWarning("Seek to {Position} failed (HRESULT 0x{Hr:X8})", TimeSpan.FromTicks(seekTicks), hr);
            else
                Interlocked.Exchange(ref _positionTicks, seekTicks);
        }
        finally
        {
            position.Clear();
        }

        // seek 后时钟原点失效，需重新对齐到下一帧 PTS。
        clockStarted = false;
        if (_state == VideoSessionState.Ended) _state = VideoSessionState.Playing;
        return true;
    }

    /// <summary>
    /// 流结束：开启 loop 时回到起点继续，否则进入 Ended 并等待后续控制。
    /// 返回 true 表示应继续循环，false 表示会话应结束。
    /// </summary>
    private bool HandleEndOfStream(IMFSourceReader reader, ref bool clockStarted)
    {
        if (_loop)
        {
            var start = PropVariant.FromLong(0);
            try { reader.SetCurrentPosition(Guid.Empty, start); }
            finally { start.Clear(); }

            clockStarted = false;
            return true;
        }

        _state = VideoSessionState.Ended;
        Raise(new VideoSessionEvent.Ended());

        // 停在结尾等待 seek / 重播；期间不再读取样本。
        while (!_stopRequested && _state == VideoSessionState.Ended)
        {
            lock (_controlLock) { if (_seekRequestTicks >= 0) return true; }
            Thread.Sleep(15);
        }

        return !_stopRequested;
    }

    /// <summary>
    /// 把样本的像素拷入托管缓冲并推给帧源。优先经 <c>IMF2DBuffer</c> 拿真实 stride：
    /// 硬解输出常带行填充，按 <c>width * bpp</c> 寻址会导致画面斜切。
    /// </summary>
    private void DeliverSample(IMFSample sample, TimeSpan pts, bool isNv12)
    {
        if (sample.ConvertToContiguousBuffer(out var buffer) < 0)
            return;

        try
        {
            var frame = isNv12
                ? ReadNv12Frame(buffer, pts)
                : ReadRgb32Frame(buffer, pts);

            if (frame != null)
                _frameSource.PushFrame(frame);
        }
        finally
        {
            Marshal.ReleaseComObject(buffer);
        }
    }

    /// <summary>
    /// 读 NV12：Y 平面在前、交错 UV 平面在后。
    /// 拷成两个独立平面交给帧源，由 shader 在 GPU 上转 RGB。
    ///
    /// <para>
    /// **关键**：UV 平面的起始偏移不是 <c>stride * height</c>。硬解器会把 Y 平面的**行数**
    /// 向上对齐到宏块边界（如 180 → 192），UV 紧随对齐后的 Y 之后。按未对齐的 height
    /// 定位会把 Y 平面尾部的填充行当成色度读入 —— 表现为画面顶部一条绿边
    /// （U=V=0 即纯绿）。因此这里由缓冲总长反推真实的对齐高度。
    /// </para>
    /// </summary>
    private VideoFrameBuffer? ReadNv12Frame(IMFMediaBuffer buffer, TimeSpan pts)
    {
        int width = _videoWidth;
        int height = _videoHeight;
        int uvHeight = (height + 1) / 2;

        if (buffer.GetCurrentLength(out uint totalLength) < 0)
            return null;

        if (!TryLockBuffer(buffer, width, (int)totalLength, out var scanline0, out int stride, out var unlock))
            return null;

        try
        {
            // NV12 总高 = alignedY + alignedY/2 = alignedY * 3/2，故 alignedY = totalRows * 2/3。
            int totalRows = (int)(totalLength / (uint)stride);
            int alignedYHeight = totalRows * 2 / 3;

            // 反推值必须至少覆盖真实高度；异常时退回未对齐布局，宁可轻微偏色也不越界。
            if (alignedYHeight < height)
                alignedYHeight = height;

            int uvOffset = stride * alignedYHeight;

            var yPlane = new byte[stride * height];
            var uvPlane = new byte[stride * uvHeight];

            Marshal.Copy(scanline0, yPlane, 0, yPlane.Length);

            // 越界保护：缓冲不足时跳过该帧，而不是读到相邻内存。
            if (uvOffset + uvPlane.Length > totalLength)
                return null;

            Marshal.Copy(scanline0 + uvOffset, uvPlane, 0, uvPlane.Length);

            return VideoFrameBuffer.FromCpuPlanes(
                width, height, VideoPixelFormat.Nv12, pts,
                [yPlane, uvPlane], [stride, stride]);
        }
        finally
        {
            unlock();
        }
    }

    /// <summary>读 RGB32 回退格式。MF 的 RGB32 是 BGRA 字节序。</summary>
    private VideoFrameBuffer? ReadRgb32Frame(IMFMediaBuffer buffer, TimeSpan pts)
    {
        int width = _videoWidth;
        int height = _videoHeight;

        if (buffer.GetCurrentLength(out uint totalLength) < 0)
            return null;

        if (!TryLockBuffer(buffer, width * 4, (int)totalLength, out var scanline0, out int stride, out var unlock))
            return null;

        try
        {
            var pixels = new byte[stride * height];
            Marshal.Copy(scanline0, pixels, 0, pixels.Length);

            return VideoFrameBuffer.FromCpuPlanes(
                width, height, VideoPixelFormat.Bgra8888, pts,
                [pixels], [stride]);
        }
        finally
        {
            unlock();
        }
    }

    /// <summary>
    /// 锁定缓冲并取得首行地址与 stride。优先 <c>IMF2DBuffer.Lock2D</c>（给出真实 pitch）；
    /// 不支持时退到一维 <c>Lock</c> 并假定紧凑排列。
    /// </summary>
    private static bool TryLockBuffer(
        IMFMediaBuffer buffer, int contiguousStride, int totalSize,
        out IntPtr scanline0, out int stride, out Action unlock)
    {
        if (buffer is IMF2DBuffer buffer2D && buffer2D.Lock2D(out scanline0, out stride) >= 0)
        {
            if (stride == 0)
            {
                // 锁已获得但 stride 不可用：必须在返回 false 前解锁，否则调用方跳过 finally，
                // 该缓冲永远不归还解码器（解码很快因缺缓冲停摆）。
                buffer2D.Unlock2D();
                unlock = static () => { };
                return false;
            }

            // 负 pitch 表示行地址反向递进（自底向上存储）。调整 scanline0 指向首行（逻辑顶部），
            // 然后取绝对值，使后续 Marshal.Copy 按正向连续内存读取。
            if (stride < 0)
            {
                int height = totalSize / -stride;  // totalSize 由调用方从 GetCurrentLength 获得
                scanline0 += stride * (height - 1);  // 移到逻辑首行（物理末行）
                stride = -stride;
            }

            unlock = () => buffer2D.Unlock2D();
            return true;
        }

        if (buffer.Lock(out scanline0, out _, out _) >= 0)
        {
            stride = contiguousStride;
            unlock = () => buffer.Unlock();
            return true;
        }

        scanline0 = IntPtr.Zero;
        stride = 0;
        unlock = static () => { };
        return false;
    }

    private void Raise(VideoSessionEvent evt) => Event?.Invoke(evt);

    public void Dispose()
    {
        _stopRequested = true;
        _decodeThread?.Join(500);
        _frameSource.Dispose();
    }
}
