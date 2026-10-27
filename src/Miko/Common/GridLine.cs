namespace Miko.Common;

/// <summary>
/// Grid 子项一侧的放置值（<c>grid-column-start</c> / <c>grid-column-end</c> /
/// <c>grid-row-start</c> / <c>grid-row-end</c>，ISSUE-159）。四选一：
///
/// <list type="bullet">
/// <item><see cref="Auto"/>：自动放置（或默认跨 1 条轨道）；</item>
/// <item>按编号或名字指定一条线：<see cref="Line(int)"/>（1 起始，负数从末尾数，-1 为最后一条显式线）、
/// <see cref="Named(string)"/>（区域名或线名）、<see cref="Named(string, int)"/>（第 n 条叫这个名字的线）；</item>
/// <item>跨度：<see cref="Span(int)"/>、<see cref="Span(string, int)"/>（跨到第 n 条叫这个名字的线）。</item>
/// </list>
///
/// <code>
/// GridColumnStart = GridLine.Line(2)              // grid-column-start: 2
/// GridColumnEnd   = GridLine.Span(2)              // grid-column-end: span 2
/// GridRowStart    = GridLine.Named("header")      // grid-row-start: header（区域 header 的起始线）
/// GridColumnStart = GridLine.Named("col", 2)      // grid-column-start: 2 col
/// GridColumnEnd   = (GridLine)(-1)                // 整数 / 字符串可隐式转换
/// </code>
///
/// <para>名字 <c>foo</c> 用在起始侧时先找名为 <c>foo-start</c> 的线、用在结束侧时先找 <c>foo-end</c>
/// （<c>grid-template-areas</c> 的每个区域都隐式提供这对线），找不到再找名为 <c>foo</c> 的线。名字不够数时，
/// 显式网格之外的隐式线都视为带有该名字（同 CSS）。</para>
/// </summary>
public readonly struct GridLine : IEquatable<GridLine>
{
    private enum LineKind : byte { Auto, Line, Span }

    // 存储布局：8 字节、不含引用（Style / ComputedStyle 各有四个放置槽，槽体积直接计入每个元素的
    // 计算样式，见 StyleStorageSizeTests）。_bits 低 2 位为 LineKind，第 2 位为「写了编号」，
    // 其余为线名在进程级驻留表中的编号（0 = 无名字）。
    private readonly int _number;
    private readonly int _bits;

    private const int KindMask = 0b11;
    private const int NumberGivenBit = 0b100;
    private const int NameShift = 3;

    private GridLine(LineKind kind, int number, string? name, bool numberGiven)
    {
        _number = number;
        _bits = (int)kind
            | (numberGiven ? NumberGivenBit : 0)
            | (name == null ? 0 : GridLineNames.Intern(name) << NameShift);
    }

    private LineKind Kind => (LineKind)(_bits & KindMask);

    private bool NumberGiven => (_bits & NumberGivenBit) != 0;

    /// <summary>自动放置（CSS <c>auto</c>），也是默认值。</summary>
    public static GridLine Auto => default;

    /// <summary>
    /// 第 <paramref name="number"/> 条线（1 起始）；负数从显式网格末尾倒数（-1 为最后一条显式线）。
    /// 超出显式网格的线在隐式网格中按需创建。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> 为 0。</exception>
    public static GridLine Line(int number)
    {
        if (number == 0)
            throw new ArgumentOutOfRangeException(nameof(number), "Grid line 0 does not exist; use GridLine.Auto.");
        return new GridLine(LineKind.Line, number, null, numberGiven: true);
    }

    /// <summary>
    /// 区域名或线名 <paramref name="name"/>（CSS <c>&lt;custom-ident&gt;</c>）：起始侧先找 <c>name-start</c>、
    /// 结束侧先找 <c>name-end</c>，再找第一条名为 <paramref name="name"/> 的线。
    /// </summary>
    public static GridLine Named(string name)
    {
        ValidateName(name, nameof(name));
        return new GridLine(LineKind.Line, 1, name, numberGiven: false);
    }

    /// <summary>
    /// 第 <paramref name="number"/> 条名为 <paramref name="name"/> 的线（CSS <c>&lt;integer&gt; &amp;&amp; &lt;custom-ident&gt;</c>）；
    /// 负数从末尾倒数。
    /// </summary>
    public static GridLine Named(string name, int number)
    {
        ValidateName(name, nameof(name));
        if (number == 0)
            throw new ArgumentOutOfRangeException(nameof(number), "The line index must not be 0.");
        return new GridLine(LineKind.Line, number, name, numberGiven: true);
    }

    /// <summary>跨 <paramref name="count"/> 条轨道（CSS <c>span &lt;integer&gt;</c>）。</summary>
    public static GridLine Span(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "A span must be at least 1.");
        return new GridLine(LineKind.Span, count, null, numberGiven: true);
    }

    /// <summary>
    /// 从另一侧的线出发，跨到第 <paramref name="count"/> 条名为 <paramref name="name"/> 的线
    /// （CSS <c>span &lt;custom-ident&gt;</c>）。两侧都不确定（子项被自动放置）时按跨 1 条处理（同 CSS）。
    /// </summary>
    public static GridLine Span(string name, int count = 1)
    {
        ValidateName(name, nameof(name));
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "A span must be at least 1.");
        return new GridLine(LineKind.Span, count, name, numberGiven: true);
    }

    /// <summary>是否为自动放置。</summary>
    public bool IsAuto => Kind == LineKind.Auto;

    /// <summary>是否为跨度（<c>span …</c>）。</summary>
    public bool IsSpan => Kind == LineKind.Span;

    /// <summary>线编号（负数倒数）、第几条同名线，或跨度；自动放置时为 0。</summary>
    public int Number => _number;

    /// <summary>线名或区域名；按编号指定或纯跨度时为 null。</summary>
    public string? Name => _bits >> NameShift is var id and > 0 ? GridLineNames.Get(id) : null;

    /// <summary>
    /// 是否为只写了名字的形式（<see cref="Named(string)"/>）：此时名字先按区域边线
    /// （<c>name-start</c> / <c>name-end</c>）解析。
    /// </summary>
    internal bool IsBareName => Kind == LineKind.Line && _bits >> NameShift != 0 && !NumberGiven;

    /// <summary>
    /// <c>2</c> → 第 2 条线；<c>0</c> → <see cref="Auto"/>（与 ISSUE-159 之前「0 = 自动放置」的整数写法一致）。
    /// </summary>
    public static implicit operator GridLine(int number) => number == 0 ? Auto : Line(number);

    /// <summary><c>"header"</c> → <see cref="Named(string)"/>。</summary>
    public static implicit operator GridLine(string name) => Named(name);

    internal static void ValidateName(string name, string paramName)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("A grid line name must not be empty.", paramName);
        foreach (var c in name)
        {
            if (char.IsWhiteSpace(c))
                throw new ArgumentException($"A grid line name must not contain whitespace: \"{name}\".", paramName);
        }
        if (name is "span" or "auto")
            throw new ArgumentException($"\"{name}\" is a reserved word and cannot name a grid line.", paramName);
    }

    // 名字已驻留：同名必同编号，按位比较即可。
    public bool Equals(GridLine other) => _number == other._number && _bits == other._bits;

    public override bool Equals(object? obj) => obj is GridLine other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_number, _bits);

    public static bool operator ==(GridLine left, GridLine right) => left.Equals(right);

    public static bool operator !=(GridLine left, GridLine right) => !left.Equals(right);

    /// <summary>CSS 写法：<c>auto</c>、<c>3</c>、<c>header</c>、<c>2 col</c>、<c>span 2</c>、<c>span col</c>。</summary>
    public override string ToString() => Kind switch
    {
        LineKind.Auto => "auto",
        LineKind.Span => Name == null ? $"span {_number}" : _number == 1 ? $"span {Name}" : $"span {_number} {Name}",
        _ => Name == null ? _number.ToString() : NumberGiven ? $"{_number} {Name}" : Name,
    };
}

/// <summary>
/// 网格线名的进程级驻留表：<see cref="GridLine"/> 只存名字的编号，以保持 8 字节、不含引用。
/// 名字按内容寻址（同名即同编号），与 FontManager / TextMeasurer 的进程级缓存同理，跨引擎共享是正确的；
/// 表只随样式里出现过的不同名字增长。写入加锁，读取无锁（数组整体替换后才发布）。
/// </summary>
internal static class GridLineNames
{
    private static readonly object s_lock = new();
    private static readonly Dictionary<string, int> s_ids = new(StringComparer.Ordinal);
    private static string[] s_names = new string[16];
    private static int s_count;

    public static int Intern(string name)
    {
        lock (s_lock)
        {
            if (s_ids.TryGetValue(name, out int id)) return id;

            id = ++s_count;
            var names = s_names;
            if (id >= names.Length)
            {
                var grown = new string[names.Length * 2];
                Array.Copy(names, grown, names.Length);
                names = grown;
            }
            names[id] = name;
            Volatile.Write(ref s_names, names);
            s_ids[name] = id;
            return id;
        }
    }

    public static string Get(int id) => Volatile.Read(ref s_names)[id];
}

/// <summary>
/// <c>grid-column</c> / <c>grid-row</c> 简写的值（ISSUE-159）：起始线与结束线。
/// <code>
/// GridColumn = 2                                 // 2 / auto
/// GridColumn = new(1, 3)                         // 1 / 3
/// GridColumn = new(1, GridLine.Span(2))          // 1 / span 2
/// GridColumn = "content"                         // content / content（区域 content 的两条边线）
/// </code>
/// </summary>
public readonly struct GridPlacement
{
    /// <summary>
    /// 只给起始线：只写名字时结束线取同一个名字（CSS：<c>grid-column: a</c> 即 <c>a / a</c>），否则为 auto。
    /// </summary>
    public GridPlacement(GridLine start)
        : this(start, start.IsBareName ? start : GridLine.Auto)
    {
    }

    public GridPlacement(GridLine start, GridLine end)
    {
        Start = start;
        End = end;
    }

    public GridLine Start { get; }

    public GridLine End { get; }

    public static implicit operator GridPlacement(GridLine start) => new(start);

    public static implicit operator GridPlacement(int start) => new(start);

    public static implicit operator GridPlacement(string name) => new(name);

    public override string ToString() => $"{Start} / {End}";
}
