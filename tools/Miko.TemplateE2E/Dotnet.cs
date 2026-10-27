using System.Diagnostics;

namespace Miko.TemplateE2E;

/// <summary>Runs the dotnet CLI with everything it prints going to a log, never to the console:
/// parallel builds would interleave their output line by line.</summary>
static class Dotnet
{
    // The dotnet that started this tool when it runs under MSBuild, else the one on PATH.
    private static readonly string Host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } path ? path : "dotnet";

    /// <summary>Runs <c>dotnet</c>, appending the command line and its output to the file at <paramref name="logPath"/>.</summary>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(string logPath, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        using var log = new StreamWriter(logPath, append: true);
        return await RunAsync(log, arguments, cancellationToken);
    }

    /// <summary>Runs <c>dotnet</c>, writing the command line and its output to <paramref name="output"/>.</summary>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(TextWriter output, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(Host)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";

        output.WriteLine($"> dotnet {string.Join(' ', arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}");
        using var process = new Process { StartInfo = start };
        DataReceivedEventHandler write = (_, e) =>
        {
            if (e.Data is not null)
                lock (output) output.WriteLine(e.Data);
        };
        process.OutputDataReceived += write;
        process.ErrorDataReceived += write;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw;
        }

        lock (output) output.WriteLine($"> exit code {process.ExitCode}");
        return process.ExitCode;
    }

    /// <summary>The MSBuild errors in a log, or its last lines when it has none (a crash, a
    /// <c>dotnet new</c> failure).</summary>
    public static IReadOnlyList<string> Errors(string logPath)
    {
        var lines = File.ReadAllLines(logPath);
        var errors = lines.Where(l => l.Contains(": error ", StringComparison.Ordinal)).Select(l => l.Trim()).Distinct().Take(10).ToList();
        return errors.Count > 0 ? errors : lines.TakeLast(15).ToList();
    }
}
