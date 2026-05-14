namespace Miko.Styling.Selectors;

/// <summary>
/// 选择器字符串解析器
/// </summary>
[Obsolete("Use Style.For<T>(), Style.Class(), or Style.Id() fluent API instead.")]
public static class SelectorParser
{
    /// <summary>
    /// 解析选择器字符串（如 "button:hover", ".btn:active", "#main:focus"）
    /// </summary>
    public static Selector Parse(string selectorString)
    {
        var parts = new List<Selector>();
        var remaining = selectorString.Trim();

        // 按顺序解析：标签、ID、类、伪类

        // 检查开头是否是标签选择器（无前缀）
        if (!string.IsNullOrEmpty(remaining) &&
            !remaining.StartsWith('.') &&
            !remaining.StartsWith('#') &&
            !remaining.StartsWith(':'))
        {
            var tagEnd = remaining.IndexOfAny(['.', '#', ':']);
            var tagName = tagEnd == -1 ? remaining : remaining[..tagEnd];

            if (!string.IsNullOrEmpty(tagName))
            {
                parts.Add(new TagSelector(tagName));
                remaining = tagEnd == -1 ? "" : remaining[tagEnd..];
            }
        }

        // 解析剩余部分
        while (!string.IsNullOrEmpty(remaining))
        {
            if (remaining.StartsWith('#'))
            {
                var end = FindNextDelimiter(remaining, 1);
                var id = remaining[1..end];
                parts.Add(new IdSelector(id));
                remaining = remaining[end..];
            }
            else if (remaining.StartsWith('.'))
            {
                var end = FindNextDelimiter(remaining, 1);
                var className = remaining[1..end];
                parts.Add(new ClassSelector(className));
                remaining = remaining[end..];
            }
            else if (remaining.StartsWith(':'))
            {
                var end = FindNextDelimiter(remaining, 1);
                var pseudoClass = remaining[1..end].ToLower();
                parts.Add(ParsePseudoClass(pseudoClass));
                remaining = remaining[end..];
            }
            else
            {
                break; // 未知格式
            }
        }

        return parts.Count == 1 ? parts[0] : new CompoundSelector(parts);
    }

    private static int FindNextDelimiter(string s, int start)
    {
        for (int i = start; i < s.Length; i++)
        {
            if (s[i] == '.' || s[i] == '#' || s[i] == ':')
                return i;
        }
        return s.Length;
    }

    private static Selector ParsePseudoClass(string name) => name switch
    {
        "hover" => new HoverSelector(),
        "active" => new ActiveSelector(),
        "focus" => new FocusSelector(),
        "disabled" => new DisabledSelector(),
        "enabled" => new EnabledSelector(),
        _ => throw new ArgumentException($"Unknown pseudo-class: {name}")
    };
}
