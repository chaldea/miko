using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Utils;

namespace Miko.Windowing.Video;

/// <summary>
/// FFmpeg 软解会话（Phase 1 基线）。在自有解码线程上解复用 + 软解视频轨，
/// 把帧转换为 RGBA 后推给 <see cref="FFmpegVideoFrameSource"/>，并按 PTS 与时钟节流。
/// <para>
/// 范围限定：本基线只解码视频轨并按墙钟时间呈现；音频与精确 A/V 同步留待后续
/// （见 ISSUE-058 方案 §2.1：音频经 OpenAL/XAudio2，以音频时钟为主时钟）。
/// 这样可在最小依赖下打通「元素 → 布局 → 合成 → 帧节流」全链路。
/// </para>
/// </summary>
internal sealed class FFmpegVideoSession : IVideoSession
{
    private readonly VideoSourceDescriptor _source;
    private readonly VideoSessionOptions _options;
    private readonly ILogger _logger;
    private readonly FFmpegVideoFrameSource _frameSource = new();

    private Thread? _decodeThread;
    private volatile bool _stopRequested;
    private volatile VideoSessionState _state = VideoSessionState.Idle;

    // 播放控制（由解码线程读取）。
    private volatile bool _playRequested;
    private volatile bool _loop;
    private long _seekRequestTicks = -1;          // -1 表示无 seek 请求
    private readonly object _controlLock = new();

    private TimeSpan _duration;
    private long _positionTicks;                  // Interlocked 访问
    private int _videoWidth;
    private int _videoHeight;

    public FFmpegVideoSession(VideoSourceDescriptor source, VideoSessionOptions options, ILogger? logger)
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
            Name = $"miko-video-decode",
        };
        _decodeThread.Start();
    }

    // ---- 控制 --------------------------------------------------------------

    public void Play() { _playRequested = true; if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing; }
    public void Pause() { _playRequested = false; if (_state == VideoSessionState.Playing) _state = VideoSessionState.Paused; }
    public void SetLoop(bool loop) => _loop = loop;

    public void Seek(TimeSpan position)
    {
        lock (_controlLock) { _seekRequestTicks = position.Ticks; }
    }

    // 音频未接入：以下控制在基线中为 no-op，保留以满足契约并便于 Phase 2 接线。
    public void SetVolume(float volume) { }
    public void SetMuted(bool muted) { }
    public void SetPlaybackRate(float rate) { }

    // ---- 解码循环 ----------------------------------------------------------

    private void DecodeLoop()
    {
        FormatContext? fc = null;
        CodecContext? decoder = null;
        VideoFrameConverter? converter = null;
        using var packet = new Packet();
        using var decoded = new Frame();
        using var rgbaFrame = new Frame();

        try
        {
            fc = FormatContext.OpenInputUrl(_source.Uri);
            fc.LoadStreamInfo();

            MediaStream stream = fc.FindBestStream(AVMediaType.Video);
            int videoStreamIndex = stream.Index;

            // FindBestStream 找到视频轨即保证 Codecpar 有效。
            var codecpar = stream.Codecpar!;
            var codec = Codec.FindDecoderById(codecpar.CodecId);
            decoder = new CodecContext(codec);
            decoder.FillParameters(codecpar);
            decoder.Open(codec);

            _videoWidth = decoder.Width;
            _videoHeight = decoder.Height;
            _duration = fc.Duration > 0
                ? TimeSpan.FromSeconds(fc.Duration / (double)ffmpeg.AV_TIME_BASE)
                : TimeSpan.Zero;

            // 目标 RGBA 帧缓冲（与解码尺寸一致）。
            rgbaFrame.Width = _videoWidth;
            rgbaFrame.Height = _videoHeight;
            rgbaFrame.Format = (int)AVPixelFormat.Rgba;
            rgbaFrame.EnsureBuffer();

            converter = new VideoFrameConverter();

            _state = _playRequested ? VideoSessionState.Playing : VideoSessionState.Ready;
            Raise(new VideoSessionEvent.Loaded(_videoWidth, _videoHeight, _duration));

            // 时间基：把帧 PTS 换算到秒，用于按墙钟节流。
            double timeBase = stream.TimeBase.Num / (double)stream.TimeBase.Den;

            var playbackClock = System.Diagnostics.Stopwatch.StartNew();
            double clockOriginSeconds = 0;       // 播放时钟原点对应的媒体秒数
            bool clockStarted = false;

            while (!_stopRequested)
            {
                // 处理 seek 请求
                long seekTicks;
                lock (_controlLock) { seekTicks = _seekRequestTicks; _seekRequestTicks = -1; }
                if (seekTicks >= 0)
                {
                    long ts = (long)(TimeSpan.FromTicks(seekTicks).TotalSeconds / timeBase);
                    fc.SeekFrame(ts, videoStreamIndex, AVSEEK_FLAG.Backward);
                    FlushDecoder(decoder);
                    clockStarted = false;
                }

                // 暂停：空转等待
                if (!_playRequested)
                {
                    _state = VideoSessionState.Paused;
                    Thread.Sleep(15);
                    continue;
                }
                if (_state == VideoSessionState.Paused) _state = VideoSessionState.Playing;

                // 读一个包
                var readResult = fc.ReadFrame(packet);
                if (readResult == CodecResult.EOF)
                {
                    if (_loop)
                    {
                        fc.SeekFrame(0, videoStreamIndex, AVSEEK_FLAG.Backward);
                        FlushDecoder(decoder);
                        clockStarted = false;
                        continue;
                    }
                    _state = VideoSessionState.Ended;
                    Raise(new VideoSessionEvent.Ended());
                    // 停在结尾，等待 seek/play 控制
                    while (!_stopRequested && _state == VideoSessionState.Ended)
                    {
                        lock (_controlLock) { if (_seekRequestTicks >= 0) break; }
                        Thread.Sleep(15);
                    }
                    continue;
                }

                try
                {
                    if (packet.StreamIndex != videoStreamIndex)
                        continue;

                    decoder.SendPacket(packet);

                    while (!_stopRequested)
                    {
                        var recv = decoder.ReceiveFrame(decoded);
                        if (recv == CodecResult.Again || recv == CodecResult.EOF)
                            break;

                        // 帧 PTS（秒）
                        long ptsRaw = decoded.BestEffortTimestamp;
                        double frameSeconds = ptsRaw == ffmpeg.AV_NOPTS_VALUE ? 0 : ptsRaw * timeBase;

                        // 按墙钟节流：把播放时钟对齐到首帧媒体时间，之后等待直到该帧到点。
                        if (!clockStarted)
                        {
                            clockStarted = true;
                            clockOriginSeconds = frameSeconds;
                            playbackClock.Restart();
                        }
                        double targetElapsed = frameSeconds - clockOriginSeconds;
                        double actualElapsed = playbackClock.Elapsed.TotalSeconds;
                        double waitSeconds = targetElapsed - actualElapsed;
                        if (waitSeconds > 0.001)
                            Thread.Sleep((int)Math.Min(waitSeconds * 1000, 1000));

                        // 转换为 RGBA 并推送
                        converter.ConvertFrame(decoded, rgbaFrame, SWS.Bilinear);
                        PushRgba(rgbaFrame);

                        Interlocked.Exchange(ref _positionTicks, TimeSpan.FromSeconds(frameSeconds).Ticks);
                        Raise(new VideoSessionEvent.FrameAvailable(TimeSpan.FromSeconds(frameSeconds)));
                    }
                }
                finally
                {
                    packet.Unref();
                }
            }
        }
        catch (Exception ex)
        {
            _state = VideoSessionState.Error;
            _logger.LogError(ex, "Video decode failed for {Uri}", _source.Uri);
            Raise(new VideoSessionEvent.Error(ex.Message, ex));
        }
        finally
        {
            converter?.Free();
            decoder?.Free();
            fc?.Free();
        }
    }

    /// <summary>把 RGBA 帧的紧凑像素拷出并推给帧源。</summary>
    private unsafe void PushRgba(Frame rgbaFrame)
    {
        int w = rgbaFrame.Width;
        int h = rgbaFrame.Height;
        int stride = rgbaFrame.Linesize[0];
        byte* src = (byte*)rgbaFrame.Data[0];

        var buffer = new byte[w * h * 4];
        fixed (byte* dst = buffer)
        {
            if (stride == w * 4)
            {
                // 无行填充：整块拷贝
                Buffer.MemoryCopy(src, dst, buffer.Length, buffer.Length);
            }
            else
            {
                // 有行填充：逐行拷贝去除 padding
                for (int y = 0; y < h; y++)
                    Buffer.MemoryCopy(src + y * stride, dst + y * w * 4, w * 4, w * 4);
            }
        }

        _frameSource.PushFrame(buffer, w, h);
    }

    /// <summary>清空解码器内部缓冲（seek / loop 后调用）。Sdcb 未暴露高层包装，直接调 raw API。</summary>
    private static unsafe void FlushDecoder(CodecContext decoder)
    {
        ffmpeg.avcodec_flush_buffers((AVCodecContext*)decoder.DangerousGetHandle());
    }

    private void Raise(VideoSessionEvent evt) => Event?.Invoke(evt);

    public void Dispose()
    {
        _stopRequested = true;
        _decodeThread?.Join(500);
        _frameSource.Dispose();
    }
}
