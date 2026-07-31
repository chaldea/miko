using Miko.Common;
using Miko.Utils;
using Shouldly;
using Xunit;

namespace Miko.Tests.Utils;

/// <summary>
/// ISSUE-110：断行单元切分（CJK 按字符、拉丁按单词）与 CJK 文本换行的单元测试。
/// </summary>
public class TextBreakUnitTests
{
    // ---- SplitBreakUnits ----------------------------------------------------

    [Fact]
    public void SplitBreakUnits_LatinWords_SplitOnSpaces()
    {
        var units = TextWrapper.SplitBreakUnits("hello world");

        units.Count.ShouldBe(3);
        units[0].Text.ShouldBe("hello");
        units[0].IsSpace.ShouldBeFalse();
        units[0].IsCjk.ShouldBeFalse();
        units[1].Text.ShouldBe(" ");
        units[1].IsSpace.ShouldBeTrue();
        units[2].Text.ShouldBe("world");
    }

    [Fact]
    public void SplitBreakUnits_PureCjk_SplitPerCharacter()
    {
        var units = TextWrapper.SplitBreakUnits("桌面软件");

        units.Count.ShouldBe(4);
        foreach (var unit in units)
        {
            unit.IsCjk.ShouldBeTrue();
            unit.IsSpace.ShouldBeFalse();
        }
        string.Concat(units.ConvertAll(u => u.Text)).ShouldBe("桌面软件");
    }

    [Fact]
    public void SplitBreakUnits_CjkLatinBoundary_SplitsWithoutSpace()
    {
        // CJK 与拉丁字符之间是合法断行点（无需空格）。
        var units = TextWrapper.SplitBreakUnits("ab打cd");

        units.Count.ShouldBe(3);
        units[0].Text.ShouldBe("ab");
        units[1].Text.ShouldBe("打");
        units[1].IsCjk.ShouldBeTrue();
        units[2].Text.ShouldBe("cd");
    }

    [Fact]
    public void SplitBreakUnits_MixedText_PreservesOrder()
    {
        var units = TextWrapper.SplitBreakUnits("Linux 桌面");

        units.Count.ShouldBe(4);
        units[0].Text.ShouldBe("Linux");
        units[1].IsSpace.ShouldBeTrue();
        units[2].Text.ShouldBe("桌");
        units[3].Text.ShouldBe("面");
    }

    [Fact]
    public void SplitBreakUnits_Empty_ReturnsEmpty()
    {
        TextWrapper.SplitBreakUnits("").ShouldBeEmpty();
    }

    // ---- CollapseWhitespacePreservingBoundaries ------------------------------

    [Fact]
    public void Collapse_PreservesBoundarySpaces()
    {
        // 边界空格不能在节点级别修剪（否则文本与相邻行内元素粘连），
        // 行首/行尾消除由换行打包阶段负责。
        TextWrapper.CollapseWhitespacePreservingBoundaries("All ").ShouldBe("All ");
        TextWrapper.CollapseWhitespacePreservingBoundaries(" components").ShouldBe(" components");
        TextWrapper.CollapseWhitespacePreservingBoundaries("a   b").ShouldBe("a b");
        TextWrapper.CollapseWhitespacePreservingBoundaries("a\nb").ShouldBe("a b");
    }

    // ---- WrapText（CJK）-----------------------------------------------------

    [Fact]
    public void WrapText_PureCjk_WrapsPerCharacter()
    {
        string text = "桌面软件的打包并不像想象中那样统一而是每个发行版都有自己的包格式";
        float availableWidth = 300;

        var lines = TextWrapper.WrapText(text, "Arial", 18, FontWeight.Normal, availableWidth, WhiteSpace.Normal);

        lines.Count.ShouldBeGreaterThan(1);
        foreach (var line in lines)
        {
            float w = TextMeasurer.MeasureTextWidth(line, "Arial", 18, FontWeight.Normal);
            w.ShouldBeLessThanOrEqualTo(availableWidth + 0.5f, $"行 \"{line}\" 超出可用宽度");
        }
        // 内容不丢失（纯中文无空格，拼接应还原）。
        string.Concat(lines).ShouldBe(text);
    }

    [Fact]
    public void WrapText_MixedCjkLatin_WrapsWithinWidth()
    {
        string text = "Linux 桌面软件的打包并不像 Windows 的 MSI/EXE 那样统一，而是每个发行版都有自己的包格式。";
        float availableWidth = 300;

        var lines = TextWrapper.WrapText(text, "Arial", 18, FontWeight.Normal, availableWidth, WhiteSpace.Normal);

        lines.Count.ShouldBeGreaterThan(1);
        foreach (var line in lines)
        {
            float w = TextMeasurer.MeasureTextWidth(line, "Arial", 18, FontWeight.Normal);
            w.ShouldBeLessThanOrEqualTo(availableWidth + 0.5f, $"行 \"{line}\" 超出可用宽度");
        }
    }

    [Fact]
    public void WrapText_CjkLongRun_FillsLinesGreedily()
    {
        // 纯中文应逐字填满每一行（而不是整串作为长单词溢出到一行）。
        string text = "每个发行版都有自己的包格式";
        float charWidth = TextMeasurer.MeasureTextWidth("每", "Arial", 18, FontWeight.Normal);
        float availableWidth = charWidth * 4.5f; // 每行约 4 个字（字形宽度不全等，按行宽断言）

        var lines = TextWrapper.WrapText(text, "Arial", 18, FontWeight.Normal, availableWidth, WhiteSpace.Normal);

        lines.Count.ShouldBeGreaterThanOrEqualTo(3, "13 个字、每行约 4 个字宽时应至少换出 3 行");
        // 贪心填充：除最后一行（余数行）外，每行至少 3 个字（4.5 字宽不可能只放得下 2 个）。
        for (int i = 0; i < lines.Count - 1; i++)
        {
            lines[i].Length.ShouldBeGreaterThanOrEqualTo(3);
        }
        foreach (var line in lines)
        {
            TextMeasurer.MeasureTextWidth(line, "Arial", 18, FontWeight.Normal)
                .ShouldBeLessThanOrEqualTo(availableWidth + 0.5f);
        }
        string.Concat(lines).ShouldBe(text, "逐字断行不丢字");
    }

    [Fact]
    public void MeasureTextWithWrap_CjkText_WidthRespectsMaxWidth()
    {
        string text = "如果你希望支持国产操作系统一般有下面几种方案可以选择使用";
        float maxWidth = 200;
        float lineHeight = 25.2f;

        var (width, height) = TextMeasurer.MeasureTextWithWrap(
            text, "Arial", 18, FontWeight.Normal, maxWidth, lineHeight, WhiteSpace.Normal);

        width.ShouldBeLessThanOrEqualTo(maxWidth + 0.5f);
        height.ShouldBeGreaterThan(lineHeight * 1.5f, "长文本应换为多行");
        (height % lineHeight).ShouldBe(0f, 0.01f, "高度应为行高的整数倍");
    }

    [Fact]
    public void MeasureTextWithWrap_CjkWiderThanEnglish_SameCharCountNarrowerWidthForLatin()
    {
        // 中文宽度与英文不同：同字符数下 CJK 文本明显更宽（等宽全角字形）。
        // 换行后的行数应反映真实测量宽度而非按英文字宽估算。
        string cjk = "桌面软件打包格式";
        string latin = "abcdabcd";

        float cjkWidth = TextMeasurer.MeasureTextWidth(cjk, "Arial", 18, FontWeight.Normal);
        float latinWidth = TextMeasurer.MeasureTextWidth(latin, "Arial", 18, FontWeight.Normal);

        cjkWidth.ShouldBeGreaterThan(latinWidth * 1.3f,
            "8 个汉字的宽度应显著大于 8 个拉丁字母（全角 vs 比例字宽）");
    }
}
