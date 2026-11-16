namespace Miko.MockServer;

public sealed class MediaLibrary
{
    public const int FramesPerSecond = 5;
    public const int DurationSeconds = 12;
    public string Mp4Path { get; }
    public Raster Background { get; }
    public IReadOnlyList<byte[][]> Frames { get; }
    public byte[] Sps { get; }
    public byte[] Pps { get; }

    public MediaLibrary(MockOptions options)
    {
        Mp4Path = Path.GetFullPath(Path.Combine(options.MediaDirectory, "demo.mp4"));
        if (!File.Exists(Mp4Path)) throw new FileNotFoundException("Bundled media missing. Run --generate-media <ffmpeg>.", Mp4Path);
        Background = Raster.Load(Path.Combine(options.MediaDirectory, "desktop.ppm"), 640, 360);
        var nals = SplitAnnexB(File.ReadAllBytes(Path.Combine(options.MediaDirectory, "panorama.h264")));
        Sps = nals.First(n => (n[0] & 31) == 7);
        Pps = nals.First(n => (n[0] & 31) == 8);
        var frames = new List<byte[][]>();
        var frame = new List<byte[]>();
        foreach (var nal in nals)
        {
            if ((nal[0] & 31) == 9 && frame.Any(n => (n[0] & 31) is 1 or 5)) { frames.Add(frame.ToArray()); frame.Clear(); }
            frame.Add(nal);
        }
        if (frame.Count > 0) frames.Add(frame.ToArray());
        if (frames.Count != FramesPerSecond * DurationSeconds)
            throw new InvalidDataException("Expected 60 AUD-delimited frames in panorama.h264.");
        Frames = frames;
    }

    private static List<byte[]> SplitAnnexB(byte[] data)
    {
        var result = new List<byte[]>();
        int start = -1;
        for (int i = 0; i < data.Length - 3; i++)
        {
            int length = data[i] == 0 && data[i + 1] == 0 ? data[i + 2] == 1 ? 3 : data[i + 2] == 0 && data[i + 3] == 1 ? 4 : 0 : 0;
            if (length == 0) continue;
            if (start >= 0 && i > start) result.Add(data[start..i]);
            start = i + length;
            i += length - 1;
        }
        if (start >= 0 && start < data.Length) result.Add(data[start..]);
        return result;
    }
}
