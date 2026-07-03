using Miko.Common;
using Miko.Fonts;
using SkiaSharp;

namespace Miko.Utils;

/// <summary>
/// 文本换行工具，根据 WhiteSpace 属性处理文本的空格、换行和自动换行
/// </summary>
public static class TextWrapper
{
    /// <summary>
    /// 处理文本并根据 WhiteSpace 属性进行预处理（空格折叠、换行保留等）
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="whiteSpace">WhiteSpace 样式</param>
    /// <returns>处理后的文本</returns>
    public static string ProcessText(string text, WhiteSpace whiteSpace)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return whiteSpace switch
        {
            WhiteSpace.Normal => CollapseWhitespace(text, preserveNewlines: false),
            WhiteSpace.Nowrap => CollapseWhitespace(text, preserveNewlines: false),
            WhiteSpace.Pre => text, // 保留所有空格和换行
            WhiteSpace.PreWrap => text, // 保留所有空格和换行
            WhiteSpace.PreLine => CollapseWhitespace(text, preserveNewlines: true),
            _ => text
        };
    }

    /// <summary>
    /// 折叠连续的空白字符
    /// </summary>
    private static string CollapseWhitespace(string text, bool preserveNewlines)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var result = new System.Text.StringBuilder(text.Length);
        bool lastWasSpace = false;
        bool lastWasNewline = false;

        foreach (char c in text)
        {
            if (c == '\n' || c == '\r')
            {
                if (preserveNewlines)
                {
                    // 保留换行符，但折叠连续的换行符为单个
                    if (!lastWasNewline)
                    {
                        result.Append('\n');
                        lastWasNewline = true;
                        lastWasSpace = false;
                    }
                    if (c == '\r' && result.Length > 0 && result[result.Length - 1] == '\n')
                    {
                        // 跳过 \r\n 中的 \r
                        continue;
                    }
                }
                else
                {
                    // 换行符视为空格
                    if (!lastWasSpace)
                    {
                        result.Append(' ');
                        lastWasSpace = true;
                    }
                    lastWasNewline = false;
                }
            }
            else if (char.IsWhiteSpace(c))
            {
                // 其他空白字符（空格、制表符等）
                if (!lastWasSpace)
                {
                    result.Append(' ');
                    lastWasSpace = true;
                }
                lastWasNewline = false;
            }
            else
            {
                // 普通字符
                result.Append(c);
                lastWasSpace = false;
                lastWasNewline = false;
            }
        }

        // 去除首尾空白
        string processed = result.ToString().Trim();
        return processed;
    }

    /// <summary>
    /// 判断是否允许自动换行
    /// </summary>
    public static bool ShouldWrap(WhiteSpace whiteSpace)
    {
        return whiteSpace switch
        {
            WhiteSpace.Normal => true,
            WhiteSpace.Nowrap => false,
            WhiteSpace.Pre => false,
            WhiteSpace.PreWrap => true,
            WhiteSpace.PreLine => true,
            _ => true
        };
    }

    /// <summary>
    /// 将文本按照可用宽度分行
    /// </summary>
    /// <param name="text">要分行的文本（已经过 ProcessText 处理）</param>
    /// <param name="fontFamily">字体族</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="fontWeight">字体粗细</param>
    /// <param name="availableWidth">可用宽度</param>
    /// <param name="whiteSpace">WhiteSpace 样式</param>
    /// <returns>分行后的文本列表</returns>
    public static List<string> WrapText(
        string text,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight,
        float availableWidth,
        WhiteSpace whiteSpace)
    {
        var lines = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            return lines;
        }

        // 不换行的情况
        if (!ShouldWrap(whiteSpace))
        {
            lines.Add(text);
            return lines;
        }

        // 处理显式换行符（pre-wrap 和 pre-line 模式）
        var paragraphs = text.Split('\n');

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add("");
                continue;
            }

            WrapParagraph(paragraph, fontFamily, fontSize, fontWeight, availableWidth, lines);
        }

        return lines;
    }

    private static void WrapParagraph(
        string paragraph,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight,
        float availableWidth,
        List<string> lines)
    {
        if (availableWidth <= 0)
        {
            lines.Add(paragraph);
            return;
        }

        // 测量整行文本
        var (fullWidth, _) = TextMeasurer.MeasureText(paragraph, fontFamily, fontSize, fontWeight);

        // 如果整行都能放下，直接添加
        if (fullWidth <= availableWidth)
        {
            lines.Add(paragraph);
            return;
        }

        // 需要换行：按单词（空格分隔）切分
        var words = paragraph.Split(' ', StringSplitOptions.None);
        var currentLine = new System.Text.StringBuilder();
        float currentWidth = 0;

        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var wordToMeasure = (currentLine.Length > 0 ? " " : "") + word;
            var (wordWidth, _) = TextMeasurer.MeasureText(wordToMeasure, fontFamily, fontSize, fontWeight);

            // 如果加上这个单词后超出宽度
            if (currentWidth + wordWidth > availableWidth && currentLine.Length > 0)
            {
                // 保存当前行并开始新行
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentLine.Append(word);
                currentWidth = TextMeasurer.MeasureTextWidth(word, fontFamily, fontSize, fontWeight);
            }
            else
            {
                // 继续添加到当前行
                if (currentLine.Length > 0)
                {
                    currentLine.Append(' ');
                }
                currentLine.Append(word);
                currentWidth += wordWidth;
            }
        }

        // 添加最后一行
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }
    }
}

