using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Miko.Ionic.Components;

/// <summary>
/// The locale-dependent half of Ionic's datetime utilities (format.ts and the locale checks of
/// helpers.ts), on top of .NET's <see cref="CultureInfo"/> instead of <c>Intl.DateTimeFormat</c>.
/// <para>
/// Ionic asks <c>Intl</c> for a set of fields (<c>{ weekday: 'short', month: 'short', day:
/// 'numeric' }</c>) and lets the locale decide order, separators and names. .NET has no such
/// "skeleton" API, only a handful of complete patterns per culture. A pattern for a field set is
/// therefore derived from the culture's own: start from the pattern that has all the fields
/// (<see cref="DateTimeFormatInfo.LongDatePattern"/> for a written month,
/// <see cref="DateTimeFormatInfo.ShortDatePattern"/> for a numeric one, the month/year and
/// month/day patterns where they fit exactly), drop the fields that were not asked for together
/// with the separator that belonged to them, then set each field's width. This keeps the
/// culture's ordering and literals: <c>Wed, Jul 15</c> (en-US), <c>7月15日 周三</c> (zh-CN),
/// <c>Mi, 15. Jul</c> (de-DE), <c>2026년 7월</c> (ko-KR).
/// </para>
/// </summary>
internal sealed partial class DatetimeLocale
{
    private static readonly ConcurrentDictionary<string, DatetimeLocale> Cache = new();

    // A private-use character standing in for an hour .NET cannot format: 0 – 11 (h11) and
    // 1 – 24 (h24). It travels through DateTime.ToString as a quoted literal and is then replaced.
    private const char HourMarker = '\uE000';

    private static readonly string[] HourCycles = ["h11", "h12", "h23", "h24"];

    internal static readonly DatetimeFormat MonthAndYearFormat = new() { Month = "long", Year = "numeric" };
    internal static readonly DatetimeFormat YearFormat = new() { Year = "numeric" };
    internal static readonly DatetimeFormat MonthFormat = new() { Month = "long" };
    internal static readonly DatetimeFormat DayFormat = new() { Day = "numeric" };
    internal static readonly DatetimeFormat TimeFormat = new() { Hour = "numeric", Minute = "numeric" };
    internal static readonly DatetimeFormat HeaderDateFormat = new() { Weekday = "short", Month = "short", Day = "numeric" };
    internal static readonly DatetimeFormat CombinedDateFormat = new() { Month = "short", Day = "numeric", Weekday = "short" };

    // Intl.RelativeTimeFormat(locale, { numeric: 'auto' }).format(0, 'day'), capitalized. .NET has
    // no relative-time data, so the common languages are listed; others fall back to English.
    private static readonly Dictionary<string, string> TodayLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ar"] = "اليوم", ["bg"] = "Днес", ["ca"] = "Avui", ["cs"] = "Dnes", ["da"] = "I dag",
        ["de"] = "Heute", ["el"] = "Σήμερα", ["en"] = "Today", ["es"] = "Hoy", ["et"] = "Täna",
        ["fa"] = "امروز", ["fi"] = "Tänään", ["fr"] = "Aujourd’hui", ["he"] = "היום", ["hi"] = "आज",
        ["hr"] = "Danas", ["hu"] = "Ma", ["id"] = "Hari ini", ["it"] = "Oggi", ["ja"] = "今日",
        ["ko"] = "오늘", ["lt"] = "Šiandien", ["lv"] = "Šodien", ["ms"] = "Hari ini", ["nb"] = "I dag",
        ["nl"] = "Vandaag", ["no"] = "I dag", ["pl"] = "Dzisiaj", ["pt"] = "Hoje", ["ro"] = "Astăzi",
        ["ru"] = "Сегодня", ["sk"] = "Dnes", ["sl"] = "Danes", ["sr"] = "Данас", ["sv"] = "I dag",
        ["th"] = "วันนี้", ["tr"] = "Bugün", ["uk"] = "Сьогодні", ["vi"] = "Hôm nay", ["zh"] = "今天",
    };

    private readonly string? _hourCycleExtension;
    private readonly ConcurrentDictionary<(DatetimeFormat Format, string? HourCycle), string> _patterns = new();

    private DatetimeLocale(CultureInfo culture, string? hourCycleExtension)
    {
        Culture = culture;
        _hourCycleExtension = hourCycleExtension;
    }

    /// <summary>The culture the text is formatted with.</summary>
    public CultureInfo Culture { get; }

    private DateTimeFormatInfo Info => Culture.DateTimeFormat;

    /// <summary>
    /// The locale for Ionic's <c>locale</c> property: a BCP 47 tag such as <c>"es-ES"</c> (Unicode
    /// extensions like <c>-u-hc-h23</c> included), or <c>"default"</c> for the current culture.
    /// An unknown tag falls back to the current culture, the invariant culture to <c>en-US</c>.
    /// </summary>
    public static DatetimeLocale Get(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale) || string.Equals(locale, "default", StringComparison.OrdinalIgnoreCase))
        {
            var current = CultureInfo.CurrentCulture;
            return Cache.GetOrAdd("default:" + current.Name, _ => Create(current, null));
        }

        return Cache.GetOrAdd(locale, static tag =>
        {
            var hourCycle = HourCycleExtension().Match(tag) is { Success: true } m ? m.Groups[1].Value.ToLowerInvariant() : null;
            var culture = TryGetCulture(tag) ?? TryGetCulture(UnicodeExtension().Replace(tag, "")) ?? CultureInfo.CurrentCulture;
            return Create(culture, hourCycle);
        });
    }

    private static DatetimeLocale Create(CultureInfo culture, string? hourCycle)
    {
        // The invariant culture has no real locale data ("2026 July"); a browser always has a
        // locale, so treat it like Ionic's English defaults.
        if (string.IsNullOrEmpty(culture.Name))
            culture = TryGetCulture("en-US") ?? culture;
        return new DatetimeLocale(culture, hourCycle);
    }

    private static CultureInfo? TryGetCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"-u(?:-[a-z0-9]{2,8})*?-hc-(h11|h12|h23|h24)", RegexOptions.IgnoreCase)]
    private static partial Regex HourCycleExtension();

    [GeneratedRegex(@"-u(?:-[a-z0-9]{2,8})+$", RegexOptions.IgnoreCase)]
    private static partial Regex UnicodeExtension();

    // ---- helpers.ts --------------------------------------------------------------------------

    /// <summary>
    /// The hour cycle (Ionic <c>getHourCycle</c>): the explicit one when given, else the
    /// locale's — from a <c>-u-hc-</c> extension or the culture's short time pattern.
    /// </summary>
    public string GetHourCycle(string? hourCycle)
    {
        if (hourCycle is not null && Array.IndexOf(HourCycles, hourCycle) >= 0) return hourCycle;
        if (_hourCycleExtension is not null) return _hourCycleExtension;
        foreach (var token in Tokenize(Info.ShortTimePattern))
        {
            if (token.Field == 'H') return "h23";
            if (token.Field == 'h') return "h12";
        }
        return "h12";
    }

    /// <summary>
    /// True when the locale writes the month before the year (Ionic <c>isMonthFirstLocale</c> with
    /// numeric month and year); <paramref name="beforeDay"/> compares month and day instead.
    /// </summary>
    public bool IsMonthFirst(bool beforeDay = false)
    {
        var other = beforeDay ? 'd' : 'y';
        foreach (var token in Tokenize(beforeDay ? Info.ShortDatePattern : Info.YearMonthPattern))
        {
            if (token.Field == 'M') return true;
            if (token.Field == other && token.Count <= (beforeDay ? 2 : int.MaxValue)) return false;
        }
        return true;
    }

    /// <summary>True when the locale writes the day period before the hour (<c>오전 10:30</c>),
    /// Ionic <c>isLocaleDayPeriodRTL</c>.</summary>
    public bool IsDayPeriodFirst()
    {
        foreach (var token in TwelveHourTimeTokens())
        {
            if (token.Field == 't') return true;
            if (token.Field is 'h' or 'H') return false;
        }
        return false;
    }

    // ---- data.ts -----------------------------------------------------------------------------

    /// <summary>
    /// The weekday labels starting at <paramref name="firstDayOfWeek"/> (Ionic
    /// <c>getDaysOfWeek</c>): short names on iOS (<c>Sun</c>), narrow ones on MD (<c>S</c>).
    /// </summary>
    public IReadOnlyList<string> GetDaysOfWeek(string mode, int firstDayOfWeek)
    {
        var names = mode == "ios" ? Info.AbbreviatedDayNames : NarrowDayNames();
        var days = new string[7];
        for (var i = 0; i < 7; i++)
            days[i] = names[((firstDayOfWeek + i) % 7 + 7) % 7];
        return days;
    }

    // .NET has no narrow day names (ShortestDayNames are "Su", "Mo" in English). Narrow names are
    // one letter: the first letter of the short name, after any prefix all names share
    // ("周日" → "日", "יום א׳" → "א").
    private string[] NarrowDayNames()
    {
        var names = Info.AbbreviatedDayNames;
        var prefix = names[0];
        foreach (var name in names)
        {
            var length = 0;
            while (length < prefix.Length && length < name.Length && prefix[length] == name[length]) length++;
            prefix = prefix[..length];
        }
        if (names.Any(n => n.Length <= prefix.Length)) prefix = string.Empty;

        var narrow = new string[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var rest = names[i][prefix.Length..];
            var first = StringInfo.GetNextTextElementLength(rest) is var n and > 0 ? rest[..n] : rest;
            narrow[i] = Culture.TextInfo.ToUpper(first);
        }
        return narrow;
    }

    /// <summary>Today's label in a date wheel (Ionic <c>getTodayLabel</c>).</summary>
    public string GetTodayLabel()
        => TodayLabels.TryGetValue(Culture.TwoLetterISOLanguageName, out var label) ? label : "Today";

    /// <summary>The localized AM/PM text (Ionic <c>getLocalizedDayPeriod</c>).</summary>
    public string GetLocalizedDayPeriod(string? dayPeriod)
    {
        var designator = dayPeriod == "am" ? Info.AMDesignator : Info.PMDesignator;
        return string.IsNullOrEmpty(designator) ? (dayPeriod ?? string.Empty).ToUpperInvariant() : designator;
    }

    // ---- format.ts ---------------------------------------------------------------------------

    /// <summary>The month name and year, e.g. <c>July 2026</c> (Ionic <c>getMonthAndYear</c>).</summary>
    public string GetMonthAndYear(DatetimeParts parts) => Format(parts, MonthAndYearFormat);

    /// <summary>The day number alone, without the unit some locales add (Ionic <c>getDay</c>).</summary>
    public string GetDay(DatetimeParts parts) => (parts.Day ?? 1).ToString(Culture);

    /// <summary>The year as the locale writes it, e.g. <c>2026年</c> (Ionic <c>getYear</c>).</summary>
    public string GetYear(DatetimeParts parts) => Format(parts, YearFormat);

    /// <summary>
    /// The time for the time button (Ionic <c>getLocalizedTime</c>), in the given hour cycle;
    /// <c>"Invalid Time"</c> when the parts have no hour or minute.
    /// </summary>
    public string GetLocalizedTime(DatetimeParts parts, string hourCycle, DatetimeFormat? format = null)
    {
        if (parts.Hour is null || parts.Minute is null) return "Invalid Time";
        // Only the time is meaningful; any date makes a valid DateTime (Ionic uses 2023-01-01).
        return Format(new DatetimeParts(parts.Year ?? 2023, parts.Month ?? 1, parts.Day ?? 1, parts.Hour, parts.Minute),
            format ?? TimeFormat, hourCycle);
    }

    /// <summary>
    /// Formats parts with the given fields in this locale (Ionic <c>getLocalizedDateTime</c>).
    /// Missing parts default to January 1, 2023, as in Ionic's <c>getNormalizedDate</c>.
    /// </summary>
    public string Format(DatetimeParts parts, DatetimeFormat format, string? hourCycle = null)
    {
        var cycle = GetHourCycle(hourCycle);
        var year = Math.Clamp(parts.Year ?? 2023, 1, 9999);
        var month = Math.Clamp(parts.Month ?? 1, 1, 12);
        var day = Math.Clamp(parts.Day ?? 1, 1, DateTime.DaysInMonth(year, month));
        var hour = Math.Clamp(parts.Hour ?? 0, 0, 23);
        var minute = Math.Clamp(parts.Minute ?? 0, 0, 59);
        var date = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

        if (format.Pattern is { } custom)
            return Normalize(date.ToString(custom, Culture));

        var pattern = _patterns.GetOrAdd((format, cycle), static (key, self) => self.BuildPattern(key.Format, key.HourCycle!), this);
        var text = date.ToString(pattern.Length == 1 ? "%" + pattern : pattern, Culture);
        if (text.IndexOf(HourMarker) >= 0)
        {
            // h11 counts 0 – 11, h24 counts 1 – 24; both pad like their 12/24-hour siblings.
            var shown = cycle == "h11" ? hour % 12 : hour == 0 ? 24 : hour;
            var digits = cycle == "h24" || format.Hour == "2-digit" ? "00" : "0";
            text = text.Replace(HourMarker.ToString(), shown.ToString(digits, CultureInfo.InvariantCulture));
        }
        return Normalize(text);
    }

    // CLDR 42+ separates "1:40 PM" with a narrow no-break space (U+202F). Fonts often lack it,
    // which would send the whole label through font fallback; a no-break space keeps the label on
    // one line just the same.
    private static string Normalize(string text) => text.Replace('\u202F', '\u00A0');

    // ---- pattern derivation ------------------------------------------------------------------

    private string BuildPattern(DatetimeFormat format, string hourCycle)
    {
        var date = BuildDatePattern(format);
        var time = BuildTimePattern(format, hourCycle);
        if (date.Length == 0) return time;
        if (time.Length == 0) return date;
        return date + " " + time;
    }

    private string BuildDatePattern(DatetimeFormat o)
    {
        bool weekday = o.Weekday is not null, year = o.Year is not null, month = o.Month is not null, day = o.Day is not null;
        if (!weekday && !year && !month && !day) return string.Empty;

        // A field on its own is the field itself: month names stand alone (七月, not 7月 of 2026年7月).
        if (month && !weekday && !year && !day) return Join([WithWidth(new Token('M', 4, ""), o)]);
        if (weekday && !year && !month && !day) return Join([WithWidth(new Token('d', 4, ""), o)]);

        var numericMonth = o.Month is "numeric" or "2-digit";
        string source;
        if (month && numericMonth) source = Info.ShortDatePattern;
        else if (year && !day && !weekday) source = Info.YearMonthPattern;
        else if (day && !year && !month && !weekday) source = Info.MonthDayPattern;
        else source = Info.LongDatePattern;

        var tokens = Reduce(Tokenize(source), t => t.Kind switch
        {
            FieldKind.Weekday => weekday,
            FieldKind.Year => year,
            FieldKind.Month => month,
            FieldKind.Day => day,
            _ => false,
        });
        for (var i = 0; i < tokens.Count; i++)
            if (!tokens[i].IsLiteral) tokens[i] = WithWidth(tokens[i], o);

        // The numeric date patterns have no weekday: place it as the long date pattern does.
        if (weekday && !tokens.Any(t => t.Kind == FieldKind.Weekday))
        {
            var longDate = Tokenize(Info.LongDatePattern);
            var index = longDate.FindIndex(t => t.Kind == FieldKind.Weekday);
            var weekdayToken = WithWidth(new Token('d', 4, ""), o);
            var firstField = longDate.FindIndex(t => !t.IsLiteral);
            if (index >= 0 && index == firstField)
            {
                var separator = index + 1 < longDate.Count && longDate[index + 1].IsLiteral ? longDate[index + 1] : Literal(" ");
                tokens.InsertRange(0, [weekdayToken, separator]);
            }
            else
            {
                var separator = index > 0 && longDate[index - 1].IsLiteral ? longDate[index - 1] : Literal(" ");
                tokens.AddRange([separator, weekdayToken]);
            }
        }

        return Join(tokens);
    }

    private string BuildTimePattern(DatetimeFormat o, string hourCycle)
    {
        bool hour = o.Hour is not null, minute = o.Minute is not null;
        if (!hour && !minute) return string.Empty;

        var twelve = hourCycle is "h11" or "h12";
        var tokens = twelve ? TwelveHourTimeTokens() : Tokenize(Info.ShortTimePattern);
        var cultureUses24 = tokens.Any(t => t.Field == 'H');

        tokens = Reduce(tokens, t => t.Kind switch
        {
            FieldKind.Hour => hour,
            FieldKind.Minute => minute,
            FieldKind.DayPeriod => twelve && hour,
            _ => false,
        });

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind == FieldKind.Hour)
            {
                tokens[i] = hourCycle switch
                {
                    "h12" => new Token('h', o.Hour == "2-digit" ? 2 : 1, ""),
                    // Ionic pads 24-hour hours (getFormattedHour); keep a culture's own H:mm.
                    "h23" => new Token('H', o.Hour == "2-digit" || !cultureUses24 ? 2 : Math.Min(token.Count, 2), ""),
                    _ => Literal("'" + HourMarker + "'"),
                };
            }
            else if (token.Kind == FieldKind.Minute)
            {
                tokens[i] = new Token('m', hour || o.Minute == "2-digit" ? 2 : 1, "");
            }
            else if (token.Kind == FieldKind.DayPeriod)
            {
                tokens[i] = new Token('t', 2, "");
            }
        }

        return Join(tokens);
    }

    /// <summary>
    /// The culture's time pattern in 12-hour form. A 24-hour culture has no day-period
    /// placement of its own: Chinese and Japanese put it before the hour, others after.
    /// </summary>
    private List<Token> TwelveHourTimeTokens()
    {
        var tokens = Tokenize(Info.ShortTimePattern);
        if (tokens.Any(t => t.Field == 'h') && tokens.Any(t => t.Field == 't')) return tokens;

        tokens = Reduce(tokens, t => t.Kind != FieldKind.DayPeriod);
        for (var i = 0; i < tokens.Count; i++)
            if (tokens[i].Field == 'H') tokens[i] = new Token('h', 1, "");

        if (Culture.TwoLetterISOLanguageName is "zh" or "ja" or "ko")
            tokens.Insert(0, new Token('t', 2, ""));
        else
            tokens.AddRange([Literal("\u00A0"), new Token('t', 2, "")]);
        return tokens;
    }

    private static Token WithWidth(Token token, DatetimeFormat o) => token.Kind switch
    {
        FieldKind.Weekday => new Token('d', o.Weekday == "long" ? 4 : 3, ""),
        FieldKind.Day => new Token('d', o.Day == "2-digit" ? 2 : 1, ""),
        FieldKind.Year => new Token('y', o.Year == "2-digit" ? 2 : 4, ""),
        FieldKind.Month => o.Month switch
        {
            "numeric" => new Token('M', 1, ""),
            "2-digit" => new Token('M', 2, ""),
            // A culture that writes a numeric month with a unit (7月) keeps it, as Intl does.
            _ when token.Count <= 2 => token,
            "long" => new Token('M', 4, ""),
            _ => new Token('M', 3, ""),
        },
        _ => token,
    };

    // ---- .NET custom format pattern tokens ---------------------------------------------------

    private enum FieldKind { None, Weekday, Day, Month, Year, Hour, Minute, Second, DayPeriod, Other }

    /// <summary>A run of one format letter, or literal text exactly as written (quotes kept).</summary>
    private readonly record struct Token(char Field, int Count, string Raw)
    {
        public bool IsLiteral => Field == '\0';

        public FieldKind Kind => Field switch
        {
            '\0' => FieldKind.None,
            'd' => Count >= 3 ? FieldKind.Weekday : FieldKind.Day,
            'M' => FieldKind.Month,
            'y' => FieldKind.Year,
            'h' or 'H' => FieldKind.Hour,
            'm' => FieldKind.Minute,
            's' => FieldKind.Second,
            't' => FieldKind.DayPeriod,
            _ => FieldKind.Other,
        };
    }

    private static Token Literal(string raw) => new('\0', 0, raw);

    private static bool IsFieldChar(char c) => c is 'd' or 'f' or 'F' or 'g' or 'h' or 'H' or 'K' or 'm' or 'M' or 's' or 't' or 'y' or 'z';

    private static List<Token> Tokenize(string pattern)
    {
        var tokens = new List<Token>();
        var literal = new StringBuilder();
        var i = 0;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (IsFieldChar(c))
            {
                Flush();
                var end = i;
                while (end < pattern.Length && pattern[end] == c) end++;
                tokens.Add(new Token(c, end - i, ""));
                i = end;
            }
            else if (c is '\'' or '"')
            {
                var end = pattern.IndexOf(c, i + 1);
                if (end < 0) end = pattern.Length - 1;
                literal.Append(pattern, i, end - i + 1);
                i = end + 1;
            }
            else if (c == '\\' && i + 1 < pattern.Length)
            {
                literal.Append(pattern, i, 2);
                i += 2;
            }
            else if (c == '%')
            {
                i++;
            }
            else
            {
                literal.Append(c);
                i++;
            }
        }
        Flush();
        return tokens;

        void Flush()
        {
            if (literal.Length == 0) return;
            tokens.Add(Literal(literal.ToString()));
            literal.Clear();
        }
    }

    private static string Join(List<Token> tokens)
    {
        var builder = new StringBuilder();
        foreach (var token in tokens)
            builder.Append(token.IsLiteral ? token.Raw : new string(token.Field, token.Count));
        return builder.ToString();
    }

    /// <summary>The literal's text with quotes and escapes resolved.</summary>
    private static string Unescape(string raw)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (c is '\'' or '"') continue;
            if (c == '\\' && i + 1 < raw.Length) c = raw[++i];
            builder.Append(c);
        }
        return builder.ToString();
    }

    private static bool HasLetter(Token literal) => Unescape(literal.Raw).Any(char.IsLetter);

    /// <summary>
    /// Removes every field <paramref name="keep"/> rejects, together with the literal that
    /// belonged to it: a unit written after it (<c>2026年</c>, <c>2026 г.</c>), a separator
    /// between it and the next field, or — at the end — the separator before it. A unit written
    /// right after the previous field (<c>2026년 </c>) stays with that field.
    /// </summary>
    private static List<Token> Reduce(List<Token> source, Func<Token, bool> keep)
    {
        var tokens = new List<Token>(source);
        while (true)
        {
            var i = tokens.FindIndex(t => !t.IsLiteral && !keep(t));
            if (i < 0) break;

            var next = i + 1 < tokens.Count && tokens[i + 1].IsLiteral ? i + 1 : -1;
            var prev = i > 0 && tokens[i - 1].IsLiteral ? i - 1 : -1;
            if (next >= 0 && (HasLetter(tokens[next]) || prev >= 0))
            {
                tokens.RemoveAt(next);
            }
            else if (prev >= 0)
            {
                var raw = tokens[prev].Raw;
                var attached = 0;
                while (attached < raw.Length && char.IsLetter(raw[attached])) attached++;
                if (attached > 0)
                {
                    tokens[prev] = Literal(raw[..attached]);
                }
                else
                {
                    tokens.RemoveAt(prev);
                    i--;
                }
            }
            else if (next >= 0)
            {
                tokens.RemoveAt(next);
            }
            tokens.RemoveAt(i);
        }

        while (tokens.Count > 0 && tokens[0].IsLiteral && !HasLetter(tokens[0])) tokens.RemoveAt(0);
        while (tokens.Count > 0 && tokens[^1].IsLiteral && !HasLetter(tokens[^1])) tokens.RemoveAt(tokens.Count - 1);
        return tokens;
    }
}
