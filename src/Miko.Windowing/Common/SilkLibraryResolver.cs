using Silk.NET.Core.Loader;

namespace Miko.Windowing.Common;

/// <summary>
/// Lets Silk.NET load the native libraries an app ships under <c>runtimes/&lt;rid&gt;/native</c>
/// (GLFW, from Ultz.Native.GLFW) on every Linux distribution.
/// </summary>
/// <remarks>
/// Silk.NET does not ask the runtime for its RID: it derives one from <c>/etc/os-release</c>
/// (<c>ubuntu.26.04-x64</c>) and looks for fallbacks in the RID graph of the app's deps.json. That
/// graph was frozen in .NET 8 and ends at Ubuntu 23.10, and Silk.NET's own guess list has no
/// <c>ubuntu</c>, so on Ubuntu 24.04 and later it never reaches <c>linux-x64</c> and only tries the
/// system library paths: without the distro's <c>libglfw3</c> package the first window fails with
/// "Could not load from any of the possible library names" (Silk.NET 2.22 and 2.23 alike).
/// The .NET host has already resolved the RID-specific native folders from deps.json for
/// <c>[DllImport]</c> (that is how libSkiaSharp loads); this resolver probes the same folders.
/// It runs after Silk.NET's own resolvers, so wherever those succeed nothing changes.
/// </remarks>
public static class SilkLibraryResolver
{
    private static int _installed;

    /// <summary>
    /// Adds the resolver to Silk.NET's default path resolver. Hosts call it before the first window
    /// is created, since that loads GLFW. Safe to call repeatedly.
    /// </summary>
    public static void Install()
    {
        if (Interlocked.Exchange(ref _installed, 1) != 0) return;
        if (PathResolver.Default is DefaultPathResolver resolver)
            resolver.Resolvers.Add(ResolveFromNativeSearchDirectories);
    }

    internal static IEnumerable<string> ResolveFromNativeSearchDirectories(string name)
    {
        // Not set under Native AOT, which copies native libraries next to the executable, where
        // Silk.NET already looks.
        if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is not string directories) yield break;

        foreach (var directory in directories.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var path = Path.Combine(directory, name);
            if (File.Exists(path)) yield return path;
        }
    }
}
