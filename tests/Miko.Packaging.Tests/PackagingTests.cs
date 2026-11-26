using System.Formats.Tar;
using System.IO.Compression;
using Shouldly;
using Xunit;

namespace Miko.Packaging.Tests;

public class PackagingTests
{
    [Theory]
    [InlineData("win-x64", "msi")]
    [InlineData("linux-x64", "deb")]
    [InlineData("osx-x64", "dmg")]
    [InlineData("osx-arm64", "dmg")]
    public void Should_select_defaults_from_target_rid(string rid, string expected) =>
        DesktopPackage.Formats(rid, "").ShouldBe([expected]);

    [Fact]
    public void Should_validate_all_requested_formats()
    {
        DesktopPackage.Formats("linux-x64", "deb;RPM;tar.gz;deb").ShouldBe(["deb", "rpm", "tar.gz"]);
        Should.Throw<ArgumentException>(() => DesktopPackage.Formats("linux-x64", "deb;msi"));
        Should.Throw<ArgumentException>(() => DesktopPackage.Formats("android-arm64", ""));
    }

    [Fact]
    public void Should_archive_nested_publish_assets_and_executable_mode()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var publish = root.CreateSubdirectory("publish");
            File.WriteAllText(Path.Combine(publish.FullName, "Demo"), "app");
            File.WriteAllText(Path.Combine(publish.CreateSubdirectory("assets").FullName, "a b.txt"), "asset");
            var package = new DesktopPackage(publish.FullName, "Demo", "1.2.3", "linux-x64", "Author", "Description");
            var output = package.Build(root.FullName, "tar.gz").Single();
            using var gzip = new GZipStream(File.OpenRead(output), CompressionMode.Decompress);
            using var tar = new TarReader(gzip);
            var files = new Dictionary<string, string>();
            while (tar.GetNextEntry() is {} entry)
            {
                if (entry.DataStream is null) continue;
                if (entry.Name == "Demo") entry.Mode.HasFlag(UnixFileMode.UserExecute).ShouldBeTrue();
                files.Add(entry.Name, new StreamReader(entry.DataStream).ReadToEnd());
            }
            files["Demo"].ShouldBe("app");
            files["assets/a b.txt"].ShouldBe("asset");
        }
        finally { root.Delete(true); }
    }
}
