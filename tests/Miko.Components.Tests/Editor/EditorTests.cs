using Miko.Components.Editor;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform;
using Shouldly;

namespace Miko.Components.Tests.Editor;

public sealed class EditorTests
{
    [Fact]
    public void Should_index_mixed_line_endings_and_keep_the_empty_last_line()
    {
        var doc = new EditorDocument("a\r\nb\rc\n");
        doc.LineCount.ShouldBe(4);
        Enumerable.Range(0, 4).Select(i => doc.GetLine(i).ToString()).ShouldBe(["a", "b", "c", ""]);
        doc.GetLineAt(2).ShouldBe(0);
        doc.GetLineStart(3).ShouldBe(7);
    }

    [Fact]
    public void Should_align_insertions_deletions_and_unchanged_lines()
    {
        var original = new EditorDocument("one\ntwo\nthree\nfour\nfive");
        var current = new EditorDocument("zero\none\ntwo\nnew\nfour\nfive\nsix");
        var diff = EditorDiff.Compare(original, current);
        diff.Select(r => (r.Original, r.Modified)).ShouldBe([
            (-1, 0), (0, 1), (1, 2), (2, -1), (-1, 3), (3, 4), (4, 5), (-1, 6)
        ]);
    }

    [Fact]
    public void Should_edit_through_host_input_bind_and_keep_graphemes_intact()
    {
        string? bound = null;
        var editor = new MikoEditor { Value = "ab\r\nc", ValueChanged = EventCallback.Factory.Create<string>(this, s => bound = s) };
        using var app = Open(editor, out var surface);
        using (surface)
        {
            var host = app.Controller;
            host.OnPointerDown(60, 8, MouseButton.Left);
            host.OnPointerUp(60, 8, MouseButton.Left);
            host.OnKeyDown(MikoKey.End, 0);
            host.OnTextInput("👩‍💻");
            bound.ShouldBe("ab👩‍💻\r\nc");
            host.OnKeyDown(MikoKey.Backspace, 0);
            bound.ShouldBe("ab\r\nc");
            host.OnKeyDown(MikoKey.Down, 0);
            host.OnKeyDown(MikoKey.Home, 0);
            host.OnKeyDown(MikoKey.Backspace, 0);
            bound.ShouldBe("abc");
            host.OnKeyDown(MikoKey.A, MikoKeyModifiers.Meta);
            host.OnTextInput("new");
            bound.ShouldBe("new");
            host.OnKeyDown(MikoKey.Z, MikoKeyModifiers.Control);
            bound.ShouldBe("abc");
            editor.Build();
            editor.Document.Text.ShouldBe("abc");
        }
    }

    [Fact]
    public void Should_keep_unchanged_lines_between_large_diff_hunks()
    {
        var original = new EditorDocument("old\nkeep\nrepeat\nrepeat\nend");
        string added = string.Join('\n', Enumerable.Range(0, 1024));
        var current = new EditorDocument(added + "\nkeep\nnew\nrepeat\nrepeat\nfinish");
        var diff = EditorDiff.Compare(original, current);
        diff.Where(r => r.Original >= 0 && r.Modified >= 0).Select(r => r.Original).ShouldBe([1, 2, 3]);
        diff.Where(r => r.Modified >= 0).Select(r => r.Modified).ShouldBe(Enumerable.Range(0, current.LineCount));
        diff.Where(r => r.Original >= 0).Select(r => r.Original).ShouldBe(Enumerable.Range(0, original.LineCount));
        EditorDiff.Compare(new(""), new("")).ShouldBeEmpty();
        EditorDiff.Compare(new(""), new("one")).ShouldBe([new DiffRow(-1, 0)]);
    }

    [Fact]
    public void Should_commit_multiline_ime_text_over_a_selection_at_the_painted_caret()
    {
        var editor = new MikoEditor { Value = "one\ntwo" };
        using var app = Open(editor, out var surface);
        using (surface)
        {
            var ime = new InputMethod();
            app.Controller.AttachInputMethod(ime);
            app.Controller.OnPointerDown(62, 32, MouseButton.Left);
            app.Controller.OnPointerUp(62, 32, MouseButton.Left);
            app.Controller.SetTextSelection(7, 4);
            ime.State!.SelectionStart.ShouldBe(4);
            ime.State.SelectionEnd.ShouldBe(7);
            ime.State.CursorRect.Y.ShouldBe(22);
            ime.State.InputType.ShouldBe(InputMethodType.Multiline);
            app.Controller.OnTextCompositionStart();
            app.Controller.OnTextCompositionUpdate("wen");
            editor.Document.Text.ShouldBe("one\ntwo");
            app.Controller.OnTextCompositionEnd("文\n\t字");
            editor.Document.Text.ShouldBe("one\n文\n\t字");
            ime.State.CursorPosition.ShouldBe(editor.Document.Text.Length);
        }
    }

    [Fact]
    public void Should_format_registered_language_and_protect_diff_from_edits()
    {
        var services = new EditorServices();
        services.RegisterFormatter("sample", new UpperCase());
        var editor = new MikoEditor { Services = services, Language = "SAMPLE", Value = "one" };
        using var app = Open(editor, out var surface);
        using (surface)
        {
            editor.FormatDocument().ShouldBeTrue();
            editor.Document.Text.ShouldBe("ONE");
            editor.OriginalValue = "old";
            editor.Build();
            app.Engine.Render(surface.Context);
            editor.FormatDocument().ShouldBeFalse();
            app.Controller.OnPointerDown(70, 15, MouseButton.Left);
            app.Controller.OnPointerUp(70, 15, MouseButton.Left);
            app.Controller.OnTextInput("discard");
            editor.Document.Text.ShouldBe("ONE");
            surface.GetPixel(250, 10).ShouldBe(editor.Theme.Removed);
            surface.GetPixel(250, 32).ShouldBe(editor.Theme.Added);
            editor.Theme = EditorTheme.Dark;
            editor.Build();
            app.Engine.Render(surface.Context);
            surface.GetPixel(250, 10).ShouldBe(EditorTheme.Dark.Removed);
        }
    }

    internal static MikoAppContext Open(MikoEditor editor, out RenderSurface surface)
    {
        var app = MikoAppBuilder.CreateDefault().UseRootComponent(editor.Build).Build();
        surface = app.Engine.Graphics.CreateSurface(320, 240);
        app.Controller.Initialize(surface.Context, 320, 240);
        return app;
    }

    private sealed class UpperCase : IEditorFormatter
    {
        public string Format(EditorDocument document) => document.Text.ToUpperInvariant();
    }

    private sealed class InputMethod : InputMethodBase
    {
        public InputMethodState? State { get; private set; }
        public override void SetState(InputMethodState? state) => State = state;
    }
}
