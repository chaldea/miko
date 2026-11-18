namespace Miko.Components.Reader;

/// <summary>
/// Shared lazy loading, navigation and bounded LRU cache. Safe against late completions after a
/// source switch, navigation or disposal. Changed runs on the completing thread; UI hosts dispatch it.
/// Provider errors become snapshots, while cancellation leaves the last committed page visible.
/// </summary>
public sealed class ReaderController : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<int, (ReaderContent Content, LinkedListNode<int> Node)> _cache = [];
    private readonly LinkedList<int> _lru = new();
    private readonly int _cacheEntries;
    private readonly long _cacheBytes;
    private long _size;
    private IReaderSource? _source;
    private ReaderSnapshot _snapshot = ReaderSnapshot.Empty;
    private ReaderSnapshot _committed = ReaderSnapshot.Empty;
    private CancellationTokenSource? _pending;
    private int _generation, _pageSize = 1;
    private bool _disposed;

    public ReaderController(int cacheEntries = 6, long cacheBytes = 32 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cacheEntries);
        ArgumentOutOfRangeException.ThrowIfNegative(cacheBytes);
        _cacheEntries = cacheEntries;
        _cacheBytes = cacheBytes;
    }

    public ReaderSnapshot Snapshot { get { lock (_gate) return _snapshot; } }
    public event Action? Changed;

    public Task OpenAsync(IReaderSource? source, int index = 0, int pageSize = 1, CancellationToken cancellationToken = default)
        => LoadAsync(source, index, pageSize, true, cancellationToken);

    public Task GoToAsync(int index, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        IReaderSource? source;
        int size;
        lock (_gate) { source = _source; size = pageSize ?? _pageSize; }
        return LoadAsync(source, index, size, false, cancellationToken);
    }

    public Task NextAsync(CancellationToken cancellationToken = default)
    {
        int next;
        lock (_gate)
        {
            if (_snapshot.Document is not { } doc || _snapshot.Index + _pageSize >= doc.Entries.Count)
                return Task.CompletedTask;
            next = _snapshot.Index + _pageSize;
        }
        return GoToAsync(next, cancellationToken: cancellationToken);
    }

    public Task PreviousAsync(CancellationToken cancellationToken = default)
    {
        int previous;
        lock (_gate) { if (_snapshot.Index == 0) return Task.CompletedTask; previous = Math.Max(0, _snapshot.Index - _pageSize); }
        return GoToAsync(previous, cancellationToken: cancellationToken);
    }

    public Task RetryAsync(CancellationToken cancellationToken = default)
    {
        IReaderSource? source;
        int index, size;
        bool reset;
        lock (_gate)
        {
            source = _source; index = _snapshot.Index; size = _pageSize; reset = _snapshot.Document == null;
            // A failed image decode can also request a retry: refetch the visible spread.
            for (int offset = 0; offset < size && (long)index + offset <= int.MaxValue; offset++)
                if (_cache.Remove(index + offset, out var item)) { _lru.Remove(item.Node); _size -= item.Content.SizeInBytes; }
        }
        return LoadAsync(source, index, size, reset, cancellationToken);
    }

    private async Task LoadAsync(IReaderSource? source, int index, int pageSize, bool reset, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (pageSize is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(pageSize));
        CancellationTokenSource request;
        ReaderDocument? document;
        int generation;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!reset) source = _source;
            _pending?.Cancel();
            request = CancellationTokenSource.CreateLinkedTokenSource(token);
            _pending = request;
            generation = ++_generation;
            _source = source;
            _pageSize = pageSize;
            if (reset)
            {
                _cache.Clear(); _lru.Clear(); _size = 0;
                _committed = ReaderSnapshot.Empty;
            }
            document = reset ? null : _snapshot.Document;
            _snapshot = source == null ? ReaderSnapshot.Empty : new(ReaderState.Loading, document, index, []);
        }
        Changed?.Invoke();
        try
        {
            if (source == null) return;
            var ct = request.Token;
            if (document == null)
            {
                var loaded = await source.GetDocumentAsync(ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                // Own the directory snapshot: mutable API lists cannot invalidate navigation mid-load.
                var entries = loaded.Entries.ToArray();
                if (entries.Any(e => e == null || string.IsNullOrWhiteSpace(e.Id)) || entries.Select(e => e.Id).Distinct().Count() != entries.Length)
                    throw new InvalidDataException("Reader directory entries must have unique, non-empty IDs.");
                document = loaded with { Entries = Array.AsReadOnly(entries) };
            }
            index = Math.Clamp(index, 0, Math.Max(0, document.Entries.Count - 1));
            lock (_gate)
            {
                if (!IsCurrent(generation)) return;
                _snapshot = new(ReaderState.Loading, document, index, []);
            }
            Changed?.Invoke();
            var pages = new List<ReaderContent>();
            for (int i = index; i < Math.Min(document.Entries.Count, (long)index + pageSize); i++)
            {
                ReaderContent? content;
                lock (_gate)
                {
                    if (!IsCurrent(generation)) return;
                    content = GetCached(i);
                }
                content ??= await source.ReadAsync(document.Entries[i], ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                if (content is not (ReaderTextContent or ReaderImageContent)) throw new InvalidDataException("The source returned unsupported reader content.");
                lock (_gate)
                {
                    if (!IsCurrent(generation)) return;
                    Cache(i, content);
                }
                pages.Add(content);
            }
            ct.ThrowIfCancellationRequested();
            lock (_gate)
            {
                if (!IsCurrent(generation)) return;
                _snapshot = _committed = new(ReaderState.Ready, document, index, pages.AsReadOnly());
            }
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            lock (_gate)
            {
                if (!IsCurrent(generation)) return;
                _snapshot = _committed;
            }
        }
        catch (Exception error)
        {
            lock (_gate)
            {
                if (!IsCurrent(generation)) return;
                _snapshot = new(ReaderState.Error, document, index, [], error);
            }
        }
        finally
        {
            lock (_gate) { if (ReferenceEquals(_pending, request)) _pending = null; }
            request.Dispose();
        }
        Changed?.Invoke();
    }

    private bool IsCurrent(int generation) => !_disposed && generation == _generation;

    private ReaderContent? GetCached(int index)
    {
        if (!_cache.TryGetValue(index, out var item)) return null;
        _lru.Remove(item.Node); _lru.AddFirst(item.Node);
        return item.Content;
    }

    private void Cache(int index, ReaderContent content)
    {
        if (_cache.ContainsKey(index) || _cacheEntries == 0 || content.SizeInBytes > _cacheBytes) return;
        while (_cache.Count >= _cacheEntries || _size + content.SizeInBytes > _cacheBytes)
        {
            int oldest = _lru.Last!.Value;
            _size -= _cache[oldest].Content.SizeInBytes;
            _cache.Remove(oldest); _lru.RemoveLast();
        }
        _cache.Add(index, (content, _lru.AddFirst(index)));
        _size += content.SizeInBytes;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true; ++_generation;
            _pending?.Cancel();
            _cache.Clear(); _lru.Clear(); _size = 0;
            _source = null; _snapshot = _committed = ReaderSnapshot.Empty;
            Changed = null;
        }
    }
}
