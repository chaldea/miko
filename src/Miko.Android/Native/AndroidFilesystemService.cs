using Android.Content;
using Miko.Native;
using Miko.Native.Filesystem;
using Directory = Miko.Native.Filesystem.Directory;
using FileInfo = Miko.Native.Filesystem.FileInfo;
using IoDirectory = System.IO.Directory;
using IoPath = System.IO.Path;

namespace Miko.Android.Native;

/// <summary>
/// Android 文件系统。
/// <para>
/// 读写逻辑与桌面完全一致（都是 <c>System.IO</c>），差别只在**限定目录如何映射到实际路径**：
/// Android 的应用私有目录、外部存储、缓存目录都由 <see cref="Context"/> 给出，
/// 不能像桌面那样用 <c>Environment.SpecialFolder</c>。因此这里只重写路径解析，
/// 其余行为通过组合复用一个共享实现。
/// </para>
/// </summary>
internal sealed class AndroidFilesystemService : NativeServiceBase, IFilesystemService
{
    private readonly INativeHostContext _hostContext;
    private readonly NativeFileOperations _ops;

    public AndroidFilesystemService(INativeHostContext hostContext)
    {
        _hostContext = hostContext;
        _ops = new NativeFileOperations(Resolve);
    }

    private Context Context => _hostContext.RequireHost<AndroidNativeHost>().Context;

    /// <summary>
    /// Android 上应用对自己的私有目录始终有读写权限，无需申请。
    /// 只有访问共享媒体/外部存储才需要运行时权限，而那属于 <c>MediaStore</c> 范畴。
    /// </summary>
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

    [Obsolete("Deprecated since Capacitor v7.1. Use a dedicated file-transfer implementation instead.")]
    public Task<DownloadFileResult> DownloadFileAsync(DownloadFileOptions options)
        => _ops.DownloadFileAsync(options, status => OnProgress?.Invoke(status));

    [Obsolete("Deprecated since Capacitor v7.1, together with DownloadFileAsync.")]
    public event Action<ProgressStatus>? OnProgress;

    /// <summary>把限定目录 + 相对路径解析为 Android 上的绝对路径。</summary>
    private string Resolve(string path, Directory? directory)
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

    private string RootFor(Directory directory)
    {
        var context = Context;

        var root = directory switch
        {
            // FilesDir 是应用私有内部存储，对应 Capacitor 的 Data/Documents/Library。
            Directory.Data or Directory.Documents or Directory.Library or Directory.LibraryNoCloud
                => context.FilesDir?.AbsolutePath,
            Directory.Cache => context.CacheDir?.AbsolutePath,
            Directory.Temporary => context.CacheDir?.AbsolutePath,
            // GetExternalFilesDir(null)：外部存储上的应用私有目录，无需运行时权限。
            Directory.External or Directory.ExternalStorage => context.GetExternalFilesDir(null)?.AbsolutePath,
            Directory.ExternalCache => context.ExternalCacheDir?.AbsolutePath,
            _ => context.FilesDir?.AbsolutePath,
        };

        // 外部存储可能被卸载（SD 卡拔出），此时 GetExternalFilesDir 返回 null。
        if (root is null)
            throw new DirectoryNotFoundException(
                $"The Android directory for {directory} is unavailable (external storage may be unmounted).");

        // Documents/Library 落在 FilesDir 的子目录，避免不同语义的文件混在一起。
        root = directory switch
        {
            Directory.Documents => IoPath.Combine(root, "Documents"),
            Directory.Library => IoPath.Combine(root, "Library"),
            Directory.LibraryNoCloud => IoPath.Combine(root, "LibraryNoCloud"),
            Directory.Temporary => IoPath.Combine(root, "Temporary"),
            _ => root,
        };

        IoDirectory.CreateDirectory(root);
        return root;
    }
}
