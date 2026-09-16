namespace Miko.Native.Filesystem;

/// <summary>
/// 文件与目录操作。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/filesystem#api">Filesystem API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface IFilesystemService
{
    /// <summary>检查读写权限。</summary>
    Task<FilePermissionStatus> CheckPermissionsAsync();

    /// <summary>请求读写权限。</summary>
    Task<FilePermissionStatus> RequestPermissionsAsync();

    /// <summary>读取文件；未指定编码时返回 Base64。</summary>
    Task<ReadFileResult> ReadFileAsync(ReadFileOptions options);

    /// <summary>
    /// 分块读取文件（7.1+，仅原生平台），返回本次读取的标识。
    /// 回调的两个参数互斥：有数据时为数据块、出错时为异常；两者皆为 <c>null</c> 表示读取结束。
    /// </summary>
    Task<string> ReadFileInChunksAsync(ReadFileInChunksOptions options, Action<ReadFileResult?, Exception?> callback);

    /// <summary>写入文件。</summary>
    Task<WriteFileResult> WriteFileAsync(WriteFileOptions options);

    /// <summary>向文件末尾追加内容。</summary>
    Task AppendFileAsync(AppendFileOptions options);

    /// <summary>删除文件。</summary>
    Task DeleteFileAsync(DeleteFileOptions options);

    /// <summary>创建目录。</summary>
    Task MkdirAsync(MkdirOptions options);

    /// <summary>删除目录。</summary>
    Task RmdirAsync(RmdirOptions options);

    /// <summary>列出目录内容（非递归）。</summary>
    Task<ReadDirectoryResult> ReadDirectoryAsync(ReadDirectoryOptions options);

    /// <summary>获取完整文件 URI。</summary>
    Task<GetUriResult> GetUriAsync(GetUriOptions options);

    /// <summary>获取文件或目录信息。</summary>
    Task<FileInfo> StatAsync(StatOptions options);

    /// <summary>重命名文件或目录。</summary>
    Task RenameAsync(RenameOptions options);

    /// <summary>复制文件或目录。</summary>
    Task<CopyResult> CopyAsync(CopyOptions options);

    /// <summary>
    /// 下载文件到本地。
    /// <para>Capacitor 自 v7.1 起弃用该 API，建议改用 File Transfer 插件；此处保留以对齐原始文档，
    /// 新代码不应使用。</para>
    /// </summary>
    [Obsolete("Deprecated since Capacitor v7.1. Use a dedicated file-transfer implementation instead.")]
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileOptions options);

    /// <summary>下载进度（随 <see cref="DownloadFileAsync"/> 一同弃用）。</summary>
    [Obsolete("Deprecated since Capacitor v7.1, together with DownloadFileAsync.")]
    event Action<ProgressStatus>? OnProgress;
}
