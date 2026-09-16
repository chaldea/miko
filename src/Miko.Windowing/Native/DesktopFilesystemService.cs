using Miko.Native;
using Miko.Native.Filesystem;
using Directory = Miko.Native.Filesystem.Directory;
using FileInfo = Miko.Native.Filesystem.FileInfo;
using IoDirectory = System.IO.Directory;
using IoPath = System.IO.Path;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面文件系统实现。
/// <para>
/// 读写逻辑全部复用 <see cref="NativeFileOperations"/>（三端共享的 <c>System.IO</c> 实现），
/// 本类只负责桌面特有的一件事：把限定目录映射到 <c>Environment.SpecialFolder</c>。
/// </para>
/// <para>
/// 桌面没有系统级的「存储权限」概念，权限查询恒为 <see cref="PermissionState.Granted"/>。
/// </para>
/// </summary>
#pragma warning disable CS0618 // DownloadFile/OnProgress 在接口上已标记弃用，实现仍需提供。
internal sealed class DesktopFilesystemService : NativeServiceBase, IFilesystemService
{
    private readonly NativeFileOperations _ops = new(Resolve);

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

    /// <summary>
    /// 把「限定目录 + 相对路径」解析为绝对路径。<paramref name="directory"/> 为 <c>null</c> 时
    /// <paramref name="path"/> 按完整路径或 <c>file://</c> URI 处理。
    /// </summary>
    internal static string Resolve(string path, Directory? directory)
    {
        if (directory is null)
        {
            if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                return new Uri(path).LocalPath;

            // 相对路径基于 AppContext.BaseDirectory，与 Miko 资源加载的既有约定一致
            // （进程 CWD 在 IDE 启动时与输出目录不同）。
            return IoPath.IsPathRooted(path)
                ? path
                : IoPath.GetFullPath(IoPath.Combine(AppContext.BaseDirectory, path));
        }

        return IoPath.GetFullPath(IoPath.Combine(RootFor(directory.Value), path));
    }

    /// <summary>把限定目录映射到桌面上的实际根目录。</summary>
    internal static string RootFor(Directory directory)
    {
        // 应用私有目录统一落在 <LocalAppData>/<AppName> 下，避免不同 Miko 应用互相覆盖文件。
        var appName = AppDomain.CurrentDomain.FriendlyName;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = IoPath.Combine(localAppData, appName);

        var root = directory switch
        {
            Directory.Documents => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Directory.Data => appData,
            Directory.Library => IoPath.Combine(appData, "Library"),
            Directory.LibraryNoCloud => IoPath.Combine(appData, "LibraryNoCloud"),
            Directory.Cache => IoPath.Combine(appData, "Cache"),
            // 桌面没有 Android 的「外部存储」分区；映射到应用目录下的同名子目录，
            // 使跨平台代码路径一致，而不是抛异常让文件 API 在桌面上半残。
            Directory.External => IoPath.Combine(appData, "External"),
            Directory.ExternalStorage => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Directory.ExternalCache => IoPath.Combine(appData, "ExternalCache"),
            Directory.Temporary => IoPath.Combine(IoPath.GetTempPath(), appName),
            _ => appData,
        };

        IoDirectory.CreateDirectory(root);
        return root;
    }
}
#pragma warning restore CS0618
