using Miko.Components.Reader;
using Miko.Hosting;

namespace MikoApp.Reader;

public static class App
{
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null, Action<ReaderDemo>? page = null,
        IReaderSource? source = null, bool comic = false, string? fileTitle = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko · 书架").UseSize(430, 900);
        builder.UseRootComponent(() =>
        {
            var demo = new ReaderDemo { Source = source, Comic = comic, FileTitle = fileTitle };
            page?.Invoke(demo);
            return demo.Build();
        });
        configure?.Invoke(builder);
        return builder.Build();
    }
}
