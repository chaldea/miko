using System.Text;
using Miko.Native.Filesystem;
using Miko.Windowing.Native;
using Shouldly;
using Directory = Miko.Native.Filesystem.Directory;
using Encoding = Miko.Native.Filesystem.Encoding;
using FileInfo = Miko.Native.Filesystem.FileInfo;

namespace Miko.Native.Tests.Desktop;

/// <summary>
/// 桌面文件系统实现的真实读写行为。这是桌面上能力最完整的 Native 服务，
/// 在本机可以端到端验证（Android/iOS 的对应实现只保证编译）。
/// </summary>
public sealed class DesktopFilesystemServiceTests : IDisposable
{
    private readonly DesktopFilesystemService _fs = new();
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "miko-native-fs-tests", Guid.NewGuid().ToString("n"));

    public DesktopFilesystemServiceTests() => System.IO.Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_root)) System.IO.Directory.Delete(_root, recursive: true);
    }

    private string PathIn(string name) => Path.Combine(_root, name);

    [Fact]
    public async Task Should_round_trip_text_with_encoding()
    {
        var path = PathIn("hello.txt");

        await _fs.WriteFileAsync(new WriteFileOptions
        {
            Path = path,
            DataBase64 = "你好 Miko",
            Encoding = Encoding.Utf8,
        });

        var read = await _fs.ReadFileAsync(new ReadFileOptions { Path = path, Encoding = Encoding.Utf8 });

        read.DataBase64.ShouldBe("你好 Miko");
    }

    [Fact]
    public async Task Should_round_trip_binary_as_base64_when_no_encoding()
    {
        var path = PathIn("blob.bin");
        var payload = new byte[] { 0x00, 0x01, 0xFE, 0xFF };

        await _fs.WriteFileAsync(new WriteFileOptions
        {
            Path = path,
            DataBase64 = Convert.ToBase64String(payload),
        });

        var read = await _fs.ReadFileAsync(new ReadFileOptions { Path = path });

        // 未指定编码时读写都走 Base64——二进制内容必须原样往返，不能被当成文本转码。
        Convert.FromBase64String(read.DataBase64!).ShouldBe(payload);
    }

    [Fact]
    public async Task Should_honour_offset_and_length_when_reading()
    {
        var path = PathIn("slice.txt");
        await File.WriteAllTextAsync(path, "0123456789");

        var read = await _fs.ReadFileAsync(new ReadFileOptions
        {
            Path = path,
            Encoding = Encoding.Utf8,
            Offset = 3,
            Length = 4,
        });

        read.DataBase64.ShouldBe("3456");
    }

    [Fact]
    public async Task Should_append_to_an_existing_file()
    {
        var path = PathIn("log.txt");

        await _fs.WriteFileAsync(new WriteFileOptions { Path = path, DataBase64 = "a", Encoding = Encoding.Utf8 });
        await _fs.AppendFileAsync(new AppendFileOptions { Path = path, DataBase64 = "b", Encoding = Encoding.Utf8 });

        (await File.ReadAllTextAsync(path)).ShouldBe("ab");
    }

    [Fact]
    public async Task Should_write_from_a_stream()
    {
        var path = PathIn("stream.txt");
        using var source = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("streamed"));

        await _fs.WriteFileAsync(new WriteFileOptions { Path = path, Data = source });

        (await File.ReadAllTextAsync(path)).ShouldBe("streamed");
    }

    [Fact]
    public async Task Should_create_parent_directories_only_when_recursive()
    {
        var nested = Path.Combine(_root, "a", "b", "c.txt");

        await _fs.WriteFileAsync(new WriteFileOptions
        {
            Path = nested,
            DataBase64 = "x",
            Encoding = Encoding.Utf8,
            Recursive = true,
        });

        File.Exists(nested).ShouldBeTrue();

        // Recursive=false 时缺失的父目录不应被悄悄创建。
        var other = Path.Combine(_root, "p", "q", "d.txt");
        await Should.ThrowAsync<DirectoryNotFoundException>(() => _fs.WriteFileAsync(new WriteFileOptions
        {
            Path = other,
            DataBase64 = "x",
            Encoding = Encoding.Utf8,
        }));
    }

    [Fact]
    public async Task Mkdir_without_recursive_should_fail_when_parent_is_missing()
    {
        // System.IO.Directory.CreateDirectory 总是递归建目录，所以 Recursive=false
        // 必须由实现自己挡住——否则这个开关毫无意义。
        await Should.ThrowAsync<DirectoryNotFoundException>(() => _fs.MkdirAsync(new MkdirOptions
        {
            Path = Path.Combine(_root, "missing", "child"),
        }));
    }

    [Fact]
    public async Task Mkdir_with_recursive_should_create_the_whole_chain()
    {
        var path = Path.Combine(_root, "x", "y", "z");

        await _fs.MkdirAsync(new MkdirOptions { Path = path, Recursive = true });

        System.IO.Directory.Exists(path).ShouldBeTrue();
    }

    [Fact]
    public async Task Rmdir_should_respect_recursive_flag()
    {
        var dir = PathIn("full");
        System.IO.Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "f.txt"), "x");

        await Should.ThrowAsync<IOException>(() => _fs.RmdirAsync(new RmdirOptions { Path = dir }));

        await _fs.RmdirAsync(new RmdirOptions { Path = dir, Recursive = true });
        System.IO.Directory.Exists(dir).ShouldBeFalse();
    }

    [Fact]
    public async Task ReadDirectory_should_list_files_and_directories()
    {
        await File.WriteAllTextAsync(PathIn("one.txt"), "1");
        System.IO.Directory.CreateDirectory(PathIn("sub"));

        var result = await _fs.ReadDirectoryAsync(new ReadDirectoryOptions { Path = _root });

        result.Files.Select(f => f.Name).OrderBy(n => n).ShouldBe(["one.txt", "sub"]);
        result.Files.Single(f => f.Name == "sub").Type.ShouldBe(FileEntryType.Directory);
        result.Files.Single(f => f.Name == "one.txt").Type.ShouldBe(FileEntryType.File);
    }

    [Fact]
    public async Task Stat_should_report_size_and_type()
    {
        var path = PathIn("sized.txt");
        await File.WriteAllTextAsync(path, "12345");

        FileInfo info = await _fs.StatAsync(new StatOptions { Path = path });

        info.Name.ShouldBe("sized.txt");
        info.Type.ShouldBe(FileEntryType.File);
        info.Size.ShouldBe(5);
        info.Uri.ShouldStartWith("file:///");
    }

    [Fact]
    public async Task Stat_on_a_missing_file_should_throw()
    {
        await Should.ThrowAsync<FileNotFoundException>(
            () => _fs.StatAsync(new StatOptions { Path = PathIn("nope.txt") }));
    }

    [Fact]
    public async Task Should_delete_a_file()
    {
        var path = PathIn("gone.txt");
        await File.WriteAllTextAsync(path, "x");

        await _fs.DeleteFileAsync(new DeleteFileOptions { Path = path });

        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_rename_a_file()
    {
        var from = PathIn("before.txt");
        var to = PathIn("after.txt");
        await File.WriteAllTextAsync(from, "x");

        await _fs.RenameAsync(new RenameOptions { From = from, To = to });

        File.Exists(from).ShouldBeFalse();
        File.Exists(to).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_copy_a_directory_recursively()
    {
        var from = PathIn("src");
        System.IO.Directory.CreateDirectory(Path.Combine(from, "inner"));
        await File.WriteAllTextAsync(Path.Combine(from, "inner", "deep.txt"), "deep");

        var result = await _fs.CopyAsync(new CopyOptions { From = from, To = PathIn("dst") });

        File.ReadAllText(Path.Combine(PathIn("dst"), "inner", "deep.txt")).ShouldBe("deep");
        result.Uri.ShouldStartWith("file:///");
    }

    [Fact]
    public async Task ReadFileInChunks_should_deliver_every_byte_then_signal_completion()
    {
        var path = PathIn("chunks.bin");
        var payload = System.Text.Encoding.UTF8.GetBytes("abcdefghij");
        await File.WriteAllBytesAsync(path, payload);

        var chunks = new List<string>();
        var completed = false;
        Exception? failure = null;

        await _fs.ReadFileInChunksAsync(
            new ReadFileInChunksOptions { Path = path, Encoding = Encoding.Utf8, ChunkSize = 4 },
            (chunk, error) =>
            {
                if (error is not null) failure = error;
                else if (chunk is null) completed = true;
                else chunks.Add(chunk.DataBase64!);
            });

        failure.ShouldBeNull();
        // 分块必须无缝拼回原文，且以一次 (null, null) 收尾表示结束。
        string.Concat(chunks).ShouldBe("abcdefghij");
        chunks.ShouldBe(["abcd", "efgh", "ij"]);
        completed.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadFileInChunks_should_report_errors_through_the_callback()
    {
        Exception? failure = null;

        await _fs.ReadFileInChunksAsync(
            new ReadFileInChunksOptions { Path = PathIn("missing.bin"), ChunkSize = 4 },
            (_, error) => failure ??= error);

        failure.ShouldBeOfType<FileNotFoundException>();
    }

    [Fact]
    public async Task GetUri_should_return_an_absolute_file_uri()
    {
        var result = await _fs.GetUriAsync(new GetUriOptions { Path = PathIn("any.txt") });

        result.Uri.ShouldStartWith("file:///");
        result.Uri.ShouldEndWith("any.txt");
    }

    [Fact]
    public async Task Permissions_should_be_granted_on_desktop()
    {
        (await _fs.CheckPermissionsAsync()).PublicStorage.ShouldBe(PermissionState.Granted);
        (await _fs.RequestPermissionsAsync()).PublicStorage.ShouldBe(PermissionState.Granted);
    }

    [Theory]
    [InlineData(Directory.Data)]
    [InlineData(Directory.Cache)]
    [InlineData(Directory.Documents)]
    [InlineData(Directory.Temporary)]
    [InlineData(Directory.Library)]
    [InlineData(Directory.External)]
    public async Task Should_write_and_read_within_each_scoped_directory(Directory directory)
    {
        var name = $"miko-scope-{Guid.NewGuid():n}.txt";

        await _fs.WriteFileAsync(new WriteFileOptions
        {
            Path = name,
            Directory = directory,
            DataBase64 = "scoped",
            Encoding = Encoding.Utf8,
        });

        try
        {
            var read = await _fs.ReadFileAsync(new ReadFileOptions
            {
                Path = name,
                Directory = directory,
                Encoding = Encoding.Utf8,
            });

            read.DataBase64.ShouldBe("scoped");
        }
        finally
        {
            await _fs.DeleteFileAsync(new DeleteFileOptions { Path = name, Directory = directory });
        }
    }

    [Fact]
    public void Relative_paths_without_a_directory_resolve_against_BaseDirectory()
    {
        // 与 Miko 资源加载的既有约定一致：进程 CWD 在 IDE 启动时与输出目录不同。
        var resolved = DesktopFilesystemService.Resolve("rel.txt", directory: null);

        resolved.ShouldBe(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "rel.txt")));
    }

    [Fact]
    public void File_uri_paths_resolve_to_a_local_path()
    {
        var path = PathIn("uri.txt");
        var uri = new Uri(path).AbsoluteUri;

        DesktopFilesystemService.Resolve(uri, directory: null).ShouldBe(path);
    }
}
