using Miko.Components.Reader;
using Miko.Windowing;
using Miko.Windowing.Native;
using MikoApp.Reader;
using MikoApp.Reader.Desktop;

if (args is ["--verify", var directory])
{
    ReaderVerification.Run(directory);
    return;
}

string? path = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
bool comic = args.Contains("--comic") || Path.GetExtension(path ?? "").ToLowerInvariant() is ".cbz" or ".cbr";
await using var file = path == null ? null : new FileReaderSource(path);
using var context = App.CreateContext(builder => builder.UseDesktopNative(), source: file, comic: comic,
    fileTitle: path == null ? null : Path.GetFileNameWithoutExtension(path));
context.RunDesktop();
