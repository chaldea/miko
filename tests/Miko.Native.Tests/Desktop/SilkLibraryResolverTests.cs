using System.Runtime.InteropServices;
using Miko.Windowing.Common;
using Shouldly;
using Silk.NET.Core.Loader;

namespace Miko.Native.Tests.Desktop;

/// <summary>
/// ISSUE-162 Linux 验证：Silk.NET 自己从 <c>/etc/os-release</c> 推出 RID（<c>ubuntu.26.04-x64</c>），
/// 而冻结在 .NET 8 的 RID 图止于 Ubuntu 23.10，Ubuntu 24.04+ 上永远回退不到 <c>linux-x64</c>，
/// 随应用发布的 libglfw 一次都没被尝试，首个窗口直接失败。那条回退失败只在这些发行版上复现；
/// 这里锁定补上的解析器：它给出 .NET 宿主按 deps.json 为当前 RID 选中的那份 GLFW。
/// </summary>
public class SilkLibraryResolverTests
{
    // Silk.NET.GLFW 按平台加载的库名（GlfwLibraryNameContainer）。
    private static string GlfwLibraryName =>
        OperatingSystem.IsWindows() ? "glfw3.dll"
        : OperatingSystem.IsMacOS() ? "libglfw.3.dylib"
        : "libglfw.so.3";

    // Ultz.Native.GLFW 随 Miko.Windowing 进入输出目录的那份。
    private static string BundledGlfwPath => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native", GlfwLibraryName));

    [Fact]
    public void Should_resolve_the_glfw_shipped_for_the_current_rid()
    {
        File.Exists(BundledGlfwPath).ShouldBeTrue();

        SilkLibraryResolver.ResolveFromNativeSearchDirectories(GlfwLibraryName)
            .Select(Path.GetFullPath)
            .ShouldContain(BundledGlfwPath);
    }

    [Fact]
    public void Should_resolve_nothing_for_a_library_that_is_not_shipped()
    {
        SilkLibraryResolver.ResolveFromNativeSearchDirectories("libmiko-not-shipped.so").ShouldBeEmpty();
    }

    [Fact]
    public void Should_join_silk_default_path_resolver_exactly_once()
    {
        SilkLibraryResolver.Install();
        SilkLibraryResolver.Install();

        var resolver = PathResolver.Default.ShouldBeOfType<DefaultPathResolver>();
        Func<string, IEnumerable<string>> ours = SilkLibraryResolver.ResolveFromNativeSearchDirectories;
        resolver.Resolvers.Count(r => r.Equals(ours)).ShouldBe(1);
        resolver.EnumeratePossibleLibraryLoadTargets(GlfwLibraryName)
            .Select(p => Path.IsPathRooted(p) ? Path.GetFullPath(p) : p)
            .ShouldContain(BundledGlfwPath);
    }
}
