using System.Collections.Concurrent;
using System.Diagnostics;
using Miko.TemplateE2E;

// End-to-end check of the dotnet new templates (Release-1.0.0 D1, ISSUE-160).
//
// Packs every library and the template package into a private feed, installs the templates into a
// private hive, scaffolds each template/layout and builds every head in Release from the packed
// packages — the path a user takes, which the unit tests never exercise. The builds run in
// parallel, each in a scaffold of its own, and write one log each under <work dir>/logs.
// Not part of PR validation: the templates change rarely, so run it by hand after changing them or
// the packaging, and before a release.
//
//   dotnet run --project tools/Miko.TemplateE2E                 all layouts, all heads the machine can build
//   dotnet run --project tools/Miko.TemplateE2E -- --layouts blank,full
//   dotnet run --project tools/Miko.TemplateE2E -- --heads Desktop,Android --parallel 4
//
// Android heads need the android workload; iOS heads need macOS with the ios workload and build for
// ios-arm64 without signing. Heads whose workload is missing are skipped, not failed.

try
{
    var options = Options.Parse(args);
    if (options is null)
    {
        Console.WriteLine(Options.Usage);
        return 0;
    }
    return await RunAsync(options);
}
catch (UsageException e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Options.Usage);
    return 2;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Cancelled.");
    return 130;
}

static async Task<int> RunAsync(Options options)
{
    var repoRoot = FindRepoRoot();
    var jobs = BuildPlan.Create(BuildPlan.ReadTemplates(repoRoot), options, await SdkPacks.LocateAsync());
    var work = WorkDirectory.Create(options.WorkDirectory);
    Console.WriteLine($"work dir: {work.Root}");

    // Not disposed: the handler stays registered until the process exits.
    var cancel = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cancel.Cancel();
    };

    var clock = Stopwatch.StartNew();
    if (!await PackAsync(repoRoot, work, cancel.Token))
        return 1;
    var packed = clock.Elapsed;

    var skipped = jobs.Where(j => j.SkipReason is not null).ToList();
    foreach (var job in skipped)
        Console.WriteLine($"SKIP {job.Label} ({job.SkipReason})");

    var runnable = jobs.Except(skipped).ToList();
    var width = runnable.Select(j => j.Label.Length).DefaultIfEmpty(0).Max();
    var failures = new ConcurrentBag<BuildJob>();
    var finished = 0;
    var console = new Lock();
    using var scaffolding = new SemaphoreSlim(1);
    Console.WriteLine($"Building {runnable.Count} apps, {options.Parallelism} at a time …");

    var parallel = new ParallelOptions { MaxDegreeOfParallelism = options.Parallelism, CancellationToken = cancel.Token };
    await Parallel.ForEachAsync(runnable, parallel, async (job, token) =>
    {
        var watch = Stopwatch.StartNew();
        var passed = await BuildAsync(job, work, scaffolding, token);
        var elapsed = watch.Elapsed;
        if (!passed)
            failures.Add(job);
        else if (!options.Keep)
            WorkDirectory.TryDelete(work.AppDirectory(job));

        lock (console)
        {
            var count = $"[{++finished}/{runnable.Count}]".PadLeft(runnable.Count.ToString().Length * 2 + 3);
            Console.WriteLine($"{count} {(passed ? "PASS" : "FAIL")} {job.Label.PadRight(width)}  {Format(elapsed)}");
            if (!passed)
            {
                Console.WriteLine($"      log: {work.LogFile(job)}");
                foreach (var line in Dotnet.Errors(work.LogFile(job)))
                    Console.WriteLine($"      {line}");
            }
        }
    });

    var times = $"{Format(clock.Elapsed)} (packing {Format(packed)}, building {Format(clock.Elapsed - packed)})";
    if (!failures.IsEmpty)
    {
        Console.WriteLine("Failed builds:");
        foreach (var job in runnable.Where(failures.Contains))
            Console.WriteLine($"  {job.Label}  ({work.LogFile(job)})");
        Console.WriteLine($"{runnable.Count - failures.Count} passed, {failures.Count} failed, {skipped.Count} skipped in {times}.");
        return 1;
    }
    // The failed apps restore from it; with none left it is only a cache.
    if (!options.Keep)
        WorkDirectory.TryDelete(work.Packages);
    Console.WriteLine($"All {runnable.Count} template builds passed ({skipped.Count} skipped) in {times}.");
    return 0;
}

// The template package ships docs/**/*.md, which building Miko.DocGen (part of the solution)
// regenerates: the libraries are packed first.
static async Task<bool> PackAsync(string repoRoot, WorkDirectory work, CancellationToken cancellationToken)
{
    // What Directory.Build.props packs locally and the templates reference.
    const string version = "1.0.0-dev";

    Console.WriteLine($"Packing the libraries and the templates ({version}) …");
    string[] pack = ["-c", "Release", "-o", work.Feed, $"-p:Version={version}", "--nologo", "-v", "minimal"];
    return await Step("pack-libraries", ["pack", Path.Combine(repoRoot, "miko.slnx"), .. pack])
        && await Step("pack-templates", ["pack", Path.Combine(repoRoot, "templates", "Miko.Templates.csproj"), .. pack])
        && await Step("install-templates", ["new", "install", Path.Combine(work.Feed, $"Miko.Templates.{version}.nupkg"), "--debug:custom-hive", work.Hive]);

    async Task<bool> Step(string name, string[] arguments)
    {
        var log = work.LogFile(name);
        if (await Dotnet.RunAsync(log, arguments, cancellationToken) == 0)
            return true;
        Console.WriteLine($"FAIL {name}");
        Console.WriteLine($"     log: {log}");
        foreach (var line in Dotnet.Errors(log))
            Console.WriteLine($"     {line}");
        return false;
    }
}

static async Task<bool> BuildAsync(BuildJob job, WorkDirectory work, SemaphoreSlim scaffolding, CancellationToken cancellationToken)
{
    var app = work.AppDirectory(job);
    var log = work.LogFile(job);

    // dotnet new keeps its state in the hive, so one scaffold at a time: it takes a second, a build minutes.
    await scaffolding.WaitAsync(cancellationToken);
    try
    {
        // A hyphenated name exercises the identifier rewriting dotnet new applies (my-app -> my_app).
        // `--layout=<value>`: `--layout list` would parse as the `dotnet new list` command.
        string[] scaffold = ["new", job.Template.ShortName, "-n", "my-app", "-o", app, $"--layout={job.Layout}", "--debug:custom-hive", work.Hive];
        if (await Dotnet.RunAsync(log, scaffold, cancellationToken) != 0)
            return false;
    }
    finally
    {
        scaffolding.Release();
    }
    work.WriteNuGetConfig(app);

    // Each head is built in a scaffold of its own: the heads of one app share its library project,
    // and two builds writing that project's obj/ at once break each other.
    var project = job.Head is null ? app : Directory.GetFiles(app, $"*.{job.Head.Name}.csproj", SearchOption.AllDirectories).Single();
    string[] build = ["build", project, "-c", "Release", "--nologo", "-v", "minimal", "-warnaserror:NU1603", .. job.BuildArguments];
    return await Dotnet.RunAsync(log, build, cancellationToken) == 0;
}

static string Format(TimeSpan time) =>
    time.TotalMinutes >= 1 ? $"{(int)time.TotalMinutes}m {time.Seconds:00}s" : $"{time.Seconds}s";

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
