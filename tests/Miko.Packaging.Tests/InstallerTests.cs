using System.Buffers.Binary;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using OpenMcdf;
using Shouldly;
using Xunit;

namespace Miko.Packaging.Tests;

public sealed class InstallerTests : IDisposable
{
    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("Miko packaging ");
    private readonly DirectoryInfo publish;
    private const string AssetName = "assets/hello world.txt";
    private readonly byte[] asset = Encoding.UTF8.GetBytes("nested file with spaces");

    public InstallerTests()
    {
        publish = root.CreateSubdirectory("publish");
        publish.CreateSubdirectory("assets");
        File.WriteAllBytes(Path.Combine(publish.FullName, AssetName), asset);
        File.WriteAllText(Path.Combine(publish.FullName, "Demo"), "app");
    }

    public void Dispose() => root.Delete(true);
    private DesktopPackage Package(string rid) => new(publish.FullName, "Demo", "1.2.3", rid, "Test", "Desktop demo")
        { DebDepends = "libc6 (>= 2.31)", RpmRequires = ["glibc >= 2.31"] };

    [Fact]
    public void Should_preserve_previous_packages_when_a_format_fails()
    {
        var package = Package("win-x64");
        string output = package.Build(root.FullName, "zip").Single();
        var original = File.ReadAllBytes(output);
        if (!OperatingSystem.IsWindows())
        {
            File.CreateSymbolicLink(Path.Combine(publish.FullName, "link"), "Demo");
            Should.Throw<InvalidDataException>(() => Package("win-x64").Build(root.FullName, "zip;msi"));
            File.ReadAllBytes(output).ShouldBe(original);
        }
        Should.Throw<ArgumentException>(() => package.Build(publish.FullName, "zip"));
        Directory.GetDirectories(root.FullName, ".miko-*").ShouldBeEmpty();
    }

    [Fact]
    public void Should_embed_a_cabinet_and_install_tables_in_the_msi()
    {
        var paths = Package("win-x64").Build(root.FullName, "zip;msi");
        using var zip = ZipFile.OpenRead(paths[0]);
        using (var contents = zip.GetEntry(AssetName)!.Open())
            new StreamReader(contents).ReadToEnd().ShouldBe(Encoding.UTF8.GetString(asset));
        using var storage = RootStorage.OpenRead(paths[1]);
        storage.CLSID.ShouldBe(Guid.Parse("000c1084-0000-0000-c000-000000000046"));
        using var table = storage.OpenStream(MsiDatabase.StreamName("InstallExecuteSequence", true));
        table.Length.ShouldBeGreaterThan(0);
        using var cabinet = storage.OpenStream(MsiDatabase.StreamName("payload.cab", false));
        using var reader = new BinaryReader(cabinet);
        Encoding.ASCII.GetString(reader.ReadBytes(4)).ShouldBe("MSCF");
        cabinet.Position = 36;
        var blockOffset = reader.ReadUInt32();
        cabinet.Position = blockOffset + 4;
        var stored = reader.ReadUInt16();
        reader.ReadUInt16().ShouldBe(stored);
        reader.ReadBytes(stored).ShouldBe([.. "app"u8.ToArray(), .. asset]);
    }

    [Fact]
    public void Should_include_control_and_data_in_the_debian_package()
    {
        using var file = File.OpenRead(Package("linux-x64").Build(root.FullName, "deb").Single());
        byte[] Read(int n) { var bytes = new byte[n]; file.ReadExactly(bytes); return bytes; }
        Encoding.ASCII.GetString(Read(8)).ShouldBe("!<arch>\n");
        var members = new Dictionary<string, byte[]>();
        while (file.Position < file.Length)
        {
            string header = Encoding.ASCII.GetString(Read(60));
            members.Add(header[..16].Trim(), Read(int.Parse(header[48..58])));
            if ((file.Position & 1) != 0) file.ReadByte().ShouldBe((int)'\n');
        }
        Encoding.ASCII.GetString(members["debian-binary"]).ShouldBe("2.0\n");
        string ReadMember(string archive, string name)
        {
            using var gz = new GZipStream(new MemoryStream(members[archive]), CompressionMode.Decompress);
            using var tar = new TarReader(gz);
            TarEntry? entry;
            while ((entry = tar.GetNextEntry()) != null)
            {
                entry.Format.ShouldBe(TarEntryFormat.Gnu);
                if (entry.Name == name) return new StreamReader(entry.DataStream!).ReadToEnd();
            }
            throw new InvalidDataException(name);
        }
        ReadMember("control.tar.gz", "./control").ShouldContain("Depends: libc6 (>= 2.31)");
        ReadMember("data.tar.gz", "./opt/demo/" + AssetName).ShouldBe(Encoding.UTF8.GetString(asset));
    }

    [Fact]
    public void Should_include_the_rpm_file_list_and_matching_cpio_payload()
    {
        byte[] rpm = File.ReadAllBytes(Package("linux-arm64").Build(root.FullName, "rpm").Single());
        int HeaderEnd(int offset) => offset + 16 + 16 * BinaryPrimitives.ReadInt32BigEndian(rpm.AsSpan(offset + 8)) +
            BinaryPrimitives.ReadInt32BigEndian(rpm.AsSpan(offset + 12));
        var header = (HeaderEnd(96) + 7) & ~7;
        var payload = HeaderEnd(header);
        var metadata = Encoding.UTF8.GetString(rpm, header, payload - header);
        metadata.ShouldContain("aarch64\0");
        metadata.ShouldContain("/opt/demo/assets/\0");
        metadata.ShouldContain("glibc\0");
        using var gz = new GZipStream(new MemoryStream(rpm, payload, rpm.Length - payload), CompressionMode.Decompress);
        using var contents = new MemoryStream();
        gz.CopyTo(contents);
        string cpio = Encoding.UTF8.GetString(contents.ToArray());
        cpio.ShouldContain("./opt/demo/" + AssetName + "\0");
        cpio.ShouldContain(Encoding.UTF8.GetString(asset));
    }

    [Fact]
    public void Should_wrap_a_flat_installer_in_the_disk_image()
    {
        var paths = Package("osx-arm64").Build(root.FullName, "pkg;dmg");
        byte[] pkg = File.ReadAllBytes(paths[0]);
        pkg[..4].ShouldBe("xar!"u8.ToArray());
        byte[] dmg = File.ReadAllBytes(paths[1]);
        dmg[^512..^508].ShouldBe("koly"u8.ToArray());
        long toc = BinaryPrimitives.ReadInt64BigEndian(dmg.AsSpan(dmg.Length - 512 + 216));
        toc.ShouldBeGreaterThan(pkg.Length);
        dmg.AsSpan(16 * 2048 + 1, 5).ToArray().ShouldBe("CD001"u8.ToArray());
        dmg.AsSpan(0, (int)toc).IndexOf(pkg).ShouldBeGreaterThan(0);
        if (OperatingSystem.IsMacOS())
        {
            Run(root.FullName, "hdiutil", "verify", paths[1]);
            Run(root.FullName, "pkgutil", "--expand-full", paths[0], "expanded");
            string bundle = Path.Combine(root.FullName, "expanded/Payload/Demo.app/Contents");
            File.ReadAllBytes(Path.Combine(bundle, "MacOS", AssetName)).ShouldBe(asset);
            Run(root.FullName, "plutil", "-lint", Path.Combine(bundle, "Info.plist"));
            Run(root.FullName, "lsbom", "expanded/Bom").ShouldContain(AssetName);
        }
    }

    [Fact]
    public void Should_package_a_consumer_via_the_nuget_publish_target()
    {
        var repo = new DirectoryInfo(AppContext.BaseDirectory);
        while (repo != null && !File.Exists(Path.Combine(repo.FullName, "miko.slnx"))) repo = repo.Parent;
        var feed = root.CreateSubdirectory("feed");
        string configuration = typeof(DesktopPackage).Assembly.GetCustomAttributesData()
            .Single(a => a.AttributeType == typeof(System.Reflection.AssemblyConfigurationAttribute)).ConstructorArguments[0].Value!.ToString()!;
        Run(repo!.FullName, "dotnet", "pack", "src/Miko.Packaging", "--no-build", "-c", configuration,
            "-o", feed.FullName, "-p:PackageVersion=1.0.0-test");
        var app = root.CreateSubdirectory("test app");
        File.WriteAllText(Path.Combine(app.FullName, "Demo.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net11.0</TargetFramework><OutputType>Exe</OutputType><UseAppHost>false</UseAppHost>
                <RestorePackagesPath>../cache</RestorePackagesPath><NuGetAudit>false</NuGetAudit>
              </PropertyGroup>
              <ItemGroup><PackageReference Include="Miko.Packaging" Version="1.0.0-test" PrivateAssets="all" /></ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(app.FullName, "Program.cs"), "System.Console.WriteLine(123);");
        Run(app.FullName, "dotnet", "restore", "--source", feed.FullName);
        Run(app.FullName, "dotnet", "build", "--no-restore", "-c", "Release");
        var output = Path.Combine(app.FullName, "bin", "Release", "net11.0", "packages");
        Directory.Exists(output).ShouldBeFalse();
        Run(app.FullName, "dotnet", "publish", "--no-restore", "-c", "Release");
        var rid = RuntimeInformation.RuntimeIdentifier;
        Directory.GetFiles(output).Select(Path.GetFileName).ShouldBe([$"Demo-1.0.0-{rid}.{DesktopPackage.Formats(rid, "")[0]}"]);
        Directory.GetFiles(Path.Combine(app.FullName, "bin/Release/net11.0/publish"), "*Packaging*").ShouldBeEmpty();
    }

    private static string Run(string directory, string executable, params string[] args)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in args) info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, stdout.Result + stderr.Result);
        return stdout.Result;
    }
}
