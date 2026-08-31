using Miko.Platform.Video;
using Shouldly;

namespace Miko.Tests.Platform.Video;

/// <summary>
/// 视频源路径归一化。系统解码器把相对路径解析到**进程 CWD**，而 Miko 的资源约定是
/// <see cref="AppContext.BaseDirectory"/>（同 <c>ResourceManager.ResolveFilePath</c>）。
/// 不归一化就会出现「同目录下 &lt;img&gt; 能加载、&lt;video&gt; 报找不到文件」，
/// 且只在从非输出目录启动时复现 —— 故在此固定该行为。
/// </summary>
public class VideoSourceDescriptorTests
{
    [Theory]
    [InlineData("http://example.com/a.mp4")]
    [InlineData("https://example.com/a.mp4")]
    [InlineData("HTTP://example.com/a.mp4")]
    public void ResolveForBackend_NetworkSource_ReturnedUnchanged(string uri)
        => new VideoSourceDescriptor(uri).ResolveForBackend().ShouldBe(uri);

    [Theory]
    [InlineData("http://example.com/a.mp4", true)]
    [InlineData("https://example.com/a.mp4", true)]
    [InlineData("movie.mp4", false)]
    [InlineData("C:/videos/movie.mp4", false)]
    public void IsNetwork_ClassifiesScheme(string uri, bool expected)
        => new VideoSourceDescriptor(uri).IsNetwork.ShouldBe(expected);

    [Fact]
    public void ResolveForBackend_ExistingRelativePath_ResolvesAgainstBaseDirectory()
    {
        // 在 BaseDirectory 下放一个真实文件，断言相对路径被解析为它的绝对路径。
        string fileName = $"miko-video-test-{Guid.NewGuid():N}.mp4";
        string fullPath = Path.Combine(AppContext.BaseDirectory, fileName);
        File.WriteAllBytes(fullPath, [0x00]);

        try
        {
            var resolved = new VideoSourceDescriptor(fileName).ResolveForBackend();

            resolved.ShouldBe(fullPath);
            Path.IsPathRooted(resolved).ShouldBeTrue();
        }
        finally
        {
            File.Delete(fullPath);
        }
    }

    [Fact]
    public void ResolveForBackend_RelativePathInSubdirectory_ResolvesAgainstBaseDirectory()
    {
        // 镜像示例的 "Assets/miko-local.mp4" 写法。
        string dir = Path.Combine(AppContext.BaseDirectory, $"miko-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string fileName = Path.GetFileName(dir) + "/clip.mp4";
        string fullPath = Path.Combine(dir, "clip.mp4");
        File.WriteAllBytes(fullPath, [0x00]);

        try
        {
            var resolved = new VideoSourceDescriptor(fileName).ResolveForBackend();

            Path.IsPathRooted(resolved).ShouldBeTrue();
            File.Exists(resolved).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ResolveForBackend_MissingRelativePath_ReturnsOriginalForBackendError()
    {
        // 不存在时保持原串，让后端报出自己的错误（而不是伪造一个不存在的绝对路径）。
        const string missing = "no-such-video-file.mp4";

        new VideoSourceDescriptor(missing).ResolveForBackend().ShouldBe(missing);
    }

    [Fact]
    public void ResolveForBackend_AbsolutePath_ReturnedUnchanged()
    {
        string absolute = Path.Combine(AppContext.BaseDirectory, "already-absolute.mp4");

        new VideoSourceDescriptor(absolute).ResolveForBackend().ShouldBe(absolute);
    }

    [Fact]
    public void ResolveForBackend_FileScheme_StripsPrefix()
    {
        var resolved = new VideoSourceDescriptor("file:///C:/videos/a.mp4").ResolveForBackend();

        // 三斜杠形式的盘符路径应去掉前导 '/'，得到可直接打开的本地路径。
        resolved.ShouldBe("C:/videos/a.mp4");
    }

    [Fact]
    public void ResolveForBackend_UnknownScheme_LeftForBackend()
    {
        // 后端可能支持自定义 scheme（rtsp/rtmp 等），不应被当成本地路径处理。
        const string rtsp = "rtsp://camera.local/stream";

        new VideoSourceDescriptor(rtsp).ResolveForBackend().ShouldBe(rtsp);
    }
}
