using Foundation;
using Miko.Native;
using Miko.Native.Filesystem;
using Directory = Miko.Native.Filesystem.Directory;
using FileInfo = Miko.Native.Filesystem.FileInfo;
using IoDirectory = System.IO.Directory;
using IoPath = System.IO.Path;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 文件系统。
/// <para>
/// 读写逻辑复用 <see cref="NativeFileOperations"/>（三端共享的 <c>System.IO</c> 实现），
/// 本类只负责 iOS 特有的一件事：把限定目录映射到沙盒内的实际路径。
/// </para>
/// </summary>
#pragma warning disable CS0618 // DownloadFile/OnProgress 在接口上已标记弃用，实现仍需提供。
internal sealed class IosFilesystemService : NativeServiceBase, IFilesystemService
{
    private readonly NativeFileOperations _ops = new(Resolve);

    /// <summary>iOS 应用对自己的沙盒始终有读写权限，无需申请。</summary>
    public Task<FilePermissionStatus> CheckPermissionsAsync()
        => Task.FromResult(new FilePermissionStatus(PermissionState.Granted));

    public Task<FilePermissionStatus> RequestPermissionsAsync()
        => Task.FromResult(new FilePermissionStatus(PermissionState.Granted));

    public Task<ReadFileResult> ReadFileAsync(ReadFileOptions options) => _ops.ReadFileAsync(options);

    public Task<string> ReadFileInChunksAsync(
        ReadFileInChunksOptions options,
        Action<ReadFileResult?, Exception?> callback)
        => _ops.ReadFileInChunksAsync(options, callback);

    public Task<WriteFileResult> WriteFileAsync(WriteFileOptions options) => _ops.WriteFileAsync(options);
    public Task AppendFileAsync(AppendFileOptions options) => _ops.AppendFileAsync(options);
    public Task DeleteFileAsync(DeleteFileOptions options) => _ops.DeleteFileAsync(options);
    public Task MkdirAsync(MkdirOptions options) => _ops.MkdirAsync(options);
    public Task RmdirAsync(RmdirOptions options) => _ops.RmdirAsync(options);
    public Task<ReadDirectoryResult> ReadDirectoryAsync(ReadDirectoryOptions options) => _ops.ReadDirectoryAsync(options);
    public Task<GetUriResult> GetUriAsync(GetUriOptions options) => _ops.GetUriAsync(options);
    public Task<FileInfo> StatAsync(StatOptions options) => _ops.StatAsync(options);
    public Task RenameAsync(RenameOptions options) => _ops.RenameAsync(options);
    public Task<CopyResult> CopyAsync(CopyOptions options) => _ops.CopyAsync(options);

    public Task<DownloadFileResult> DownloadFileAsync(DownloadFileOptions options)
        => _ops.DownloadFileAsync(options, status => OnProgress?.Invoke(status));

    public event Action<ProgressStatus>? OnProgress;

    private static string Resolve(string path, Directory? directory)
    {
        if (directory is null)
        {
            if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                return new Uri(path).LocalPath;

            return IoPath.IsPathRooted(path)
                ? path
                : IoPath.GetFullPath(IoPath.Combine(AppContext.BaseDirectory, path));
        }

        return IoPath.GetFullPath(IoPath.Combine(RootFor(directory.Value), path));
    }

    /// <summary>把限定目录映射到 iOS 沙盒中的实际根目录。</summary>
    private static string RootFor(Directory directory)
    {
        var root = directory switch
        {
            Directory.Documents => SearchPath(NSSearchPathDirectory.DocumentDirectory),
            Directory.Library => SearchPath(NSSearchPathDirectory.LibraryDirectory),
            Directory.Cache or Directory.ExternalCache => SearchPath(NSSearchPathDirectory.CachesDirectory),
            // Application Support 不会被 iCloud 备份扫描，也不会像 Caches 那样被系统清理。
            Directory.Data => IoPath.Combine(
                SearchPath(NSSearchPathDirectory.ApplicationSupportDirectory), "Data"),
            Directory.LibraryNoCloud => IoPath.Combine(
                SearchPath(NSSearchPathDirectory.LibraryDirectory), "NoCloud"),
            Directory.Temporary => IoPath.GetTempPath(),
            // iOS 沙盒没有「外部存储」；映射到 Documents 下的同名子目录，
            // 使跨平台代码路径一致，而不是让文件 API 在 iOS 上半残。
            Directory.External or Directory.ExternalStorage => IoPath.Combine(
                SearchPath(NSSearchPathDirectory.DocumentDirectory), "External"),
            _ => SearchPath(NSSearchPathDirectory.DocumentDirectory),
        };

        IoDirectory.CreateDirectory(root);
        return root;
    }

    private static string SearchPath(NSSearchPathDirectory directory)
    {
        var paths = NSSearchPath.GetDirectories(directory, NSSearchPathDomain.User);

        return paths.Length > 0
            ? paths[0]
            : throw new DirectoryNotFoundException($"The iOS directory for {directory} is unavailable.");
    }
}
#pragma warning restore CS0618
