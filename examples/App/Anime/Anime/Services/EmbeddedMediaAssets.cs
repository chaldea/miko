// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Anime.Services;

public interface IMediaAssets
{
    string VideoPath { get; }
}

/// <summary>System decoders need a seekable file on desktop, Android and iOS.</summary>
public sealed class EmbeddedMediaAssets : IMediaAssets
{
    private readonly Lazy<string> _video = new(ExtractVideo);

    public string VideoPath => _video.Value;

    private static string ExtractVideo()
    {
        var directory = Path.Combine(Path.GetTempPath(), "miko-anime");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "sample-v1.mp4");
        using var source = typeof(App).Assembly.GetManifestResourceStream("Anime.Assets.sample.mp4")
            ?? throw new InvalidOperationException("The bundled sample video is missing.");
        if (!File.Exists(path) || new FileInfo(path).Length != source.Length)
        {
            using var destination = File.Create(path);
            source.CopyTo(destination);
        }
        return path;
    }
}
