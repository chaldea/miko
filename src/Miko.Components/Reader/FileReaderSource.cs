using System.Globalization;
using System.IO.Compression;
using System.Text;
using SharpCompress.Archives;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Rendering.Skia;

namespace Miko.Components.Reader;

public sealed record ReaderFileOptions
{
    /// <summary>UTF-8 by default, with BOM detection. Supply a legacy encoding explicitly when needed.</summary>
    public Encoding TextEncoding { get; init; } = Encoding.UTF8;
    public int TextChunkLength { get; init; } = 16_000;
    public long MaxEntryBytes { get; init; } = 64 * 1024 * 1024;
    public int MaxEntries { get; init; } = 100_000;
    /// <summary>False preserves PDF pages (including scans); true extracts text for adjustable typography.</summary>
    public bool ReflowPdf { get; init; }
    public float PdfScale { get; init; } = 1.5f;
    public long MaxPdfPixels { get; init; } = 16_000_000;
}

/// <summary>
/// TXT / EPUB / PDF / CBZ / CBR adapter. Archives are read in place, never extracted to disk.
/// Directory parsing and decompression run off the render thread. The owner must await DisposeAsync
/// after removing this source from readers. DRM and password-protected files are not supported.
/// </summary>
public sealed class FileReaderSource : IReaderSource, IAsyncDisposable
{
    private readonly string _path;
    private readonly ReaderFileOptions _options;
    private readonly SemaphoreSlim _gate = new(1);
    private ReaderDocument? _document;
    private ZipArchive? _zip;
    private IArchive? _rar;
    private PdfDocument? _pdf;
    private Dictionary<string, ZipArchiveEntry> _zipEntries = [];
    private Dictionary<string, IArchiveEntry> _rarEntries = [];
    private string[]? _text;
    private bool _disposed;

    public FileReaderSource(string path, ReaderFileOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        _options = options ?? new();
        if (_options.TextChunkLength < 2 || _options.MaxEntryBytes < 1 || _options.MaxEntries < 1 ||
            !float.IsFinite(_options.PdfScale) || _options.PdfScale <= 0 || _options.MaxPdfPixels < 1)
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    public Task<ReaderDocument> GetDocumentAsync(CancellationToken cancellationToken = default)
        => RunAsync(() => EnsureDocument(cancellationToken), cancellationToken);

    public Task<ReaderContent> ReadAsync(ReaderEntry entry, CancellationToken cancellationToken = default)
        => RunAsync<ReaderContent>(() =>
        {
            var document = EnsureDocument(cancellationToken);
            if (!document.Entries.Any(e => e.Id == entry.Id)) throw new ArgumentException("Unknown directory entry.", nameof(entry));
            if (_text != null) return new ReaderTextContent(_text[int.Parse(entry.Id, CultureInfo.InvariantCulture)]);
            if (_pdf != null)
            {
                int pageNumber = int.Parse(entry.Id, CultureInfo.InvariantCulture) + 1;
                var page = _pdf.GetPage(pageNumber);
                if (_options.ReflowPdf) return new ReaderTextContent(ContentOrderTextExtractor.GetText(page));
                // Cap raster memory even for PDFs with enormous media boxes.
                double area = page.Width * page.Height;
                if (!double.IsFinite(area) || area <= 0) throw new InvalidDataException("Invalid PDF page size.");
                float scale = (float)Math.Min(_options.PdfScale, Math.Sqrt(_options.MaxPdfPixels / area));
                using var png = _pdf.GetPageAsPng(pageNumber, scale);
                cancellationToken.ThrowIfCancellationRequested();
                return new ReaderImageContent(ReadBounded(png, cancellationToken));
            }
            byte[] data;
            if (_zipEntries.TryGetValue(entry.Id, out var zipEntry))
            {
                CheckSize(zipEntry.Length);
                using var stream = zipEntry.Open();
                data = ReadBounded(stream, cancellationToken);
            }
            else
            {
                var rarEntry = _rarEntries[entry.Id];
                CheckSize(rarEntry.Size);
                if (rarEntry.IsEncrypted) throw new NotSupportedException("Encrypted comic archives are not supported.");
                using var stream = rarEntry.OpenEntryStream();
                data = ReadBounded(stream, cancellationToken);
            }
            return Path.GetExtension(_path).Equals(".epub", StringComparison.OrdinalIgnoreCase)
                ? new ReaderTextContent(EpubReader.ReadText(data)) : new ReaderImageContent(data);
        }, cancellationToken);

    private async Task<T> RunAsync<T>(Func<T> work, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return await Task.Run(() => { ct.ThrowIfCancellationRequested(); return work(); }, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private ReaderDocument EnsureDocument(CancellationToken ct)
    {
        if (_document != null) return _document;
        string title = Path.GetFileNameWithoutExtension(_path);
        try
        {
            switch (Path.GetExtension(_path).ToLowerInvariant())
            {
                case ".txt":
                    using (var file = File.OpenRead(_path))
                    {
                        CheckSize(file.Length);
                        using var data = new MemoryStream(ReadBounded(file, ct));
                        using var reader = new StreamReader(data, _options.TextEncoding, detectEncodingFromByteOrderMarks: true);
                        _text = SplitText(reader.ReadToEnd(), _options.TextChunkLength).ToArray();
                        _document = Indexed(title, _text.Length, "Section");
                    }
                    break;
                case ".pdf":
                    _pdf = PdfDocument.Open(_path, SkiaRenderingParsingOptions.Instance);
                    _pdf.AddSkiaPageFactory();
                    _document = Indexed(title, _pdf.NumberOfPages, "Page");
                    break;
                case ".epub":
                case ".cbz":
                    _zip = ZipFile.OpenRead(_path);
                    CheckCount(_zip.Entries.Count);
                    _zipEntries = _zip.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToDictionary(e => e.FullName, StringComparer.Ordinal);
                    _document = Path.GetExtension(_path).Equals(".epub", StringComparison.OrdinalIgnoreCase)
                        ? EpubReader.ReadDirectory(title, ReadZip)
                        : ComicDirectory(title, _zipEntries.Keys);
                    break;
                case ".cbr":
                    _rar = ArchiveFactory.OpenArchive(_path);
                    var entries = _rar.Entries.Take(_options.MaxEntries + 1).ToArray();
                    CheckCount(entries.Length);
                    _rarEntries = entries.Where(e => !e.IsDirectory && e.Key != null).ToDictionary(e => e.Key!, StringComparer.Ordinal);
                    _document = ComicDirectory(title, _rarEntries.Keys);
                    break;
                default:
                    throw new NotSupportedException("Supported reader formats: .txt, .epub, .pdf, .cbz, .cbr.");
            }
            CheckCount(_document.Entries.Count);
            ct.ThrowIfCancellationRequested();
            return _document;
        }
        catch
        {
            Close();
            throw;
        }

        byte[] ReadZip(string name)
        {
            ct.ThrowIfCancellationRequested();
            if (!_zipEntries.TryGetValue(name, out var entry)) throw new InvalidDataException($"Missing EPUB resource: {name}");
            CheckSize(entry.Length);
            using var stream = entry.Open();
            return ReadBounded(stream, ct);
        }
    }

    private static ReaderDocument Indexed(string title, int count, string label) => new(title,
        Array.AsReadOnly(Enumerable.Range(0, count).Select(i => new ReaderEntry(i.ToString(CultureInfo.InvariantCulture), $"{label} {i + 1}")).ToArray()));

    private static ReaderDocument ComicDirectory(string title, IEnumerable<string> names) => new(title,
        Array.AsReadOnly(names.Where(IsImage).Order(NaturalPathComparer.Instance).Select(n => new ReaderEntry(n, Path.GetFileName(n))).ToArray()));

    private static bool IsImage(string name) => !name.Split('/').Any(p => p.StartsWith('.') || p == "__MACOSX") &&
        Path.GetExtension(name).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp" or ".svg";

    internal static IEnumerable<string> SplitText(string text, int length)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        for (int start = 0; start < text.Length;)
        {
            int end = Math.Min(text.Length, start + length);
            if (end < text.Length)
            {
                int newline = text.LastIndexOf('\n', end - 1, end - start);
                if (newline > start + length / 2) end = newline + 1;
                // Keep combining sequences and surrogate pairs intact at artificial chapter boundaries.
                else end = start + StringInfo.GetNextTextElementLength(text.AsSpan(start));
                if (newline <= start + length / 2)
                    while (end < text.Length && end - start < length)
                    {
                        int next = end + StringInfo.GetNextTextElementLength(text.AsSpan(end));
                        if (next - start > length) break;
                        end = next;
                    }
            }
            yield return text[start..end];
            start = end;
        }
    }

    private void CheckSize(long size)
    {
        if (size < 0 || size > _options.MaxEntryBytes) throw new InvalidDataException("Reader entry exceeds MaxEntryBytes.");
    }

    private void CheckCount(int count)
    {
        if (count > _options.MaxEntries) throw new InvalidDataException("Reader directory exceeds MaxEntries.");
    }

    private byte[] ReadBounded(Stream stream, CancellationToken ct)
    {
        using var result = new MemoryStream();
        byte[] buffer = new byte[81920];
        int count;
        while ((count = stream.Read(buffer)) != 0)
        {
            ct.ThrowIfCancellationRequested();
            CheckSize(result.Length + count);
            result.Write(buffer, 0, count);
        }
        return result.ToArray();
    }

    private void Close()
    {
        _zip?.Dispose(); _zip = null;
        _rar?.Dispose(); _rar = null;
        _pdf?.Dispose(); _pdf = null;
        _text = null; _document = null;
        _zipEntries.Clear(); _rarEntries.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { if (!_disposed) { _disposed = true; Close(); } }
        finally { _gate.Release(); }
    }
}

/// <summary>Ordinal natural order, without integer overflow for long digit runs: page2 before page10.</summary>
internal sealed class NaturalPathComparer : IComparer<string>
{
    public static readonly NaturalPathComparer Instance = new();
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null) return string.CompareOrdinal(x, y);
        int a = 0, b = 0;
        while (a < x.Length && b < y.Length)
        {
            if (char.IsAsciiDigit(x[a]) && char.IsAsciiDigit(y[b]))
            {
                int sa = a, sb = b;
                while (a < x.Length && char.IsAsciiDigit(x[a])) a++;
                while (b < y.Length && char.IsAsciiDigit(y[b])) b++;
                var dx = x.AsSpan(sa, a - sa).TrimStart('0');
                var dy = y.AsSpan(sb, b - sb).TrimStart('0');
                int c = dx.Length.CompareTo(dy.Length);
                if (c == 0) c = dx.SequenceCompareTo(dy);
                if (c != 0) return c;
            }
            else
            {
                int c = char.ToUpperInvariant(x[a++]).CompareTo(char.ToUpperInvariant(y[b++]));
                if (c != 0) return c;
            }
        }
        int length = (x.Length - a).CompareTo(y.Length - b);
        return length != 0 ? length : string.CompareOrdinal(x, y);
    }
}
