namespace Miko.Common;

/// <summary>
/// <c>repeat()</c> 的自动重复模式（CSS <c>auto-fill</c> / <c>auto-fit</c>，ISSUE-159）。
/// </summary>
public enum GridAutoRepeat : byte
{
    /// <summary><c>auto-fill</c>：按容器尺寸放下尽可能多的重复轨道，空轨道保留。</summary>
    AutoFill,

    /// <summary><c>auto-fit</c>：同 auto-fill，但放置后仍为空的重复轨道折叠为 0（两侧 gap 一并折叠）。</summary>
    AutoFit,
}

/// <summary>
/// CSS Grid 轨道列表（<c>grid-template-columns</c> / <c>grid-template-rows</c>）中的一项，
/// 或隐式轨道（<c>grid-auto-rows</c> / <c>grid-auto-columns</c>）的尺寸。
///
/// <para>一项是下列之一：</para>
/// <list type="bullet">
/// <item>轨道尺寸：固定长度（px/em/rem/% 等 <see cref="Length"/>）、<c>fr</c> 分数、<c>auto</c>、
/// <c>min-content</c>、<c>max-content</c>、<c>minmax(min, max)</c>、<c>fit-content(limit)</c>；</item>
/// <item><c>repeat(count | auto-fill | auto-fit, …)</c>：按次数（或容器尺寸）重复其中的轨道与线名（ISSUE-159）；</item>
/// <item>线名 <c>[name …]</c>：给它所在位置的网格线命名，供子项按名放置（见 <see cref="GridLine"/>）。</item>
/// </list>
///
/// <code>
/// GridTrackSize.Px(100)                                   // 100px
/// GridTrackSize.Fr(1)                                     // 1fr
/// GridTrackSize.MinMax(120, GridTrackSize.Fr(1))          // minmax(120px, 1fr)
/// GridTrackSize.FitContent(Length.Px(300))                // fit-content(300px)
/// GridTrackSize.Repeat(3, GridTrackSize.Fr(1))            // repeat(3, 1fr)
/// GridTrackSize.Repeat(GridAutoRepeat.AutoFill,
///     GridTrackSize.MinMax(120, GridTrackSize.Fr(1)))     // repeat(auto-fill, minmax(120px, 1fr))
/// GridTrackSize.LineNames("content-start")                // [content-start]
/// (GridTrackSize)100f / (GridTrackSize)Length.Em(2)       // 隐式转换为固定轨道
/// </code>
///
/// <para>线名与 <c>repeat()</c> 只能出现在模板列表里；用作 <c>grid-auto-*</c> 时按 <c>auto</c> 处理。</para>
/// </summary>
public readonly struct GridTrackSize
{
    /// <summary>轨道列表项的类别。</summary>
    public enum TrackKind : byte { Fixed, Fraction, Auto, MinContent, MaxContent, MinMax, FitContent, Repeat, LineNames }

    /// <summary>当前项的类别。</summary>
    public TrackKind Kind { get; }

    /// <summary>
    /// 固定长度：<see cref="TrackKind.Fixed"/> 时为轨道尺寸，<see cref="TrackKind.FitContent"/> 时为上限。
    /// </summary>
    public Length Fixed { get; }

    /// <summary>fr 分数（仅 <see cref="Kind"/> 为 Fraction 时有意义，&gt; 0）。</summary>
    public float Fraction { get; }

    // 罕见类别的负载：MinMax → GridTrackSize[2]（min, max）；Repeat → RepeatPayload；LineNames → string[]。
    private readonly object? _payload;

    private GridTrackSize(TrackKind kind, Length fixedLength, float fraction, object? payload = null)
    {
        Kind = kind;
        Fixed = fixedLength;
        Fraction = fraction;
        _payload = payload;
    }

    private sealed class RepeatPayload(int count, GridAutoRepeat? mode, GridTrackSize[] tracks)
    {
        public int Count { get; } = count;
        public GridAutoRepeat? Mode { get; } = mode;
        public GridTrackSize[] Tracks { get; } = tracks;
    }

    /// <summary>该项是否为 fr 分数轨道。</summary>
    public bool IsFraction => Kind == TrackKind.Fraction;

    /// <summary>该项是否为 auto（内容尺寸）轨道。</summary>
    public bool IsAuto => Kind == TrackKind.Auto;

    /// <summary>该项是否为固定长度轨道（含百分比）。</summary>
    public bool IsFixed => Kind == TrackKind.Fixed;

    /// <summary>该项是否为 <c>minmax()</c>。</summary>
    public bool IsMinMax => Kind == TrackKind.MinMax;

    /// <summary>该项是否为 <c>repeat()</c>。</summary>
    public bool IsRepeat => Kind == TrackKind.Repeat;

    /// <summary>该项是否为线名 <c>[name …]</c>（不是轨道）。</summary>
    public bool IsLineNames => Kind == TrackKind.LineNames;

    /// <summary><c>minmax()</c> 的下限（其余类别返回自身）。</summary>
    public GridTrackSize Min => _payload is GridTrackSize[] pair ? pair[0] : this;

    /// <summary><c>minmax()</c> 的上限（其余类别返回自身）。</summary>
    public GridTrackSize Max => _payload is GridTrackSize[] pair ? pair[1] : this;

    /// <summary><c>repeat()</c> 的重复次数；auto-fill / auto-fit 时为 0。</summary>
    public int RepeatCount => (_payload as RepeatPayload)?.Count ?? 0;

    /// <summary><c>repeat()</c> 的自动重复模式；按次数重复（或不是 repeat）时为 null。</summary>
    public GridAutoRepeat? AutoRepeat => (_payload as RepeatPayload)?.Mode;

    /// <summary><c>repeat()</c> 中被重复的轨道与线名。</summary>
    public IReadOnlyList<GridTrackSize> RepeatTracks => (_payload as RepeatPayload)?.Tracks ?? [];

    /// <summary>线名项的名字。</summary>
    public IReadOnlyList<string> Names => _payload as string[] ?? [];

    /// <summary>固定像素轨道。</summary>
    public static GridTrackSize Px(float value) => new(TrackKind.Fixed, Length.Px(value), 0);

    /// <summary>百分比轨道（相对容器对应轴的内容尺寸；对不确定尺寸轴解析时退化为 auto）。</summary>
    public static GridTrackSize Percent(float value) => new(TrackKind.Fixed, Length.Percent(value), 0);

    /// <summary>fr 分数轨道：按比例分配固定/auto 轨道与 gap 之外的剩余空间。</summary>
    public static GridTrackSize Fr(float value) => new(TrackKind.Fraction, default, value);

    /// <summary>auto 轨道：由该轨道内子项的内容尺寸决定。</summary>
    public static GridTrackSize Auto => new(TrackKind.Auto, default, 0);

    /// <summary><c>min-content</c> 轨道：容纳子项的最小内容尺寸（在每个换行机会处换行）。</summary>
    public static GridTrackSize MinContent => new(TrackKind.MinContent, default, 0);

    /// <summary><c>max-content</c> 轨道：容纳子项不换行时的内容尺寸。</summary>
    public static GridTrackSize MaxContent => new(TrackKind.MaxContent, default, 0);

    /// <summary>由任意 <see cref="Length"/> 构造固定轨道（auto 长度视为 auto 轨道）。</summary>
    public static GridTrackSize FromLength(Length length)
        => length.IsAuto ? Auto : new GridTrackSize(TrackKind.Fixed, length, 0);

    /// <summary>
    /// <c>minmax(min, max)</c>：轨道尺寸不小于 <paramref name="min"/>、不大于 <paramref name="max"/>。
    /// 两端可为固定长度、auto、min-content、max-content；<paramref name="max"/> 还可为 fr。
    /// <paramref name="max"/> 小于 <paramref name="min"/> 时按 <paramref name="min"/> 处理（同 CSS）。
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="min"/> 是 fr，或任一端不是单个轨道尺寸。</exception>
    public static GridTrackSize MinMax(GridTrackSize min, GridTrackSize max)
    {
        if (!IsBreadth(min) || min.IsFraction)
            throw new ArgumentException($"minmax() minimum must be a length, auto, min-content or max-content, not {min}.", nameof(min));
        if (!IsBreadth(max))
            throw new ArgumentException($"minmax() maximum must be a length, fr, auto, min-content or max-content, not {max}.", nameof(max));
        return new GridTrackSize(TrackKind.MinMax, default, 0, new[] { min, max });
    }

    /// <summary>
    /// <c>fit-content(limit)</c>：同 <c>auto</c> 按内容定尺寸，但不超过 <paramref name="limit"/>
    /// （内容的最小尺寸更大时以最小尺寸为准）。
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="limit"/> 为 auto。</exception>
    public static GridTrackSize FitContent(Length limit)
    {
        if (limit.IsAuto)
            throw new ArgumentException("fit-content() needs a length or percentage.", nameof(limit));
        return new GridTrackSize(TrackKind.FitContent, limit, 0);
    }

    /// <summary>
    /// <c>repeat(count, tracks…)</c>：把 <paramref name="tracks"/>（轨道尺寸与线名）重复
    /// <paramref name="count"/> 次。
    /// </summary>
    /// <exception cref="ArgumentException">次数小于 1、没有轨道，或其中嵌套了 repeat()。</exception>
    public static GridTrackSize Repeat(int count, params GridTrackSize[] tracks)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "repeat() count must be at least 1.");
        return new GridTrackSize(TrackKind.Repeat, default, 0, new RepeatPayload(count, null, ValidateRepeated(tracks, autoRepeat: false)));
    }

    /// <summary>
    /// <c>repeat(auto-fill | auto-fit, tracks…)</c>：重复次数取容器能放下的最多次数（容器尺寸不确定时
    /// 按 max-width/height，再按 min-width/height 能填满的最少次数，都没有则为 1）。被重复的每条轨道
    /// 至少要有一端是固定长度（CSS <c>&lt;fixed-size&gt;</c>），否则无从计算次数。
    /// 一个轨道列表里只能有一个自动重复。
    /// </summary>
    /// <exception cref="ArgumentException">没有轨道、嵌套了 repeat()，或某条轨道两端都不是固定长度。</exception>
    public static GridTrackSize Repeat(GridAutoRepeat mode, params GridTrackSize[] tracks)
        => new(TrackKind.Repeat, default, 0, new RepeatPayload(0, mode, ValidateRepeated(tracks, autoRepeat: true)));

    /// <summary>
    /// 线名 <c>[name …]</c>：给轨道列表中此处的网格线命名（一条线可有多个名字）。
    /// 子项经 <see cref="GridLine.Named(string)"/> / <see cref="GridLine.Span(string, int)"/> 引用；
    /// <c>foo-start</c> / <c>foo-end</c> 成对出现时还构成名为 <c>foo</c> 的隐式区域。
    /// </summary>
    /// <exception cref="ArgumentException">没有名字，或名字为空、含空白、是保留字 <c>span</c> / <c>auto</c>。</exception>
    public static GridTrackSize LineNames(params string[] names)
    {
        if (names == null || names.Length == 0)
            throw new ArgumentException("A line-names entry needs at least one name.", nameof(names));
        foreach (var name in names)
            GridLine.ValidateName(name, nameof(names));
        return new GridTrackSize(TrackKind.LineNames, default, 0, (string[])names.Clone());
    }

    /// <summary><c>100f</c> → 100px 固定轨道。</summary>
    public static implicit operator GridTrackSize(float value) => Px(value);

    /// <summary><c>100</c> → 100px 固定轨道。</summary>
    public static implicit operator GridTrackSize(int value) => Px(value);

    /// <summary>任意 <see cref="Length"/> → 固定轨道（auto → auto 轨道）。</summary>
    public static implicit operator GridTrackSize(Length length) => FromLength(length);

    /// <summary>是否为单个轨道尺寸（可出现在 minmax() 两端的形式）。</summary>
    private static bool IsBreadth(GridTrackSize t)
        => t.Kind is TrackKind.Fixed or TrackKind.Fraction or TrackKind.Auto or TrackKind.MinContent or TrackKind.MaxContent;

    /// <summary>是否为 CSS <c>&lt;fixed-size&gt;</c>：固定长度，或至少一端是固定长度的 minmax()。</summary>
    internal bool IsFixedSize => Kind switch
    {
        TrackKind.Fixed => true,
        TrackKind.MinMax => Min.IsFixed || Max.IsFixed,
        _ => false,
    };

    private static GridTrackSize[] ValidateRepeated(GridTrackSize[] tracks, bool autoRepeat)
    {
        if (tracks == null || tracks.Length == 0)
            throw new ArgumentException("repeat() needs at least one track.", nameof(tracks));

        bool anyTrack = false;
        foreach (var t in tracks)
        {
            if (t.IsRepeat)
                throw new ArgumentException("repeat() cannot be nested.", nameof(tracks));
            if (t.IsLineNames) continue;
            anyTrack = true;
            if (autoRepeat && !t.IsFixedSize)
                throw new ArgumentException(
                    $"repeat(auto-fill/auto-fit) tracks need a fixed size (a length, or minmax() with a length end), not {t}.",
                    nameof(tracks));
        }
        if (!anyTrack)
            throw new ArgumentException("repeat() needs at least one track, not only line names.", nameof(tracks));
        return (GridTrackSize[])tracks.Clone();
    }

    /// <summary>
    /// 该项（含 minmax/fit-content/repeat 内部）是否有任一长度满足 <paramref name="predicate"/>。
    /// 每个元素的样式计算都会对 grid-auto-* 调用它，故不用捕获型 lambda（那会在入口分配闭包）。
    /// </summary>
    internal bool AnyLength(Func<Length, bool> predicate)
    {
        switch (Kind)
        {
            case TrackKind.Fixed:
            case TrackKind.FitContent:
                return predicate(Fixed);
            case TrackKind.MinMax:
                return Min.AnyLength(predicate) || Max.AnyLength(predicate);
            case TrackKind.Repeat:
                foreach (var t in ((RepeatPayload)_payload!).Tracks)
                {
                    if (t.AnyLength(predicate)) return true;
                }
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// 把该项（含嵌套部分）中的每个长度经 <paramref name="map"/> 改写（env()/vw/vh 折算用）。
    /// 返回新值，不改动自身——轨道列表常直接来自样式表规则（样式表不可变契约，见 ISSUE-096）。
    /// </summary>
    internal GridTrackSize MapLengths(Func<Length, Length> map)
    {
        switch (Kind)
        {
            case TrackKind.Fixed:
                return FromLength(map(Fixed));
            case TrackKind.FitContent:
                return new GridTrackSize(TrackKind.FitContent, map(Fixed), 0);
            case TrackKind.MinMax:
                return new GridTrackSize(TrackKind.MinMax, default, 0, new[] { Min.MapLengths(map), Max.MapLengths(map) });
            case TrackKind.Repeat:
                var repeat = (RepeatPayload)_payload!;
                var tracks = new GridTrackSize[repeat.Tracks.Length];
                for (int i = 0; i < tracks.Length; i++)
                    tracks[i] = repeat.Tracks[i].MapLengths(map);
                return new GridTrackSize(TrackKind.Repeat, default, 0, new RepeatPayload(repeat.Count, repeat.Mode, tracks));
            default:
                return this;
        }
    }

    /// <summary>CSS 写法，如 <c>minmax(120px, 1fr)</c>、<c>repeat(auto-fill, 100px)</c>、<c>[a b]</c>。</summary>
    public override string ToString() => Kind switch
    {
        TrackKind.Fraction => $"{Fraction}fr",
        TrackKind.Auto => "auto",
        TrackKind.MinContent => "min-content",
        TrackKind.MaxContent => "max-content",
        TrackKind.MinMax => $"minmax({Min}, {Max})",
        TrackKind.FitContent => $"fit-content({Fixed})",
        TrackKind.Repeat => $"repeat({AutoRepeat switch
        {
            GridAutoRepeat.AutoFill => "auto-fill",
            GridAutoRepeat.AutoFit => "auto-fit",
            _ => RepeatCount.ToString(),
        }}, {string.Join(' ', RepeatTracks)})",
        TrackKind.LineNames => $"[{string.Join(' ', Names)}]",
        _ => Fixed.ToString(),
    };
}
