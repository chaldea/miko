using System.Globalization;
using System.Text;

// Generates the Unicode Bidirectional Algorithm data that Miko compiles in (ISSUE-157), and the
// conformance-test subsets that tests/Miko.Tests runs against it.
//
//   dotnet run --project tools/Miko.UnicodeGen                  download UCD 15.1.0 and regenerate
//   dotnet run --project tools/Miko.UnicodeGen -- --ucd <dir>   use already-downloaded UCD files
//
// The version is pinned to the one HarfBuzzSharp 8.3 (the shaper Miko uses) was built with, so the
// bidi classes and the shaper's script / mirroring data never disagree. Bump both together.

const string UnicodeVersion = "15.1.0";

var repoRoot = FindRepoRoot();
var ucdIndex = Array.IndexOf(args, "--ucd");
var ucdDir = ucdIndex >= 0 && ucdIndex + 1 < args.Length ? args[ucdIndex + 1] : null;

string[] files = ["extracted/DerivedBidiClass.txt", "BidiBrackets.txt", "BidiCharacterTest.txt", "BidiTest.txt"];
var content = new Dictionary<string, string>();
using (var http = new HttpClient())
{
    foreach (var file in files)
    {
        var name = Path.GetFileName(file);
        content[name] = ucdDir != null
            ? File.ReadAllText(Path.Combine(ucdDir, name))
            : await http.GetStringAsync($"https://www.unicode.org/Public/{UnicodeVersion}/ucd/{file}");
    }
}

var source = BidiDataWriter.Write(content["DerivedBidiClass.txt"], content["BidiBrackets.txt"], UnicodeVersion);
WriteFile(Path.Combine(repoRoot, "src/Miko/Text/Bidi/UnicodeBidiData.g.cs"), source);

var testDir = Path.Combine(repoRoot, "tests/Miko.Tests/TestAssets/Unicode");
WriteFile(Path.Combine(testDir, "BidiCharacterTest.subset.txt"), TestSubsets.CharacterTest(content["BidiCharacterTest.txt"]));
WriteFile(Path.Combine(testDir, "BidiTest.subset.txt"), TestSubsets.ClassTest(content["BidiTest.txt"]));

static void WriteFile(string path, string text)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, text.Replace("\r\n", "\n"));
    Console.WriteLine($"Wrote {path}");
}

static string FindRepoRoot()
{
    foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "miko.slnx")))
                return directory.FullName;
        }
    }
    throw new InvalidOperationException("Could not find the repository root (miko.slnx).");
}

static class BidiDataWriter
{
    // Order must match Miko.Text.Bidi.BidiClass: the generated table stores these indices.
    private static readonly string[] ShortNames =
        ["L", "R", "AL", "EN", "ES", "ET", "AN", "CS", "NSM", "BN", "B", "S", "WS", "ON",
         "LRE", "LRO", "RLE", "RLO", "PDF", "LRI", "RLI", "FSI", "PDI"];

    private static readonly Dictionary<string, string> LongNames = new()
    {
        ["Left_To_Right"] = "L", ["Right_To_Left"] = "R", ["Arabic_Letter"] = "AL",
        ["European_Number"] = "EN", ["European_Separator"] = "ES", ["European_Terminator"] = "ET",
        ["Arabic_Number"] = "AN", ["Common_Separator"] = "CS", ["Nonspacing_Mark"] = "NSM",
        ["Boundary_Neutral"] = "BN", ["Paragraph_Separator"] = "B", ["Segment_Separator"] = "S",
        ["White_Space"] = "WS", ["Other_Neutral"] = "ON",
    };

    public static string Write(string derivedBidiClass, string bidiBrackets, string version)
    {
        var classes = new byte[0x110000];
        foreach (var raw in derivedBidiClass.Split('\n'))
        {
            var line = raw.Trim();
            string range, value;
            if (line.StartsWith("# @missing:", StringComparison.Ordinal))
            {
                // Defaults for unassigned code points, listed before the data they underlie.
                var parts = line["# @missing:".Length..].Split(';');
                range = parts[0].Trim();
                value = LongNames[parts[1].Trim()];
            }
            else if (line.Length > 0 && line[0] != '#')
            {
                var parts = line.Split('#')[0].Split(';');
                range = parts[0].Trim();
                value = parts[1].Trim();
            }
            else
            {
                continue;
            }

            var (start, end) = ParseRange(range);
            var index = (byte)Array.IndexOf(ShortNames, value);
            Array.Fill(classes, index, start, end - start + 1);
        }

        var starts = new List<int>();
        var values = new List<byte>();
        for (int cp = 0; cp < classes.Length; cp++)
        {
            if (cp == 0 || classes[cp] != classes[cp - 1])
            {
                starts.Add(cp);
                values.Add(classes[cp]);
            }
        }

        var brackets = new List<(int Cp, int Pair, bool Open)>();
        foreach (var raw in bidiBrackets.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim();
            if (line.Length == 0) continue;
            var parts = line.Split(';');
            brackets.Add((ParseHex(parts[0]), ParseHex(parts[1]), parts[2].Trim() == "o"));
        }
        brackets.Sort((a, b) => a.Cp.CompareTo(b.Cp));

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine($"// Generated by tools/Miko.UnicodeGen from the Unicode Character Database {version}");
        sb.AppendLine("// (DerivedBidiClass.txt, BidiBrackets.txt). Do not edit by hand; rerun the tool.");
        sb.AppendLine("// Unicode data © Unicode, Inc., used under the Unicode License v3 (see THIRD-PARTY-NOTICES.txt).");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("namespace Miko.Text.Bidi;");
        sb.AppendLine();
        sb.AppendLine("internal static partial class UnicodeBidiData");
        sb.AppendLine("{");
        sb.AppendLine($"    public const string UnicodeVersion = \"{version}\";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>First code point of each run of equal Bidi_Class values, ascending.</summary>");
        sb.AppendLine("    private static ReadOnlySpan<int> RangeStarts => new int[]");
        sb.AppendLine("    {");
        AppendRows(sb, starts.Select(s => $"0x{s:X5}"), 10);
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>The <see cref=\"BidiClass\"/> of the run starting at the same index of <see cref=\"RangeStarts\"/>.</summary>");
        sb.AppendLine("    private static ReadOnlySpan<byte> RangeClasses => new byte[]");
        sb.AppendLine("    {");
        AppendRows(sb, values.Select(v => v.ToString(CultureInfo.InvariantCulture)), 24);
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Code points with a Bidi_Paired_Bracket_Type, ascending (BidiBrackets.txt).</summary>");
        sb.AppendLine("    private static ReadOnlySpan<int> BracketCodePoints => new int[]");
        sb.AppendLine("    {");
        AppendRows(sb, brackets.Select(b => $"0x{b.Cp:X4}"), 10);
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Bidi_Paired_Bracket of the code point at the same index of <see cref=\"BracketCodePoints\"/>.</summary>");
        sb.AppendLine("    private static ReadOnlySpan<int> BracketPairs => new int[]");
        sb.AppendLine("    {");
        AppendRows(sb, brackets.Select(b => $"0x{b.Pair:X4}"), 10);
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>1 when the bracket at the same index of <see cref=\"BracketCodePoints\"/> opens, 0 when it closes.</summary>");
        sb.AppendLine("    private static ReadOnlySpan<byte> BracketIsOpen => new byte[]");
        sb.AppendLine("    {");
        AppendRows(sb, brackets.Select(b => b.Open ? "1" : "0"), 24);
        sb.AppendLine("    };");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendRows(StringBuilder sb, IEnumerable<string> items, int perRow)
    {
        var list = items.ToList();
        for (int i = 0; i < list.Count; i += perRow)
        {
            sb.Append("        ");
            sb.AppendJoin(", ", list.Skip(i).Take(perRow));
            sb.AppendLine(",");
        }
    }

    private static (int Start, int End) ParseRange(string range)
    {
        var dots = range.IndexOf("..", StringComparison.Ordinal);
        return dots < 0
            ? (ParseHex(range), ParseHex(range))
            : (ParseHex(range[..dots]), ParseHex(range[(dots + 2)..]));
    }

    private static int ParseHex(string s) => int.Parse(s.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}

/// <summary>
/// The official conformance files are 7–8 MB each. The repository keeps every hand-written case
/// (the UAX #9 examples and the targeted regression sections) and a deterministic sample of the
/// machine-generated permutations, which is enough to catch a regression in any rule.
/// </summary>
static class TestSubsets
{
    private const string Note =
        "# Subset generated by tools/Miko.UnicodeGen (ISSUE-157): every hand-written case, plus a\n" +
        "# deterministic sample of the generated permutations. The original file is part of the\n" +
        "# Unicode Character Database; see the header below and THIRD-PARTY-NOTICES.txt.\n#\n";

    public static string CharacterTest(string text)
    {
        var lines = text.Split('\n');
        var sb = new StringBuilder(Note);
        bool inPermutations = false;
        int dataIndex = 0;
        foreach (var line in lines)
        {
            if (line.StartsWith("# Permutations of sequences", StringComparison.Ordinal))
                inPermutations = true;

            bool isData = line.Length > 0 && line[0] != '#';
            if (!isData)
            {
                // Keep the file header and the section headers before the bulk sections.
                if (!inPermutations) sb.Append(line).Append('\n');
                continue;
            }
            if (!inPermutations || dataIndex++ % 97 == 0)
                sb.Append(line).Append('\n');
        }
        return sb.ToString();
    }

    public static string ClassTest(string text)
    {
        var lines = text.Split('\n');
        var sb = new StringBuilder(Note);
        int inGroup = 0;
        bool inHeader = true;
        foreach (var line in lines)
        {
            if (line.StartsWith('@'))
            {
                inHeader = false;
                if (line.StartsWith("@Levels", StringComparison.Ordinal)) inGroup = 0;
                sb.Append(line).Append('\n');
                continue;
            }
            bool isData = line.Length > 0 && line[0] != '#';
            if (!isData)
            {
                if (inHeader) sb.Append(line).Append('\n');
                continue;
            }
            // The first case of every @Levels group, then a sample.
            if (inGroup++ % 173 == 0) sb.Append(line).Append('\n');
        }
        return sb.ToString();
    }
}
