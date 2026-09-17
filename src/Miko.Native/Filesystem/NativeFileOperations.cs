using System.Text;

namespace Miko.Native.Filesystem;

/// <summary>
/// <see cref="IFilesystemService"/> 的共享实现：全部读写逻辑都建立在 <c>System.IO</c> 上。
/// <para>
/// 桌面、Android、iOS 三端的文件操作**完全相同**——都是 .NET 的 <c>System.IO</c>。
/// 三端唯一的差异是**限定目录如何映射到实际路径**（桌面用 <c>Environment.SpecialFolder</c>、
/// Android 用 <c>Context.FilesDir</c>、iOS 用 <c>NSSearchPath</c>）。因此各平台只需提供一个
/// 路径解析委托，其余逻辑在这里共享，而不是抄三遍。
/// </para>
/// </summary>
/// <param name="resolve">把「路径 + 限定目录」解析为绝对路径的平台委托。</param>
public sealed class NativeFileOperations(Func<string, Directory?, string> resolve)
{
    private readonly Func<string, Directory?, string> _resolve = resolve;
    private HttpClient? _httpClient;

    public async Task<ReadFileResult> ReadFileAsync(ReadFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = _resolve(options.Path, options.Directory);

        // Offset/Length 让调用方可以只读文件的一段，因此统一走字节再按需转码
        // （File.ReadAllText 无法表达偏移）。
        var bytes = await ReadBytesAsync(path, options.Offset, options.Length);

        return options.Encoding is { } encoding
            ? new ReadFileResult(ToEncoding(encoding).GetString(bytes))
            : new ReadFileResult(Convert.ToBase64String(bytes));
    }

    public async Task<string> ReadFileInChunksAsync(
        ReadFileInChunksOptions options,
        Action<ReadFileResult?, Exception?> callback)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(callback);

        var path = _resolve(options.Path, options.Directory);
        var chunkSize = options.ChunkSize > 0 ? (int)Math.Min(options.ChunkSize, int.MaxValue) : 64 * 1024;
        var readId = Guid.NewGuid().ToString("n");

        try
        {
            await using var stream = File.OpenRead(path);
            var buffer = new byte[chunkSize];

            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, chunkSize));
                if (read == 0) break;

                var chunk = buffer.AsSpan(0, read).ToArray();
                callback(
                    options.Encoding is { } encoding
                        ? new ReadFileResult(ToEncoding(encoding).GetString(chunk))
                        : new ReadFileResult(Convert.ToBase64String(chunk)),
                    null);
            }

            // 两者皆 null 表示读取正常结束（见 IFilesystemService 的接口注释）。
            callback(null, null);
        }
        catch (Exception ex)
        {
            callback(null, ex);
        }

        return readId;
    }

    public async Task<WriteFileResult> WriteFileAsync(WriteFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = _resolve(options.Path, options.Directory);

        if (options.Recursive)
        {
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) System.IO.Directory.CreateDirectory(parent);
        }

        await using (var target = File.Create(path))
        {
            await WritePayloadAsync(target, options.Data, options.DataBase64, options.Encoding);
        }

        return new WriteFileResult(ToUri(path));
    }

    public async Task AppendFileAsync(AppendFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = _resolve(options.Path, options.Directory);

        await using var target = new FileStream(path, FileMode.Append, FileAccess.Write);
        await WritePayloadAsync(target, options.Data, options.DataBase64, options.Encoding);
    }

    public Task DeleteFileAsync(DeleteFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        File.Delete(_resolve(options.Path, options.Directory));
        return Task.CompletedTask;
    }

    public Task MkdirAsync(MkdirOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = _resolve(options.Path, options.Directory);

        if (!options.Recursive)
        {
            // 非递归语义：父目录必须已存在。CreateDirectory 本身总是递归创建，
            // 因此缺父目录时要主动失败，否则 Recursive 这个开关形同虚设。
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && !System.IO.Directory.Exists(parent))
                throw new DirectoryNotFoundException($"Parent directory does not exist: {parent}");
        }

        System.IO.Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public Task RmdirAsync(RmdirOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        System.IO.Directory.Delete(_resolve(options.Path, options.Directory), options.Recursive);
        return Task.CompletedTask;
    }

    public Task<ReadDirectoryResult> ReadDirectoryAsync(ReadDirectoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = _resolve(options.Path, options.Directory);
        var entries = new List<FileInfo>();

        foreach (var entry in System.IO.Directory.EnumerateFileSystemEntries(path))
            entries.Add(Describe(entry));

        return Task.FromResult(new ReadDirectoryResult(entries));
    }

    public Task<GetUriResult> GetUriAsync(GetUriOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Task.FromResult(new GetUriResult(ToUri(_resolve(options.Path, options.Directory))));
    }

    public Task<FileInfo> StatAsync(StatOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = _resolve(options.Path, options.Directory);

        if (!File.Exists(path) && !System.IO.Directory.Exists(path))
            throw new FileNotFoundException($"File does not exist: {path}", path);

        return Task.FromResult(Describe(path));
    }

    public Task RenameAsync(RenameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var from = _resolve(options.From, options.Directory);
        var to = _resolve(options.To, options.ToDirectory ?? options.Directory);

        if (System.IO.Directory.Exists(from)) System.IO.Directory.Move(from, to);
        else File.Move(from, to, overwrite: true);

        return Task.CompletedTask;
    }

    public Task<CopyResult> CopyAsync(CopyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var from = _resolve(options.From, options.Directory);
        var to = _resolve(options.To, options.ToDirectory ?? options.Directory);

        if (System.IO.Directory.Exists(from)) CopyDirectory(from, to);
        else File.Copy(from, to, overwrite: true);

        return Task.FromResult(new CopyResult(ToUri(to)));
    }

    /// <summary>下载文件；<paramref name="onProgress"/> 在 <c>Progress</c> 开启时按块回调。</summary>
    public async Task<DownloadFileResult> DownloadFileAsync(
        DownloadFileOptions options,
        Action<ProgressStatus>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var path = _resolve(options.Path, options.Directory);

        if (options.Recursive)
        {
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) System.IO.Directory.CreateDirectory(parent);
        }

        var client = _httpClient ??= new HttpClient();

        using var response = await client.GetAsync(options.Url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength ?? 0;
        var downloaded = 0L;

        await using (var source = await response.Content.ReadAsStreamAsync())
        await using (var target = File.Create(path))
        {
            var buffer = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(buffer)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read));
                downloaded += read;

                if (options.Progress) onProgress?.Invoke(new ProgressStatus(options.Url, downloaded, contentLength));
            }
        }

        return new DownloadFileResult(path);
    }

    private static async Task WritePayloadAsync(Stream target, Stream? data, string? dataBase64, Encoding? encoding)
    {
        if (data is not null)
        {
            await data.CopyToAsync(target);
            return;
        }

        if (dataBase64 is null) return;

        var bytes = encoding is { } enc
            ? ToEncoding(enc).GetBytes(dataBase64)
            : Convert.FromBase64String(dataBase64);

        await target.WriteAsync(bytes);
    }

    private static async Task<byte[]> ReadBytesAsync(string path, long offset, long length)
    {
        await using var stream = File.OpenRead(path);

        if (offset > 0) stream.Seek(offset, SeekOrigin.Begin);

        var remaining = length > 0
            ? Math.Min(length, stream.Length - offset)
            : stream.Length - offset;

        if (remaining <= 0) return [];

        var buffer = new byte[remaining];
        await stream.ReadExactlyAsync(buffer);
        return buffer;
    }

    private static void CopyDirectory(string from, string to)
    {
        System.IO.Directory.CreateDirectory(to);

        foreach (var file in System.IO.Directory.EnumerateFiles(from))
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);

        foreach (var dir in System.IO.Directory.EnumerateDirectories(from))
            CopyDirectory(dir, Path.Combine(to, Path.GetFileName(dir)));
    }

    private static FileInfo Describe(string path)
    {
        var isDirectory = System.IO.Directory.Exists(path);
        FileSystemInfo info = isDirectory ? new DirectoryInfo(path) : new System.IO.FileInfo(path);

        return new FileInfo(
            Name: info.Name,
            Type: isDirectory ? FileEntryType.Directory : FileEntryType.File,
            Size: isDirectory ? 0 : ((System.IO.FileInfo)info).Length,
            CreationTime: new DateTimeOffset(info.CreationTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            ModifiedTime: new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            Uri: ToUri(path));
    }

    private static string ToUri(string path) => new Uri(Path.GetFullPath(path)).AbsoluteUri;

    private static System.Text.Encoding ToEncoding(Encoding encoding) => encoding switch
    {
        Encoding.Utf8 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Encoding.Ascii => System.Text.Encoding.ASCII,
        Encoding.Utf16 => System.Text.Encoding.Unicode,
        _ => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
    };
}
