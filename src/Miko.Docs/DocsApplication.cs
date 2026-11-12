// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Miko.Docs;

/// <summary>Entry point for documentation projects: return await DocsApplication.RunAsync(args).</summary>
public static class DocsApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            if (options.Command == "help")
            {
                Console.WriteLine("Miko.Docs — .NET documentation generator\n\nCommands: build | serve (default) | help\nOptions: --project <dir> --output <dir> --base <path> --port <1-65535> --host <IP> --no-watch\n\nserve builds sources, watches changes and refreshes browsers. Published sites can be served without sources.");
                return 0;
            }
            if (options.Command == "build")
            {
                var result = new SiteBuilder().Build(options.Project, options.Output ?? Path.Combine(options.Project, "bin", "docs"), options.Base);
                Console.WriteLine($"Miko.Docs: generated {result.PageCount} pages -> {result.OutputDirectory}");
                return 0;
            }
            await ServeAsync(options);
            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Miko.Docs: {ex.Message}");
            return 1;
        }
    }

    private static async Task ServeAsync(Options options)
    {
        var sourceConfig = SiteConfig.Load(options.Project);
        var hasSources = Directory.Exists(Paths.Within(options.Project, sourceConfig.SrcDir));
        var output = options.Output ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        string siteBase;
        if (hasSources) siteBase = new SiteBuilder().Build(options.Project, output, options.Base).Config.Base;
        else
        {
            if (!Directory.Exists(output)) throw new DirectoryNotFoundException($"No documentation sources or built site found. Expected {output}.");
            var info = Path.Combine(output, "site-info.json");
            using var document = JsonDocument.Parse(File.Exists(info) ? File.ReadAllText(info) : "{\"base\":\"/\"}");
            siteBase = document.RootElement.GetProperty("base").GetString() ?? "/";
        }
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [], ContentRootPath = options.Project });
        builder.WebHost.UseUrls($"http://{(options.Host.Contains(':') ? "[" + options.Host + "]" : options.Host)}:{options.Port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        await using var app = builder.Build();
        var state = new BuildState();
        using var watcher = hasSources && options.Watch ? new SourceWatcher(options.Project, output, state, options.Base) : null;
        var contentTypes = new FileExtensionContentTypeProvider();
        app.Run(async context =>
        {
            if (context.Request.Method is not ("GET" or "HEAD"))
            { context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed; context.Response.Headers.Allow = "GET, HEAD"; return; }
            var raw = context.Request.Path.Value ?? "/";
            if (raw == siteBase + "__miko/status")
            {
                context.Response.Headers.CacheControl = "no-store";
                await context.Response.WriteAsJsonAsync(new { version = state.Version, error = state.Error });
                return;
            }
            if (siteBase != "/" && raw == siteBase.TrimEnd('/'))
            { context.Response.Redirect(siteBase + context.Request.QueryString, false); return; }
            if (!raw.StartsWith(siteBase, StringComparison.Ordinal)) { context.Response.StatusCode = 404; return; }
            string path;
            try
            {
                var relative = Uri.UnescapeDataString(raw[siteBase.Length..]);
                if (relative.Contains('\\') || relative.Contains('\0') || relative.Split('/').Any(p => p.StartsWith('.') || p.Contains(':')))
                { context.Response.StatusCode = 404; return; }
                path = relative.Length == 0 ? Path.Combine(output, "index.html") : Paths.Within(output, relative);
                Paths.NoLinks(path);
                if (Directory.Exists(path))
                {
                    if (!raw.EndsWith('/')) { context.Response.Redirect(raw + "/" + context.Request.QueryString, false); return; }
                    path = Path.Combine(path, "index.html");
                }
                else if (!File.Exists(path) && !Path.HasExtension(path) && File.Exists(path + ".html")) path += ".html";
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
            { context.Response.StatusCode = 404; return; }
            if (!File.Exists(path))
            { context.Response.StatusCode = 404; path = Path.Combine(output, "404.html"); }
            if (!File.Exists(path)) return;
            contentTypes.TryGetContentType(path, out var contentType);
            context.Response.ContentType = contentType ?? "application/octet-stream";
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            if (context.Request.Method == "HEAD") return;
            if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await File.ReadAllTextAsync(path, context.RequestAborted);
                if (watcher is not null)
                {
                    var endpoint = JsonSerializer.Serialize(siteBase + "__miko/status");
                    var version = JsonSerializer.Serialize(state.Version);
                    html = html.Replace("</body>", $$$$"""
                        <script>(()=>{const version={{{{version}}}};let busy=false;setInterval(async()=>{if(busy)return;busy=true;try{const r=await fetch({{{{endpoint}}}},{cache:'no-store'});if(r.ok){const s=await r.json();let e=document.getElementById('miko-build-error');if(s.error){if(!e){e=document.createElement('pre');e.id='miko-build-error';e.style='position:fixed;bottom:0;left:0;right:0;z-index:9999;padding:20px;background:#3b1720;color:#fff;white-space:pre-wrap';e.setAttribute('role','alert');document.body.append(e)}e.textContent=s.error}else if(s.version!==version){location.reload()}else if(e){e.remove()}}}catch{}finally{busy=false}},750)})();</script></body>
                        """, StringComparison.Ordinal);
                }
                await context.Response.WriteAsync(html, context.RequestAborted);
            }
            else await context.Response.SendFileAsync(path, context.RequestAborted);
        });
        Console.WriteLine($"Miko.Docs: http://{(options.Host == "0.0.0.0" ? "localhost" : options.Host)}:{options.Port}{siteBase}");
        Console.WriteLine(watcher is null ? "Serving static output. Press Ctrl+C to stop." : "Watching Markdown, assets and miko.docs.json. Press Ctrl+C to stop.");
        await app.RunAsync();
    }

    private sealed class BuildState
    {
        public volatile string Version = Guid.NewGuid().ToString("N");
        public volatile string? Error;
    }

    private sealed class SourceWatcher : IDisposable
    {
        private readonly FileSystemWatcher watcher;
        private readonly Timer timer;
        private readonly object gate = new();
        private readonly string project;
        private readonly string output;
        private readonly BuildState state;
        private string source;
        private readonly string siteBase;
        private readonly string? basePath;
        private bool disposed;

        public SourceWatcher(string project, string output, BuildState state, string? basePath)
        {
            this.project = project; this.output = output; this.state = state;
            var config = SiteConfig.Load(project);
            this.basePath = basePath;
            source = Paths.Within(project, config.SrcDir); siteBase = basePath ?? config.Base;
            timer = new Timer(_ => Rebuild(), null, Timeout.Infinite, Timeout.Infinite);
            watcher = new FileSystemWatcher(project) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size };
            watcher.Changed += Changed; watcher.Created += Changed; watcher.Deleted += Changed;
            watcher.Renamed += (_, e) => { Queue(e.FullPath); Queue(e.OldFullPath); };
            watcher.Error += (_, _) => Queue(Path.Combine(project, "miko.docs.json"));
            watcher.EnableRaisingEvents = true;
        }
        private void Changed(object sender, FileSystemEventArgs args) => Queue(args.FullPath);
        private void Queue(string path)
        {
            lock (gate)
            {
                if (disposed || Paths.Contains(output, path)) return;
                if (path.Equals(Path.Combine(project, "miko.docs.json"), StringComparison.OrdinalIgnoreCase) || Paths.Contains(source, path))
                    timer.Change(200, Timeout.Infinite);
            }
        }
        private void Rebuild()
        {
            lock (gate)
            {
                if (disposed) return;
                try
                {
                    var config = SiteConfig.Load(project);
                    if ((basePath ?? config.Base) != siteBase) throw new InvalidDataException("base changed; restart the development server (Ctrl+R in dotnet watch).");
                    var result = new SiteBuilder().Build(project, output, basePath);
                    source = Paths.Within(project, result.Config.SrcDir);
                    state.Error = null; state.Version = Guid.NewGuid().ToString("N");
                    Console.WriteLine($"Miko.Docs: rebuilt {result.PageCount} pages.");
                }
                catch (Exception ex) { state.Error = ex.Message; Console.Error.WriteLine($"Miko.Docs: {ex.Message}"); }
            }
        }
        public void Dispose()
        {
            lock (gate) { disposed = true; watcher.Dispose(); timer.Dispose(); }
        }
    }

    private sealed record Options(string Command, string Project, string? Output, string Host, int Port, bool Watch, string? Base)
    {
        public static Options Parse(string[] args)
        {
            var command = "serve"; var project = Directory.GetCurrentDirectory(); string? output = null; string? basePath = null;
            var host = "127.0.0.1"; var port = 5173; var watch = true; var i = 0;
            if (args.Length > 0 && !args[0].StartsWith('-')) command = args[i++];
            for (; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg is "--help" or "-h") { command = "help"; continue; }
                if (arg == "--no-watch") { watch = false; continue; }
                if (i + 1 >= args.Length) throw new ArgumentException($"Missing value for {arg}.");
                var value = args[++i];
                switch (arg)
                {
                    case "--project": project = Path.GetFullPath(value); break;
                    case "--output": output = value; break;
                    case "--base": basePath = value; new SiteConfig { Base = value }.Validate(); break;
                    case "--port":
                        if (!int.TryParse(value, out port) || port is < 1 or > 65535) throw new ArgumentException("port must be between 1 and 65535.");
                        break;
                    case "--host":
                        host = value == "localhost" ? "127.0.0.1" : value;
                        if (!IPAddress.TryParse(host, out _)) throw new ArgumentException("host must be localhost or an IP address.");
                        break;
                    default: throw new ArgumentException($"Unknown option: {arg}");
                }
            }
            if (command is not ("build" or "serve" or "help")) throw new ArgumentException($"Unknown command: {command}");
            return new Options(command, project, output is null ? null : Path.GetFullPath(output, project), host, port, watch, basePath);
        }
    }
}
