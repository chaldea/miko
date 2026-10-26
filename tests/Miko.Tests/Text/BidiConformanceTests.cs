using System.Globalization;
using Miko.Text.Bidi;
using Shouldly;

namespace Miko.Tests.Text;

/// <summary>
/// UAX #9 conformance (ISSUE-157) against the Unicode test files. The repository carries subsets
/// (tools/Miko.UnicodeGen); set <c>MIKO_UCD_DIR</c> to a directory holding the full
/// <c>BidiTest.txt</c> / <c>BidiCharacterTest.txt</c> to run every case.
/// </summary>
public class BidiConformanceTests
{
    [Fact]
    public void Should_Match_BidiCharacterTest()
    {
        var failures = new List<string>();
        int cases = 0;
        foreach (var raw in ReadTestFile("BidiCharacterTest"))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var fields = line.Split(';');
            var codePoints = fields[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(h => int.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            int direction = int.Parse(fields[1], CultureInfo.InvariantCulture);
            int expectedParagraphLevel = int.Parse(fields[2], CultureInfo.InvariantCulture);
            var expectedLevels = fields[3].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var expectedOrder = fields[4].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

            var types = codePoints.Select(UnicodeBidiData.GetBidiClass).ToArray();
            var paragraph = new BidiParagraph(types, codePoints, direction == 2 ? -1 : direction);
            cases++;
            var problem = Compare(paragraph, expectedLevels, expectedOrder);
            if (paragraph.ParagraphLevel != expectedParagraphLevel) problem ??= $"paragraph level {paragraph.ParagraphLevel}";
            if (problem != null && failures.Count < 20) failures.Add($"{line} => {problem}");
        }

        cases.ShouldBeGreaterThan(1000);
        failures.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Match_BidiTest()
    {
        var failures = new List<string>();
        int cases = 0;
        string[] levels = [];
        int[] order = [];
        foreach (var raw in ReadTestFile("BidiTest"))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            if (line.StartsWith("@Levels:", StringComparison.Ordinal))
            {
                levels = line["@Levels:".Length..].Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                continue;
            }
            if (line.StartsWith("@Reorder:", StringComparison.Ordinal))
            {
                order = line["@Reorder:".Length..].Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse).ToArray();
                continue;
            }
            if (line[0] == '@') continue;

            var fields = line.Split(';');
            var types = fields[0].Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                .Select(Enum.Parse<BidiClass>).ToArray();
            int bitset = int.Parse(fields[1].Trim(), CultureInfo.InvariantCulture);
            foreach (var (bit, paragraphLevel) in new[] { (1, -1), (2, 0), (4, 1) })
            {
                if ((bitset & bit) == 0) continue;
                var paragraph = new BidiParagraph(types, [], paragraphLevel);
                cases++;
                var problem = Compare(paragraph, levels, order);
                if (problem != null && failures.Count < 20) failures.Add($"{line} (level {paragraphLevel}) => {problem}");
            }
        }

        cases.ShouldBeGreaterThan(5000);
        failures.ShouldBeEmpty();
    }

    /// <summary>Levels, ignoring the positions marked x (removed by X9), and the visual order without them.</summary>
    private static string? Compare(BidiParagraph paragraph, string[] expectedLevels, int[] expectedOrder)
    {
        var actual = paragraph.GetLevels([expectedLevels.Length]);
        for (int i = 0; i < expectedLevels.Length; i++)
        {
            if (expectedLevels[i] == "x") continue;
            if (actual[i].ToString(CultureInfo.InvariantCulture) != expectedLevels[i])
                return $"levels {string.Join(' ', actual)}";
        }

        var visual = BidiParagraph.ComputeReordering(actual).Where(i => expectedLevels[i] != "x").ToArray();
        return visual.SequenceEqual(expectedOrder) ? null : $"order {string.Join(' ', visual)}";
    }

    private static IEnumerable<string> ReadTestFile(string name)
    {
        var fullDir = Environment.GetEnvironmentVariable("MIKO_UCD_DIR");
        var path = !string.IsNullOrEmpty(fullDir) && File.Exists(Path.Combine(fullDir, name + ".txt"))
            ? Path.Combine(fullDir, name + ".txt")
            : Path.Combine(AppContext.BaseDirectory, "TestAssets", "Unicode", name + ".subset.txt");
        return File.ReadLines(path);
    }
}
