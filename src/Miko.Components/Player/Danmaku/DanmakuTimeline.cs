namespace Miko.Components.Player.Danmaku;

/// <summary>Bounded active set. Positions depend only on media time, never a wall-clock timer.</summary>
public sealed class DanmakuTimeline
{
    private DanmakuItem[] _items = [];
    private readonly List<Active> _active = new(80);
    private readonly List<VisibleDanmaku> _visible = new(80);
    private int _next;
    private double _last = double.NaN;
    private float _width, _height;
    private DanmakuSettings _settings = new();
    private readonly record struct Active(DanmakuItem Item, float Width, int Lane);
    public IReadOnlyList<VisibleDanmaku> Visible => _visible;
    public DanmakuSettings Settings
    {
        get => _settings;
        set { _settings = value.Validate(); Reset(); }
    }

    public void SetItems(IEnumerable<DanmakuItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items = items.Where(x => x.Time >= TimeSpan.Zero && !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => x.Text.Length > 300 ? x with { Text = x.Text[..300] } : x)
            .OrderBy(x => x.Time).ToArray();
        Reset();
    }

    public void Reset() { _last = double.NaN; _active.Clear(); _visible.Clear(); }

    public void Advance(TimeSpan position, float width, float height, Func<string, float> measure)
    {
        _visible.Clear();
        if (!_settings.Enabled || width <= 0 || height <= 0) { _active.Clear(); _last = double.NaN; return; }
        double now = position.TotalSeconds;
        if (double.IsNaN(_last) || now < _last || now - _last > .5 || width != _width || height != _height)
        {
            _active.Clear();
            _next = LowerBound(Math.Max(0, now - _settings.Duration));
        }
        _width = width;
        _height = height;
        _last = now;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (now - _active[i].Item.Time.TotalSeconds >= _settings.Duration) _active.RemoveAt(i);

        int end = UpperBound(now);
        // Limit seek/catch-up work even for adversarially dense timelines.
        _next = Math.Max(_next, end - 2048);
        float lineHeight = _settings.FontSize + 8;
        int lanes = Math.Max(1, (int)(height * _settings.Area / lineHeight));
        while (_next < end)
        {
            var item = _items[_next++];
            if (_active.Count >= _settings.MaxVisible || now - item.Time.TotalSeconds >= _settings.Duration) continue;
            float itemWidth = Math.Max(1, measure(item.Text));
            for (int n = 0; n < lanes; n++)
            {
                int lane = item.Mode == DanmakuMode.Bottom ? lanes - 1 - n : n;
                if (!CanUseLane(item, itemWidth, lane, width)) continue;
                _active.Add(new Active(item, itemWidth, lane));
                break;
            }
        }
        foreach (var active in _active)
        {
            float age = (float)(now - active.Item.Time.TotalSeconds);
            float x = active.Item.Mode == DanmakuMode.Scroll
                ? width - age * (width + active.Width) / _settings.Duration
                : (width - active.Width) / 2;
            _visible.Add(new VisibleDanmaku(active.Item, x, active.Lane * lineHeight, active.Width));
        }
    }

    private bool CanUseLane(DanmakuItem item, float itemWidth, int lane, float width)
    {
        foreach (var previous in _active)
        {
            if (previous.Lane != lane) continue;
            double elapsed = item.Time.TotalSeconds - previous.Item.Time.TotalSeconds;
            if (elapsed >= _settings.Duration) continue;
            if (item.Mode != DanmakuMode.Scroll || previous.Item.Mode != DanmakuMode.Scroll) return false;
            double previousSpeed = (width + previous.Width) / _settings.Duration;
            if (width - elapsed * previousSpeed + previous.Width + 12 > width) return false;
            double remaining = _settings.Duration - elapsed;
            double speed = (width + itemWidth) / _settings.Duration;
            if (width - speed * remaining < 12) return false;
        }
        return true;
    }

    private int LowerBound(double seconds)
    {
        int lo = 0, hi = _items.Length;
        while (lo < hi) { int mid = lo + (hi - lo) / 2; if (_items[mid].Time.TotalSeconds < seconds) lo = mid + 1; else hi = mid; }
        return lo;
    }
    private int UpperBound(double seconds)
    {
        int lo = 0, hi = _items.Length;
        while (lo < hi) { int mid = lo + (hi - lo) / 2; if (_items[mid].Time.TotalSeconds <= seconds) lo = mid + 1; else hi = mid; }
        return lo;
    }
}
