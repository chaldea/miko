using System.Reflection;

namespace Miko.DocGen;

/// <summary>
/// The built Miko.Razor.Compiler, whose element table the elements reference is generated from.
/// Its directory is stamped into this assembly at build time (see Miko.DocGen.csproj) so the
/// tool and the tests that run it in-process read the compiler of the same configuration.
/// </summary>
internal static class CompilerLocation
{
    public static string Path
    {
        get
        {
            var directory = typeof(CompilerLocation).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "MikoRazorCompilerDir")?.Value
                ?? throw new InvalidOperationException("MikoRazorCompilerDir metadata is missing.");
            return System.IO.Path.Combine(directory, "Microsoft.CodeAnalysis.Razor.Compiler.dll");
        }
    }
}
