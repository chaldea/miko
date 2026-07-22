namespace Miko.Highlight;

/// <summary>
/// 语法高亮器接口：把代码文本按语言切分为带类型的 token 序列。
///
/// 默认实现为内置的 <see cref="SyntaxHighlighter"/>（正则驱动，支持常见语言），
/// 由 <c>MikoAppBuilder.CreateDefault</c> 注册进 DI 容器。需要自定义高亮（配色、
/// 新语言、或接入 TextMate 等完整语法分析）时，实现本接口并重新注册即可覆盖：
/// <code>builder.Services.AddSingleton&lt;ISyntaxHighlighter, MyHighlighter&gt;();</code>
/// 或使用便捷扩展 <c>builder.UseSyntaxHighlighter&lt;MyHighlighter&gt;()</c>。
/// </summary>
public interface ISyntaxHighlighter
{
    /// <summary>当前高亮主题（各 token 类型的绘制颜色）。</summary>
    SyntaxTheme Theme { get; }

    /// <summary>
    /// 按语言把 <paramref name="text"/> 切分为高亮 token（按起点升序、互不重叠；
    /// 未覆盖区间为 <see cref="CodeTokenType.Plain"/>）。语言未知或文本为空时返回
    /// null——调用方按普通文本绘制。
    /// </summary>
    /// <param name="text">绘制用文本（应已完成 white-space 预处理，保证偏移与绘制一致）。</param>
    /// <param name="language">语言标识，如 "csharp"、"json"。</param>
    IReadOnlyList<CodeToken>? Tokenize(string text, string? language);
}
