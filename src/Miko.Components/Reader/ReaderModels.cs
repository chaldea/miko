namespace Miko.Components.Reader;

/// <summary>A stable source identifier and its directory label. Indices in the reader are zero based.</summary>
public sealed record ReaderEntry(string Id, string Title);

/// <summary>Reading order, independent of whether content comes from an archive or a remote API.</summary>
public sealed record ReaderDocument(string Title, IReadOnlyList<ReaderEntry> Entries);

/// <summary>Immutable content. Providers must not modify returned text or image buffers.</summary>
public abstract record ReaderContent
{
    public abstract long SizeInBytes { get; }
}

public sealed record ReaderTextContent(string Text) : ReaderContent
{
    public override long SizeInBytes => (long)Text.Length * sizeof(char);
}

/// <summary>Encoded PNG, JPEG, WebP, GIF or SVG; decoded through Miko.Graphics only when displayed.</summary>
public sealed record ReaderImageContent(ReadOnlyMemory<byte> Data) : ReaderContent
{
    public override long SizeInBytes => Data.Length;
}

/// <summary>
/// Directory and content are separate requests. ReadAsync loads only the requested chapter/page.
/// Implementations must support overlapping calls (cancelled requests can still be completing).
/// The caller owns the source and any HttpClient or file handles, never the component/controller.
/// </summary>
public interface IReaderSource
{
    Task<ReaderDocument> GetDocumentAsync(CancellationToken cancellationToken = default);
    Task<ReaderContent> ReadAsync(ReaderEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>Adapts an application's directory/page API without prescribing a network protocol.</summary>
public sealed class DelegateReaderSource(
    Func<CancellationToken, Task<ReaderDocument>> getDocument,
    Func<ReaderEntry, CancellationToken, Task<ReaderContent>> read) : IReaderSource
{
    public Task<ReaderDocument> GetDocumentAsync(CancellationToken cancellationToken = default) => getDocument(cancellationToken);
    public Task<ReaderContent> ReadAsync(ReaderEntry entry, CancellationToken cancellationToken = default) => read(entry, cancellationToken);
}

public enum ReaderState { Idle, Loading, Ready, Error }
public enum BookReadingMode { Paged, Scroll }
public enum ReaderTheme { Light, Sepia, Dark }
public enum ComicPageMode { Single, Double }
public enum ReaderDirection { LeftToRight, RightToLeft }
public enum ReaderOrientation { Horizontal, Vertical }

public sealed record ReaderSnapshot(
    ReaderState State, ReaderDocument? Document, int Index,
    IReadOnlyList<ReaderContent> Content, Exception? Error = null)
{
    public static ReaderSnapshot Empty { get; } = new(ReaderState.Idle, null, 0, []);
}
