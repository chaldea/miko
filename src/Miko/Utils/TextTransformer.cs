using System.Globalization;
using Miko.Common;

namespace Miko.Utils;

/// <summary>
/// 文本变换工具：实现 CSS <c>text-transform</c>（大小写变换）。
/// 在测量与绘制文本前统一应用，确保二者一致。
/// </summary>
public static class TextTransformer
{
    /// <summary>
    /// 按 <paramref name="transform"/> 变换文本大小写。
    /// <list type="bullet">
    /// <item><c>Uppercase</c> → 全部大写。</item>
    /// <item><c>Lowercase</c> → 全部小写。</item>
    /// <item><c>Capitalize</c> → 每个单词首字母大写。</item>
    /// <item><c>None</c> → 原样返回。</item>
    /// </list>
    /// </summary>
    public static string Apply(string? text, TextTransform transform)
    {
        if (string.IsNullOrEmpty(text) || transform == TextTransform.None)
            return text ?? string.Empty;

        // 使用不变文化以保证跨平台/跨区域的一致结果。
        var culture = CultureInfo.InvariantCulture;

        return transform switch
        {
            TextTransform.Uppercase => text.ToUpper(culture),
            TextTransform.Lowercase => text.ToLower(culture),
            TextTransform.Capitalize => Capitalize(text, culture),
            _ => text
        };
    }

    /// <summary>
    /// 将每个单词的首字母大写（单词以空白字符分隔），其余字符保持原样
    /// （与 CSS <c>capitalize</c> 一致：不强制其余字符小写）。
    /// </summary>
    private static string Capitalize(string text, CultureInfo culture)
    {
        var chars = text.ToCharArray();
        bool atWordStart = true;
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (char.IsWhiteSpace(c))
            {
                atWordStart = true;
            }
            else
            {
                if (atWordStart && char.IsLetter(c))
                    chars[i] = char.ToUpper(c, culture);
                atWordStart = false;
            }
        }
        return new string(chars);
    }
}
