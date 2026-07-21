namespace Miko.Highlight;

/// <summary>
/// 内置的轻量语法高亮器（<see cref="ISyntaxHighlighter"/> 的默认实现）：
/// 按语言规则把代码文本切分为带类型的 token 序列。
///
/// 这是一个正则驱动的启发式高亮器（而非完整语法分析器），面向文档场景的代码展示：
/// 支持常见语言（csharp / javascript / typescript / json / xml / css / sql / python / bash
/// 及其常见别名），未知语言返回 null——调用方按普通文本绘制即可。
///
/// 颜色由 <see cref="Theme"/>（默认 VS Light+ 配色）决定；<see cref="CodeTokenType.Plain"/>
/// 区间的文本使用元素自身的颜色。该实现由 DI 容器默认注册（见
/// <c>Hosting.HighlightingExtensions.AddSyntaxHighlighter</c>），应用可重新注册
/// <see cref="ISyntaxHighlighter"/> 以替换实现。
/// </summary>
public class SyntaxHighlighter : ISyntaxHighlighter
{
    /// <summary>当前高亮主题。默认为 <see cref="SyntaxTheme.Default"/>，可按实例替换。</summary>
    public SyntaxTheme Theme { get; set; } = SyntaxTheme.Default;

    /// <summary>
    /// 按语言把 <paramref name="text"/> 切分为高亮 token（按起点升序、互不重叠；
    /// 未覆盖区间为 Plain）。语言未知或文本为空时返回 null。
    /// </summary>
    /// <param name="text">绘制用文本（应已完成 white-space 预处理，保证偏移与绘制一致）。</param>
    /// <param name="language">语言标识，如 "csharp"、"c#"、"json"；大小写与常见别名均可。</param>
    public IReadOnlyList<CodeToken>? Tokenize(string text, string? language)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var rules = GetRules(language);
        return rules?.Tokenize(text);
    }

    /// <summary>
    /// 语言标识（含常见别名）→ 语言规则。未知语言返回 null。
    /// </summary>
    internal static LanguageRules? GetRules(string language)
        => language.Trim().ToLowerInvariant() switch
        {
            "csharp" or "c#" or "cs" => CSharp,
            "javascript" or "js" or "jsx" or "mjs" => JavaScript,
            "typescript" or "ts" or "tsx" => TypeScript,
            "json" or "jsonc" => Json,
            "xml" or "html" or "xhtml" or "svg" or "razor" => Xml,
            "css" or "scss" or "less" => Css,
            "sql" or "mysql" or "pgsql" or "tsql" or "sqlite" => Sql,
            "python" or "py" or "python3" => Python,
            "bash" or "sh" or "shell" or "zsh" => Bash,
            _ => null,
        };

    // ------------------------------------------------------------------
    // 各语言的主正则。命名组优先级（靠左优先）：comment > string > number
    // > keyword > function > type。所有正则延迟构造（首次使用时编译）。
    // ------------------------------------------------------------------

    private const string CComment = @"(?<comment>//[^\n]*)|(?<comment>/\*[\s\S]*?\*/)";
    private const string CNumber = @"(?<number>\b0[xX][0-9a-fA-F][0-9a-fA-F_]*|\b0[bB][01][01_]*|\b\d[\d_]*(?:\.[\d_]+)?(?:[eE][+-]?[\d_]+)?[fFdDmMuUlL]*)";

    private static LanguageRules? s_csharp;
    private static LanguageRules CSharp => s_csharp ??= new LanguageRules(
        CComment
        + @"|(?<string>[@$]{1,2}""(?:[^""]|"""")*""|\$?""(?:\\[\s\S]|[^""\\\n])*""|'(?:\\[\s\S]|[^'\\\n])')"
        + "|" + CNumber
        + @"|(?<keyword>\b(?:abstract|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|dynamic|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|get|goto|if|implicit|in|init|int|interface|internal|is|lock|long|nameof|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|record|ref|required|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|value|var|virtual|void|volatile|when|where|while|with|yield)\b)"
        + @"|(?<function>\b[A-Za-z_][A-Za-z0-9_]*(?=\s*\())"
        + @"|(?<type>\b[A-Z][A-Za-z0-9_]*)");

    private const string JsString = @"(?<string>""(?:\\[\s\S]|[^""\\\n])*""|'(?:\\[\s\S]|[^'\\\n])*'|`(?:\\[\s\S]|[^`\\])*`)";
    private const string JsNumber = @"(?<number>\b0[xX][0-9a-fA-F][0-9a-fA-F_]*|\b0[bB][01][01_]*|\b0[oO][0-7][0-7_]*|\b\d[\d_]*(?:\.[\d_]+)?(?:[eE][+-]?[\d_]+)?n?)";
    private const string JsKeywords = "break|case|catch|class|const|continue|debugger|default|delete|do|else|export|extends|finally|for|function|if|import|in|instanceof|let|new|of|return|super|switch|this|throw|try|typeof|var|void|while|with|yield|async|await|static|get|set|null|true|false|undefined";

    private static LanguageRules? s_javascript;
    private static LanguageRules JavaScript => s_javascript ??= new LanguageRules(
        CComment
        + "|" + JsString
        + "|" + JsNumber
        + @"|(?<keyword>\b(?:" + JsKeywords + @")\b)"
        + @"|(?<function>\b[A-Za-z_$][A-Za-z0-9_$]*(?=\s*\())"
        + @"|(?<type>\b[A-Z][A-Za-z0-9_]*)");

    private static LanguageRules? s_typescript;
    private static LanguageRules TypeScript => s_typescript ??= new LanguageRules(
        CComment
        + "|" + JsString
        + "|" + JsNumber
        + @"|(?<keyword>\b(?:" + JsKeywords + "|interface|type|enum|namespace|declare|implements|readonly|abstract|keyof|infer|is|asserts|public|private|protected|any|unknown|never|string|number|boolean|object|symbol|bigint" + @")\b)"
        + @"|(?<function>\b[A-Za-z_$][A-Za-z0-9_$]*(?=\s*\())"
        + @"|(?<type>\b[A-Z][A-Za-z0-9_]*)");

    private static LanguageRules? s_json;
    private static LanguageRules Json => s_json ??= new LanguageRules(
        @"(?<string>""(?:\\[\s\S]|[^""\\\n])*"")"
        + @"|(?<number>-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)"
        + @"|(?<keyword>\b(?:true|false|null)\b)");

    private static LanguageRules? s_xml;
    private static LanguageRules Xml => s_xml ??= new LanguageRules(
        @"(?<comment><!--[\s\S]*?-->)"
        + @"|(?<string>""[^""\n]*""|'[^'\n]*')"
        + @"|(?<keyword>(?<=</?)[A-Za-z][A-Za-z0-9:._-]*)"
        + @"|(?<type>\b[A-Za-z_][A-Za-z0-9:._-]*(?=\s*=))");

    private static LanguageRules? s_css;
    private static LanguageRules Css => s_css ??= new LanguageRules(
        @"(?<comment>/\*[\s\S]*?\*/)"
        + @"|(?<string>""(?:\\[\s\S]|[^""\\\n])*""|'(?:\\[\s\S]|[^'\\\n])*')"
        + @"|(?<number>#(?:[0-9a-fA-F]{8}\b|[0-9a-fA-F]{6}\b|[0-9a-fA-F]{3,4}\b)|-?\b\d+(?:\.\d+)?(?:px|em|rem|vh|vw|vmin|vmax|%|ms|s|deg|rad|fr|ch|ex|pt|pc|cm|mm|in)?\b)"
        + @"|(?<keyword>@[A-Za-z-]+|!important\b)"
        + @"|(?<function>\b[A-Za-z-][A-Za-z0-9-]*(?=\s*\())"
        + @"|(?<type>\b[A-Za-z-][A-Za-z0-9-]*(?=\s*:(?!:)))");

    private static LanguageRules? s_sql;
    private static LanguageRules Sql => s_sql ??= new LanguageRules(
        @"(?<comment>--[^\n]*|/\*[\s\S]*?\*/)"
        + @"|(?<string>'(?:[^']|'')*'|""(?:[^""]|"""")*"")"
        + @"|(?<number>\b\d+(?:\.\d+)?\b)"
        + @"|(?<keyword>\b(?:select|insert|update|delete|from|where|and|or|not|null|like|in|is|between|exists|as|on|join|inner|left|right|full|outer|cross|group|by|order|having|limit|offset|union|all|distinct|create|table|alter|drop|index|view|into|values|set|primary|key|foreign|references|unique|check|default|constraint|begin|commit|rollback|transaction|case|when|then|else|end|with|asc|desc)\b)"
        + @"|(?<function>\b[A-Za-z_][A-Za-z0-9_]*(?=\s*\())",
        ignoreCase: true);

    private static LanguageRules? s_python;
    private static LanguageRules Python => s_python ??= new LanguageRules(
        "(?<comment>#[^\\n]*)"
        + "|(?<string>[rRbBfFuU]{0,2}(?:\"\"\"[\\s\\S]*?\"\"\"|'''[\\s\\S]*?'''|\"(?:\\\\[\\s\\S]|[^\"\\\\\\n])*\"|'(?:\\\\[\\s\\S]|[^'\\\\\\n])*'))"
        + "|(?<number>\\b0[xX][0-9a-fA-F][0-9a-fA-F_]*|\\b0[bB][01][01_]*|\\b0[oO][0-7][0-7_]*|\\b\\d[\\d_]*(?:\\.[\\d_]+)?(?:[eE][+-]?[\\d_]+)?j?)"
        + "|(?<keyword>\\b(?:and|as|assert|async|await|break|class|continue|def|del|elif|else|except|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|raise|return|try|while|with|yield|True|False|None)\\b)"
        + "|(?<function>\\b[A-Za-z_][A-Za-z0-9_]*(?=\\s*\\())"
        + "|(?<type>\\b[A-Z][A-Za-z0-9_]*)");

    private static LanguageRules? s_bash;
    private static LanguageRules Bash => s_bash ??= new LanguageRules(
        @"(?<comment>(?<![\w$])#[^\n]*)"
        + @"|(?<string>""(?:\\[\s\S]|[^""\\])*""|'[^']*')"
        + @"|(?<number>\b\d+\b)"
        + @"|(?<keyword>\b(?:if|then|else|elif|fi|for|while|until|do|done|case|esac|function|in|select|echo|printf|read|cd|export|local|declare|typeset|return|exit|source|alias|set|unset|shift|test|eval|exec|trap|break|continue|let)\b)"
        + @"|(?<type>\$(?:\{[^}]*\}|[A-Za-z_][A-Za-z0-9_]*|[0-9@#?$!*]))");
}
