using Shouldly;

namespace Miko.Native.Tests.Desktop;

/// <summary>
/// A desktop app gets the renderer's native libraries for every OS from Miko.Windowing alone.
/// SkiaSharp and HarfBuzzSharp carry only their macOS and Win32 natives; the Linux ones are separate
/// packages, and a missing one only fails on Linux, where CI does not run: the Charts example died
/// with <c>DllNotFoundException: libHarfBuzzSharp</c> the first time it shaped a label. A portable
/// build copies every RID's natives into the output, so the check works on any OS.
/// This project deliberately declares no native package of its own.
/// </summary>
public class DesktopNativeAssetsTests
{
    [Theory]
    [InlineData("linux-x64", "libSkiaSharp.so")]
    [InlineData("linux-x64", "libHarfBuzzSharp.so")]
    [InlineData("linux-arm64", "libSkiaSharp.so")]
    [InlineData("linux-arm64", "libHarfBuzzSharp.so")]
    [InlineData("osx", "libSkiaSharp.dylib")]
    [InlineData("osx", "libHarfBuzzSharp.dylib")]
    [InlineData("win-x64", "libSkiaSharp.dll")]
    [InlineData("win-x64", "libHarfBuzzSharp.dll")]
    public void Should_ship_the_renderer_native_libraries_with_a_desktop_app(string rid, string library)
    {
        File.Exists(Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", library)).ShouldBeTrue();
    }
}
