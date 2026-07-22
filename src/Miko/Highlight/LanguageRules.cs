using System.Text.RegularExpressions;

namespace Miko.Highlight;

/// <summary>
/// 基于「主正则」的轻量语言规则：一个语言的全部 token 模式合并为单个带命名组的正则，
/// 各命名组按优先级顺序交替（comment &gt; string &gt; number &gt; keyword &gt; function &gt; type），
/// 同一起点上靠左的组优先命中（因此 keyword 先于 function/type，function 先于 type）。
/// 命中的组名映射为 <see cref="CodeTokenType"/>；未匹配的文本区间即为 Plain。
/// </summary>
internal sealed class LanguageRules
{
    private static readonly (string Group, CodeTokenType Type)[] s_groupMap =
    [
        ("comment", CodeTokenType.Comment),
        ("string", CodeTokenType.String),
        ("number", CodeTokenType.Number),
        ("keyword", CodeTokenType.Keyword),
        ("function", CodeTokenType.Function),
        ("type", CodeTokenType.Type),
    ];

    private readonly Regex _regex;

    public LanguageRules(string pattern, bool ignoreCase = false)
    {
        var options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        if (ignoreCase) options |= RegexOptions.IgnoreCase;
        _regex = new Regex(pattern, options);
    }

    /// <summary>
    /// 将文本切分为高亮 token 序列（按起点升序、互不重叠，Plain 区间不产出）。
    /// </summary>
    public List<CodeToken> Tokenize(string text)
    {
        var tokens = new List<CodeToken>();
        foreach (Match match in _regex.Matches(text))
        {
            if (!match.Success || match.Length == 0) continue;
            foreach (var (group, type) in s_groupMap)
            {
                var g = match.Groups[group];
                if (g.Success)
                {
                    tokens.Add(new CodeToken(type, g.Index, g.Length));
                    break;
                }
            }
        }
        return tokens;
    }
}
