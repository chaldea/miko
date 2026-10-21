using System.Xml.Linq;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-154 BUG-06: the NuGet package's <c>build/Miko.Razor.Compiler.props</c> must locate the
/// compiler by the compiler's own target framework. It used the consuming project's
/// <c>$(TargetFramework)</c>, so a platform project (<c>net10.0-android</c>, <c>net10.0-ios</c>) with
/// <c>.razor</c> files looked for <c>tools/net10.0-android/</c>, which the package does not contain.
/// </summary>
public class RazorCompilerPackagePropsTests
{
    private static string RepoFile(params string[] relative)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "miko.slnx")))
                return Path.Combine(new[] { dir.FullName }.Concat(relative).ToArray());
        }
        throw new InvalidOperationException("Repository root (miko.slnx) not found.");
    }

    private static string Property(XDocument document, string name)
        => document.Descendants(name).Single().Value.Trim();

    [Fact]
    public void Props_locate_the_tools_folder_by_the_compilers_own_framework()
    {
        var props = XDocument.Load(RepoFile("src", "Miko.Razor.Compiler", "build", "Miko.Razor.Compiler.props"));
        var project = XDocument.Load(RepoFile("src", "Miko.Razor.Compiler", "Miko.Razor.Compiler.csproj"));

        var generatorDir = Property(props, "_CustomRazorGeneratorDir");
        generatorDir.ShouldNotContain("$(TargetFramework)");
        generatorDir.ShouldContain("$(_MikoRazorCompilerToolFramework)");

        // The compiler is packed into tools/<its TargetFramework>/; the props must name that folder.
        Property(props, "_MikoRazorCompilerToolFramework").ShouldBe(Property(project, "TargetFramework"));
    }
}
