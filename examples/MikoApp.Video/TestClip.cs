using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Utils;

namespace MikoApp.Video;

/// <summary>
/// 用 bundled FFmpeg 生成一段测试 MP4（移动渐变 + 计时条），用于在没有外部视频文件时演示播放。
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
        var codec = Codec.FindEncoderById(AVCodecID.Mpeg4);

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
        };
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
