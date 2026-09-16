namespace Miko.Native.Filesystem;

/// <summary>未注册平台实现时的 <see cref="IFilesystemService"/> 占位实现，调用即抛。</summary>
#pragma warning disable CS0618 // 实现已弃用成员是必要的：接口仍声明它们以对齐原始文档。
public sealed class NullFilesystemService : NativeServiceBase, IFilesystemService
{
    public Task<FilePermissionStatus> CheckPermissionsAsync() => UnsupportedAsync<FilePermissionStatus>();
    public Task<FilePermissionStatus> RequestPermissionsAsync() => UnsupportedAsync<FilePermissionStatus>();
    public Task<ReadFileResult> ReadFileAsync(ReadFileOptions options) => UnsupportedAsync<ReadFileResult>();

    public Task<string> ReadFileInChunksAsync(ReadFileInChunksOptions options, Action<ReadFileResult?, Exception?> callback)
        => UnsupportedAsync<string>();

    public Task<WriteFileResult> WriteFileAsync(WriteFileOptions options) => UnsupportedAsync<WriteFileResult>();
    public Task AppendFileAsync(AppendFileOptions options) => UnsupportedAsync();
    public Task DeleteFileAsync(DeleteFileOptions options) => UnsupportedAsync();
    public Task MkdirAsync(MkdirOptions options) => UnsupportedAsync();
    public Task RmdirAsync(RmdirOptions options) => UnsupportedAsync();
    public Task<ReadDirectoryResult> ReadDirectoryAsync(ReadDirectoryOptions options) => UnsupportedAsync<ReadDirectoryResult>();
    public Task<GetUriResult> GetUriAsync(GetUriOptions options) => UnsupportedAsync<GetUriResult>();
    public Task<FileInfo> StatAsync(StatOptions options) => UnsupportedAsync<FileInfo>();
    public Task RenameAsync(RenameOptions options) => UnsupportedAsync();
    public Task<CopyResult> CopyAsync(CopyOptions options) => UnsupportedAsync<CopyResult>();
    public Task<DownloadFileResult> DownloadFileAsync(DownloadFileOptions options) => UnsupportedAsync<DownloadFileResult>();

    public event Action<ProgressStatus>? OnProgress { add { } remove { } }
}
#pragma warning restore CS0618
