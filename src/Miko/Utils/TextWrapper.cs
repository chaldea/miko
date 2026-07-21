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
            WhiteSpace.Pre => PreparePreformatted(text), // 保留所有空格和换行（归一换行、展开 Tab）
            WhiteSpace.PreWrap => text, // 保留所有空格和换行
            WhiteSpace.PreLine => CollapseWhitespace(text, preserveNewlines: true),
            _ => text
        };
    }

    /// <summary>
    /// <c>white-space: pre</c> 的预处理：把 <c>\r\n</c> / 孤立 <c>\r</c> 归一为 <c>\n</c>
    /// （Windows 行尾的 Razor 源码会带入 <c>\r</c>，否则会作为多余字形参与测量与绘制），
    /// 并把制表符按 tab-size 8 展开为空格（对齐浏览器默认 <c>tab-size: 8</c>，
    /// SkiaSharp 本身不展开 <c>\t</c>）。无变化时返回原字符串引用，避免逐帧分配。
    /// </summary>
    private static string PreparePreformatted(string text, int tabSize = 8)
    {
        System.Text.StringBuilder? sb = null;
        int column = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r')
            {
                // \r\n 合并为单个 \n（跳过紧随的 \n，主循环会处理不到它）。
                sb ??= new System.Text.StringBuilder(text.Length).Append(text, 0, i);
                sb.Append('\n');
                if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                column = 0;
            }
            else if (c == '\t')
            {
                int spaces = tabSize - (column % tabSize);
                sb ??= new System.Text.StringBuilder(text.Length + spaces).Append(text, 0, i);
                sb.Append(' ', spaces);
                column += spaces;
            }
            else
            {
                sb?.Append(c);
                column = c == '\n' ? 0 : column + 1;
            }
        }
        return sb?.ToString() ?? text;
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
    /// 依据 CSS <c>word-break</c> 与 <c>overflow-wrap</c> 判断是否需要对超过整行宽度的
    /// 连续长单词做逐字符断行。
    /// <list type="bullet">
    /// <item><c>word-break: break-all</c> → 允许在任意字符间断行。</item>
    /// <item><c>overflow-wrap: break-word</c> 或 <c>anywhere</c> → 当单词整体放不下时允许内部断行。</item>
    /// </list>
    /// 其余情况（默认）返回 false，长单词整体溢出。
    /// </summary>
    public static bool ShouldBreakLongWords(WordBreak wordBreak, OverflowWrap overflowWrap)
    {
        if (wordBreak == WordBreak.BreakAll)
            return true;
        if (overflowWrap == OverflowWrap.BreakWord || overflowWrap == OverflowWrap.Anywhere)
            return true;
        return false;
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
    /// <param name="breakLongWords">
    /// 是否对超过整行宽度的连续长单词做逐字符断行（对齐 CSS <c>overflow-wrap: anywhere</c>）。
    /// textarea 等表单控件采用软换行：任何超出内容宽度的内容都应换行而非溢出，故传 true；
    /// 普通文本节点默认 false，仅在单词（空格）边界换行，保持既有行为。
    /// </param>
    /// <returns>分行后的文本列表</returns>
    public static List<string> WrapText(
        string text,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight,
        float availableWidth,
        WhiteSpace whiteSpace,
        bool breakLongWords = false)
    {
        var lines = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            return lines;
        }

        // 不做软换行的情况（pre / nowrap）：仍按显式换行符分行。
        // nowrap 的换行符已被 ProcessText 折叠为空格，split 恒为单行；
        // pre 保留的换行符在这里形成真正的行（修复 pre 多行文本被当作单行绘制的缺陷）。
        if (!ShouldWrap(whiteSpace))
        {
            lines.AddRange(text.Split('\n'));
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

            WrapParagraph(paragraph, fontFamily, fontSize, fontWeight, availableWidth, lines, breakLongWords);
        }

        return lines;
    }

    private static void WrapParagraph(
        string paragraph,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight,
        float availableWidth,
        List<string> lines,
        bool breakLongWords = false)
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
                currentWidth = 0;
            }

            // 单词自身就超过整行宽度：按字符断行（overflow-wrap: anywhere）。
            // 仅在启用 breakLongWords 时执行，否则保持旧行为（长单词整体溢出）。
            float soloWordWidth = TextMeasurer.MeasureTextWidth(word, fontFamily, fontSize, fontWeight);
            if (breakLongWords && currentLine.Length == 0 && soloWordWidth > availableWidth)
            {
                BreakLongWord(word, fontFamily, fontSize, fontWeight, availableWidth, lines, currentLine, ref currentWidth);
                continue;
            }

            // 继续添加到当前行
            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
                currentWidth += TextMeasurer.MeasureTextWidth(" ", fontFamily, fontSize, fontWeight);
            }
            currentLine.Append(word);
            currentWidth += soloWordWidth;
        }

        // 添加最后一行
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }
    }

    /// <summary>
    /// 逐字符断开一个超过整行宽度的长单词，填满每一行。最后一段（未满一行）不落盘，
    /// 而是留在 <paramref name="currentLine"/> 中，以便后续单词能继续拼接在同一行。
    /// </summary>
    private static void BreakLongWord(
        string word,
        string fontFamily,
        float fontSize,
        FontWeight fontWeight,
        float availableWidth,
        List<string> lines,
        System.Text.StringBuilder currentLine,
        ref float currentWidth)
    {
        foreach (var ch in word)
        {
            float charWidth = TextMeasurer.MeasureTextWidth(ch.ToString(), fontFamily, fontSize, fontWeight);

            // 当前行放不下这个字符（且行内已有内容）：换行。
            if (currentLine.Length > 0 && currentWidth + charWidth > availableWidth)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentWidth = 0;
            }

            currentLine.Append(ch);
            currentWidth += charWidth;
        }
    }
}

