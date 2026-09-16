namespace Miko.Native.Filesystem;

/// <summary>
/// 限定目录。路径选项中的相对 <c>Path</c> 相对于该目录解析。
/// <para>注意与 <c>System.IO.Directory</c> 同名，本类型位于 <c>Miko.Native.Filesystem</c> 命名空间。</para>
/// </summary>
public enum Directory
{
    /// <summary>用户文档目录。</summary>
    Documents,

    /// <summary>应用私有数据目录。</summary>
    Data,

    /// <summary>应用 Library 目录（iOS）。</summary>
    Library,

    /// <summary>缓存目录，系统可能清理。</summary>
    Cache,

    /// <summary>应用外部存储目录（Android）。</summary>
    External,

    /// <summary>公共外部存储（Android）。</summary>
    ExternalStorage,

    /// <summary>外部缓存目录（Android）。</summary>
    ExternalCache,

    /// <summary>不参与 iCloud 备份的 Library 目录（iOS）。</summary>
    LibraryNoCloud,

    /// <summary>临时目录。</summary>
    Temporary,
}

/// <summary>
/// 文本编码。未指定编码时文件按 Base64 读写二进制。
/// <para>注意与 <c>System.Text.Encoding</c> 同名，本类型位于 <c>Miko.Native.Filesystem</c> 命名空间。</para>
/// </summary>
public enum Encoding
{
    /// <summary>UTF-8。</summary>
    Utf8,

    /// <summary>ASCII。</summary>
    Ascii,

    /// <summary>UTF-16。</summary>
    Utf16,
}

/// <summary>文件项类型。</summary>
public enum FileEntryType
{
    /// <summary>文件。</summary>
    File,

    /// <summary>目录。</summary>
    Directory,
}

/// <summary>
/// 文件或目录的信息。
/// <para>注意与 <c>System.IO.FileInfo</c> 同名，本类型位于 <c>Miko.Native.Filesystem</c> 命名空间。</para>
/// </summary>
/// <param name="Name">文件名（不含路径）。</param>
/// <param name="Type">是文件还是目录。</param>
/// <param name="Size">大小（字节）。</param>
/// <param name="CreationTime">创建时间（Unix 毫秒时间戳）。</param>
/// <param name="ModifiedTime">修改时间（Unix 毫秒时间戳）。</param>
/// <param name="Uri">完整的原生 URI。</param>
public sealed record FileInfo(
    string Name,
    FileEntryType Type,
    long Size,
    long? CreationTime,
    long? ModifiedTime,
    string Uri);

/// <summary>文件读写权限状态。</summary>
/// <param name="PublicStorage">公共存储读写权限。</param>
public sealed record FilePermissionStatus(PermissionState PublicStorage);

/// <summary>读取文件的选项。</summary>
public sealed record ReadFileOptions
{
    /// <summary>文件路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录；为 <c>null</c> 时 <see cref="Path"/> 视为完整路径/URI。</summary>
    public Directory? Directory { get; init; }

    /// <summary>文本编码；为 <c>null</c> 时返回 Base64 二进制。</summary>
    public Encoding? Encoding { get; init; }

    /// <summary>读取起始偏移（字节）。</summary>
    public long Offset { get; init; }

    /// <summary>读取长度（字节）；0 表示读到文件末尾。</summary>
    public long Length { get; init; }
}

/// <summary>分块读取文件的选项（7.1+，仅原生平台）。</summary>
public sealed record ReadFileInChunksOptions
{
    /// <summary>文件路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>文本编码；为 <c>null</c> 时每块返回 Base64。</summary>
    public Encoding? Encoding { get; init; }

    /// <summary>每块大小（字节）。</summary>
    public long ChunkSize { get; init; }
}

/// <summary>读取文件的结果。</summary>
/// <param name="DataBase64">未指定编码时的 Base64 内容，或指定编码时的文本内容。</param>
/// <param name="Blob">二进制流形式的内容，平台支持时提供。</param>
public sealed record ReadFileResult(string? DataBase64, Stream? Blob = null);

/// <summary>写入文件的选项。</summary>
public sealed record WriteFileOptions
{
    /// <summary>文件路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>要写入的内容：未指定 <see cref="Encoding"/> 时按 Base64 解码，否则按文本写入。</summary>
    public string? DataBase64 { get; init; }

    /// <summary>要写入的二进制流；优先于 <see cref="DataBase64"/>。</summary>
    public Stream? Data { get; init; }

    /// <summary>文本编码；为 <c>null</c> 时 <see cref="DataBase64"/> 按 Base64 解码。</summary>
    public Encoding? Encoding { get; init; }

    /// <summary>是否自动创建缺失的父目录。</summary>
    public bool Recursive { get; init; }
}

/// <summary>写入文件的结果。</summary>
/// <param name="Uri">写入文件的完整 URI。</param>
public sealed record WriteFileResult(string Uri);

/// <summary>追加文件的选项。</summary>
public sealed record AppendFileOptions
{
    /// <summary>文件路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>要追加的内容。</summary>
    public string? DataBase64 { get; init; }

    /// <summary>要追加的二进制流；优先于 <see cref="DataBase64"/>。</summary>
    public Stream? Data { get; init; }

    /// <summary>文本编码；为 <c>null</c> 时按 Base64 解码。</summary>
    public Encoding? Encoding { get; init; }
}

/// <summary>删除文件的选项。</summary>
public sealed record DeleteFileOptions
{
    /// <summary>文件路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }
}

/// <summary>创建目录的选项。</summary>
public sealed record MkdirOptions
{
    /// <summary>目录路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>是否连同缺失的父目录一并创建。</summary>
    public bool Recursive { get; init; }
}

/// <summary>删除目录的选项。</summary>
public sealed record RmdirOptions
{
    /// <summary>目录路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>是否递归删除目录内容；为 <c>false</c> 且目录非空时失败。</summary>
    public bool Recursive { get; init; }
}

/// <summary>列出目录的选项。</summary>
public sealed record ReadDirectoryOptions
{
    /// <summary>目录路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }
}

/// <summary>列出目录的结果（非递归）。</summary>
/// <param name="Files">目录下的文件与子目录。</param>
public sealed record ReadDirectoryResult(IReadOnlyList<FileInfo> Files);

/// <summary>获取完整 URI 的选项。</summary>
public sealed record GetUriOptions
{
    /// <summary>路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }
}

/// <summary>获取完整 URI 的结果。</summary>
/// <param name="Uri">完整的原生 URI。</param>
public sealed record GetUriResult(string Uri);

/// <summary>获取文件信息的选项。</summary>
public sealed record StatOptions
{
    /// <summary>路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }
}

/// <summary>重命名文件或目录的选项。</summary>
public sealed record RenameOptions
{
    /// <summary>源路径。</summary>
    public required string From { get; init; }

    /// <summary>目标路径。</summary>
    public required string To { get; init; }

    /// <summary>源所在的限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>目标所在的限定目录；为 <c>null</c> 时沿用 <see cref="Directory"/>。</summary>
    public Directory? ToDirectory { get; init; }
}

/// <summary>复制文件或目录的选项。</summary>
public sealed record CopyOptions
{
    /// <summary>源路径。</summary>
    public required string From { get; init; }

    /// <summary>目标路径。</summary>
    public required string To { get; init; }

    /// <summary>源所在的限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>目标所在的限定目录；为 <c>null</c> 时沿用 <see cref="Directory"/>。</summary>
    public Directory? ToDirectory { get; init; }
}

/// <summary>复制结果。</summary>
/// <param name="Uri">目标文件的完整 URI。</param>
public sealed record CopyResult(string Uri);

/// <summary>下载文件的选项（v7.1+ 已弃用，建议改用 File Transfer 插件）。</summary>
public sealed record DownloadFileOptions
{
    /// <summary>下载地址。</summary>
    public required string Url { get; init; }

    /// <summary>保存路径。</summary>
    public required string Path { get; init; }

    /// <summary>限定目录。</summary>
    public Directory? Directory { get; init; }

    /// <summary>是否上报下载进度（通过 <c>OnProgress</c>）。</summary>
    public bool Progress { get; init; }

    /// <summary>是否自动创建缺失的父目录。</summary>
    public bool Recursive { get; init; }
}

/// <summary>下载结果。</summary>
/// <param name="Path">保存到的本地路径。</param>
/// <param name="Blob">下载内容流，平台支持时提供。</param>
public sealed record DownloadFileResult(string Path, Stream? Blob = null);

/// <summary>下载进度。</summary>
/// <param name="Url">下载地址。</param>
/// <param name="Bytes">已下载字节数。</param>
/// <param name="ContentLength">总字节数；未知时为 0。</param>
public sealed record ProgressStatus(string Url, long Bytes, long ContentLength);
