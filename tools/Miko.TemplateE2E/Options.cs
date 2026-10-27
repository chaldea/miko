namespace Miko.TemplateE2E;

/// <summary>The command line. <c>null</c> lists mean "everything the templates offer".</summary>
sealed record Options(IReadOnlyList<string>? Layouts, IReadOnlyList<string>? Heads, int Parallelism, string? WorkDirectory, bool Keep)
{
    // An iOS or Android Release build keeps several cores busy and takes 2.5–3 GB. Measured on 12
    // cores / 16 GB, all 25 builds: 4 at a time 2m 58s, 6 2m 43s, 8 2m 31s (the sequential script
    // took 8m 00s), with the memory compressor at 7–8 GB and swap growing from 6 on.
    public static readonly int DefaultParallelism = Math.Clamp(
        Math.Min(Environment.ProcessorCount / 2, (int)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (3L << 30))), 1, 6);

    public static string Usage => $"""
        Usage: dotnet run --project tools/Miko.TemplateE2E -- [options]

          --layouts <a,b,...>   layouts to scaffold (default: every layout the templates offer)
          --heads <a,b,...>     multiplatform heads to build (default: every head)
          --parallel <n>        builds to run at once (default: {DefaultParallelism} on this machine)
          --work-dir <dir>      empty or missing directory to work in (default: a new temp directory)
          --keep                keep the apps of passing builds too (a full run leaves about 7 GB);
                                the apps of failed builds are always kept
        """;

    /// <summary>Parses the arguments; <c>null</c> when help was asked for.</summary>
    public static Options? Parse(IReadOnlyList<string> args)
    {
        IReadOnlyList<string>? layouts = null, heads = null;
        var parallelism = DefaultParallelism;
        string? workDirectory = null;
        var keep = false;

        for (var i = 0; i < args.Count; i++)
        {
            var name = args[i];
            if (name is "-h" or "--help")
                return null;
            if (name == "--keep")
            {
                keep = true;
                continue;
            }
            if (i + 1 == args.Count)
                throw new UsageException($"{name} needs a value.");
            var value = args[++i];
            switch (name)
            {
                case "--layouts":
                    layouts = List(value);
                    break;
                case "--heads":
                    heads = List(value);
                    break;
                case "--parallel" or "-j":
                    parallelism = int.TryParse(value, out var n) && n > 0 ? n : throw new UsageException($"{name} needs a positive number.");
                    break;
                case "--work-dir":
                    workDirectory = value;
                    break;
                default:
                    throw new UsageException($"Unknown option {name}.");
            }
        }

        return new Options(layouts, heads, parallelism, workDirectory, keep);
    }

    private static string[] List(string value) =>
        value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>A command line the tool cannot act on; reported together with the usage text.</summary>
sealed class UsageException(string message) : Exception(message);
