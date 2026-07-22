using Miko.Common;

namespace Miko.Highlight;

/// <summary>
/// 语法高亮主题：各 token 类型的绘制颜色。
/// 默认主题为 VS Light+ 配色（文档场景通常为浅色背景）。
/// </summary>
public sealed class SyntaxTheme
{
    public Color Keyword { get; init; }
    public Color String { get; init; }
    public Color Comment { get; init; }
    public Color Number { get; init; }
    public Color Type { get; init; }
    public Color Function { get; init; }

    /// <summary>获取 token 类型的绘制颜色；Plain 返回 null（使用元素自身颜色）。</summary>
    public Color? ColorFor(CodeTokenType type) => type switch
    {
        CodeTokenType.Keyword => Keyword,
        CodeTokenType.String => String,
        CodeTokenType.Comment => Comment,
        CodeTokenType.Number => Number,
        CodeTokenType.Type => Type,
        CodeTokenType.Function => Function,
        _ => (Color?)null,
    };

    /// <summary>默认主题（VS Light+ 配色）。</summary>
    public static SyntaxTheme Default { get; } = new()
    {
        Keyword = Color.FromRgb(0x00, 0x00, 0xFF),
        String = Color.FromRgb(0xA3, 0x15, 0x15),
        Comment = Color.FromRgb(0x00, 0x80, 0x00),
        Number = Color.FromRgb(0x09, 0x86, 0x58),
        Type = Color.FromRgb(0x2B, 0x91, 0xAF),
        Function = Color.FromRgb(0x79, 0x5E, 0x26),
    };
}
