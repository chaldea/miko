using System.Diagnostics;
using Miko.Common;
using Miko.Components.Editor;
using Miko.Graphics;
using Miko.Highlight;
using Miko.Hosting;
using Shouldly;
using Xunit.Abstractions;

namespace Miko.Components.Tests.Editor;

[CollectionDefinition("Editor rendering measurements", DisableParallelization = true)]
public sealed class EditorRenderingCollection;

[Collection("Editor rendering measurements")]
public sealed class EditorRenderingTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(80)]
    [InlineData(100_000)]
    public void Should_measure_open_memory_and_scroll_frames(int lineCount)
    {
        const int width = 800, height = 440, frames = 120;
        using var file = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), FileMode.CreateNew,
            FileAccess.ReadWrite, FileShare.Read | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
        using (var writer = new StreamWriter(file, leaveOpen: true))
            for (int i = 0; i < lineCount; i++) writer.Write($"const int line{i} = {i}; // sample\n");
        var editor = new MikoEditor { Language = "csharp", Style = new() { Height = Length.Px(height) } };
        var probe = new Probe();
        editor.Services = new();
        editor.Services.RegisterLanguage("csharp", probe);

        // Warm the native font backend, parser and host before measuring a fresh editor.
        using (var warm = EditorTests.Open(new MikoEditor { Value = "const int warm = 1;", Language = "csharp" }, out var warmSurface))
            warmSurface.Dispose();
        using var app = MikoAppBuilder.CreateDefault().UseRootComponent(editor.Build).Build();
        using var surface = app.Engine.Graphics.CreateSurface(width, height);
        long heapBefore = GC.GetTotalMemory(true);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long open = Stopwatch.GetTimestamp();
        using (var reader = new StreamReader(new FileStream(file.Name, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete)))
            editor.Value = reader.ReadToEnd();
        app.Controller.Initialize(surface.Context, width, height);
        var openTime = Stopwatch.GetElapsedTime(open);
        long openBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        long heapDelta = GC.GetTotalMemory(true) - heapBefore;
        probe.Lines.ShouldBeLessThanOrEqualTo(height / 22 + 1);
        probe.Lines.ShouldBeGreaterThan(0);

        var elapsed = new double[frames];
        int slow = 0;
        Action<IDrawingContext> draw = c => app.Engine.Render(c);
        allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < frames; frame++)
        {
            // Spread samples across the whole document, traversing both endpoints.
            float step = (lineCount - height / 22 + 1) * 22f / 60;
            long start = Stopwatch.GetTimestamp();
            app.Controller.OnScroll(400, 180, 0, frame < 60 ? step : -step);
            app.Controller.RenderFrame(surface.Context, width, height, 1f / 60, draw);
            elapsed[frame] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (elapsed[frame] > 1000d / 60) slow++;
        }
        long frameBytes = (GC.GetAllocatedBytesForCurrentThread() - allocated) / frames;
        Array.Sort(elapsed);
        output.WriteLine($"lines={lineCount:N0}, input={editor.Value.Length:N0} ASCII bytes; open+first paint={openTime.TotalMilliseconds:F2} ms, allocated={openBytes:N0} B, retained managed heap delta={heapDelta:N0} B");
        output.WriteLine($"scroll frames={frames}, p50={elapsed[59]:F2} ms, p95={elapsed[113]:F2} ms, max={elapsed[^1]:F2} ms, over 16.67 ms={slow}, allocated/frame={frameBytes:N0} B");
        probe.MaxLine.ShouldBeGreaterThanOrEqualTo(lineCount - 2);
        probe.Lines.ShouldBeLessThanOrEqualTo((frames + 1) * (height / 22 + 1));
    }

    [Fact]
    public void Should_clip_a_long_line_and_reach_its_end()
    {
        var probe = new Probe();
        var editor = new MikoEditor { Value = new string('x', 1_000_000) + "END", Language = "test", Services = new() };
        editor.Services.RegisterLanguage("test", probe);
        using var app = EditorTests.Open(editor, out var surface);
        using (surface)
        {
            probe.Lines.ShouldBe(0);
            editor.Select(editor.Value.Length, editor.Value.Length);
            app.Engine.Render(surface.Context);
            app.Controller.OnPointerDown(260, 12, Events.MouseButton.Left);
            app.Controller.OnPointerUp(260, 12, Events.MouseButton.Left);
            app.Controller.OnTextInput("z");
            editor.Document.Text.IndexOf('z').ShouldBeGreaterThan(999_950);
        }
    }

    private sealed class Probe : IEditorLanguageService
    {
        private readonly SyntaxHighlighter _syntax = new();
        public int Lines, MaxLine;

        public IReadOnlyList<CodeToken>? TokenizeLine(EditorDocument document, int line)
        {
            Lines++;
            MaxLine = Math.Max(line, MaxLine);
            return _syntax.Tokenize(document.GetLine(line).ToString(), "csharp");
        }
    }
}
