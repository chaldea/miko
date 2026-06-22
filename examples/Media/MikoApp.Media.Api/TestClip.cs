using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Utils;

namespace MikoApp.Media.Api;

/// <summary>
/// 用 bundled FFmpeg 生成一段测试 MP4（移动渐变 + 计时条），作为离线样例视频供客户端经 http 加载。
/// 仅依赖随 <c>Sdcb.FFmpeg.runtime.windows-x64</c> 提供的原生库。
/// </summary>
public static class TestClip
{
    private const int Width = 320;
    private const int Height = 240;
    private const int Fps = 25;
    private const int Seconds = 5;

    /// <summary>确保示例片段存在并返回其路径（已存在则直接复用）。</summary>
    public static string EnsureSampleClip()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "miko-sample.mp4");
        if (File.Exists(path) && new FileInfo(path).Length > 0)
            return path;

        Encode(path);
        return path;
    }

    private static unsafe void Encode(string path)
    {
        // 用 H.264 编码：标准 MP4 视频编码，浏览器/播放器/FFmpeg 通用。
        // 之前用 MPEG-4 Part 2 (mp4v) 生成的流缺少有效解码器配置，导致播放器无法打开。
        var codec = Codec.FindEncoderById(AVCodecID.H264);

        using var fc = FormatContext.AllocOutput(formatName: null, fileName: path);
        MediaStream stream = fc.NewStream(codec);

        using var encoder = new CodecContext(codec)
        {
            Width = Width,
            Height = Height,
            PixelFormat = AVPixelFormat.Yuv420p,
            TimeBase = new AVRational(1, Fps),
            Framerate = new AVRational(Fps, 1),
            BitRate = 1_500_000,
            GopSize = 12,
            MaxBFrames = 0,
        };

        // MP4 要求把 SPS/PPS 等编码器配置放进容器头（avcC box），而非内联在码流中。
        // 不设置 GLOBAL_HEADER 会让 moov 缺少解码配置，文件无法被解码。
        if (((fc.OutputFormat?.Flags ?? default) & AVFMT.Globalheader) != 0)
            encoder.Flags |= AV_CODEC_FLAG.GlobalHeader;

        encoder.Open(codec);

        stream.Codecpar.CopyFrom(encoder);
        stream.TimeBase = encoder.TimeBase;

        fc.Pb = IOContext.Open(path, AVIO_FLAG.Write);
        fc.WriteHeader();

        using var converter = new VideoFrameConverter();
        using var rgba = new Frame { Width = Width, Height = Height, Format = (int)AVPixelFormat.Rgba };
        rgba.EnsureBuffer();
        using var yuv = new Frame { Width = Width, Height = Height, Format = (int)AVPixelFormat.Yuv420p };
        yuv.EnsureBuffer();
        using var packet = new Packet();

        int totalFrames = Fps * Seconds;
        for (int i = 0; i < totalFrames; i++)
        {
            FillGradient(rgba, i, totalFrames);
            converter.ConvertFrame(rgba, yuv, SWS.Bilinear);
            yuv.Pts = i;

            encoder.SendFrame(yuv);
            DrainEncoder(encoder, fc, stream, packet);
        }

        // flush
        encoder.SendFrame(null!);
        DrainEncoder(encoder, fc, stream, packet);

        fc.WriteTrailer();
    }

    /// <summary>
    /// 解码自检：用与客户端相同的 FFmpeg 解码器打开生成的片段并解码全部帧，
    /// 验证文件可被正常解码（H.264）。返回成功解码的帧数。失败抛异常。
    /// </summary>
    public static int Verify(string path)
    {
        using var fc = FormatContext.OpenInputUrl(path);
        fc.LoadStreamInfo();

        MediaStream stream = fc.FindBestStream(AVMediaType.Video);
        var codecpar = stream.Codecpar!;
        var codec = Codec.FindDecoderById(codecpar.CodecId);

        using var decoder = new CodecContext(codec);
        decoder.FillParameters(codecpar);
        decoder.Open(codec);

        using var packet = new Packet();
        using var frame = new Frame();
        int decoded = 0;

        // 解码到 EOF：与客户端 FFmpegVideoSession 同一解码路径。
        while (fc.ReadFrame(packet) != CodecResult.EOF)
        {
            try
            {
                if (packet.StreamIndex != stream.Index) continue;
                decoder.SendPacket(packet);
                while (decoder.ReceiveFrame(frame) == CodecResult.Success)
                    decoded++;
            }
            finally
            {
                packet.Unref();
            }
        }

        Console.WriteLine($"Verify: codec={codec.Name} {decoder.Width}x{decoder.Height} decodedFrames={decoded}");
        if (decoded == 0)
            throw new InvalidOperationException("Sample clip decoded 0 frames — file is not playable.");
        return decoded;
    }

    private static void DrainEncoder(CodecContext encoder, FormatContext fc, MediaStream stream, Packet packet)
    {
        while (true)
        {
            var result = encoder.ReceivePacket(packet);
            if (result == CodecResult.Again || result == CodecResult.EOF)
                break;

            packet.StreamIndex = stream.Index;
            packet.RescaleTimestamp(encoder.TimeBase, stream.TimeBase);
            fc.InterleavedWritePacket(packet);
            packet.Unref();
        }
    }

    /// <summary>画一帧：随帧序号移动的对角渐变，底部一条随时间推进的进度条。</summary>
    private static unsafe void FillGradient(Frame rgba, int frameIndex, int totalFrames)
    {
        int stride = rgba.Linesize[0];
        byte* data = (byte*)rgba.Data[0];
        int phase = frameIndex * 4;

        for (int y = 0; y < Height; y++)
        {
            byte* row = data + y * stride;
            for (int x = 0; x < Width; x++)
            {
                byte* px = row + x * 4;
                px[0] = (byte)((x + phase) & 0xFF);          // R
                px[1] = (byte)((y + phase) & 0xFF);          // G
                px[2] = (byte)((x + y + phase) & 0xFF);      // B
                px[3] = 255;                                  // A
            }
        }

        // 底部进度条
        int progressX = (int)((frameIndex / (float)totalFrames) * Width);
        for (int y = Height - 16; y < Height; y++)
        {
            byte* row = data + y * stride;
            for (int x = 0; x < progressX; x++)
            {
                byte* px = row + x * 4;
                px[0] = 255; px[1] = 255; px[2] = 255; px[3] = 255;
            }
        }
    }
}
