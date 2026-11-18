using Miko.Common;
using Miko.Components.Reader;
using Miko.Core;
using Miko.Events;
using Miko.Layout;

namespace MikoApp.Reader.Desktop;

/// <summary>Exercises the shared shelf-to-reader flow and produces real phone/desktop renders.</summary>
internal static class ReaderVerification
{
    public static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        ReaderDemo? page = null;
        using var app = App.CreateContext(page: p => page = p);
        using var phone = app.Engine.Graphics.CreateSurface(390, 844);
        app.Controller.Initialize(phone.Context, 390, 844);
        app.Controller.SetSafeAreaInsets(0, 44, 0, 34);
        Frame(); Save("shelf.png");
        if (page is not { IsShelfVisible: true }) throw new InvalidOperationException("The app must start on the shelf.");
        Click("reader-shelf-book", 0);
        if (page.Reader is not BookReader book) throw new InvalidOperationException("An ebook must open BookReader.");
        Save("book.png");
        book.ShowControls(); Frame(); Save("book-controls.png");
        Click("miko-reader-settings");
        if (!book.IsSettingsOpen) throw new InvalidOperationException("Settings did not open.");
        Save("book-settings.png");
        Click("miko-reader-increase", 0);
        if (book.FontSize != 22) throw new InvalidOperationException("The font-size control did not apply.");
        Click("miko-reader-appearance"); Save("book-appearance.png");
        Click("miko-reader-theme", 3); Save("book-dark.png");
        if (phone.GetPixel(5, 20).R > 50) throw new InvalidOperationException("Night theme must cover the safe-area background too.");
        Click("miko-reader-contents"); Save("book-contents.png");
        Click("miko-reader-entry", 1);
        if (book.CurrentIndex != 1) throw new InvalidOperationException("The directory did not navigate.");
        book.ShowControls(); Frame(); Click("miko-reader-back");
        if (!page.IsShelfVisible || page.Books[0].FontSize != 22 || page.Books[0].Index != 1)
            throw new InvalidOperationException("Returning to the shelf must retain settings and chapter.");
        Click("reader-shelf-filter", 1);
        if (app.Engine.GetRoot()!.FindByClass("reader-shelf-book").Count() != 2)
            throw new InvalidOperationException("The ebook filter must show only ebooks.");
        Click("reader-shelf-book", 0);
        if (page.Reader is not BookReader { FontSize: 22, CurrentIndex: 1, Theme: ReaderTheme.Dark })
            throw new InvalidOperationException("Reopening the book must restore its settings and chapter.");
        page.ReturnToShelf(); Frame();
        Click("reader-shelf-book", 1);
        if (page.Reader is not ComicReader comic) throw new InvalidOperationException("A comic must open ComicReader.");
        Save("comic.png"); comic.ShowControls(); Frame();
        Click("miko-reader-settings"); Save("comic-settings.png");
        comic.HideControls(); Frame();
        comic.ToggleZoom(); Frame(); Save("comic-zoom.png");
        page.ReturnToShelf(); Frame();
        using var desktop = app.Engine.Graphics.CreateSurface(1000, 760);
        app.Controller.SetSafeAreaInsets(0, 0, 0, 0);
        for (int i = 0; i < 4; i++) app.Controller.RenderFrame(desktop.Context, 1000, 760, 1 / 60f, c => app.Engine.Render(c));
        File.WriteAllBytes(Path.Combine(directory, "shelf-desktop.png"), desktop.EncodePng());
        Console.WriteLine("Verified shelf navigation, book/comic selection, settings, directory, return and retained preferences.");

        void Frame() { for (int i = 0; i < 4; i++) app.Controller.RenderFrame(phone.Context, 390, 844, 1 / 60f, c => app.Engine.Render(c)); }
        void Save(string name) => File.WriteAllBytes(Path.Combine(directory, name), phone.EncodePng());
        void Click(string name, int index = 0)
        {
            var element = app.Engine.GetRoot()!.FindByClass(name).ElementAt(index);
            var box = Find(app.Engine.GetCurrentLayout()!, element)?.BoxModel.BorderBox
                ?? throw new InvalidOperationException($"Missing layout: {name}");
            if (box.Height <= 0 || box.Bottom > 844 || box.Y < 0) throw new InvalidOperationException($"Control outside phone viewport: {name}: {box}");
            float x = box.X + box.Width / 2, y = box.Y + box.Height / 2;
            app.Controller.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
            app.Controller.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch);
            Frame();
        }
    }

    private static LayoutBox? Find(LayoutBox box, Element element) => box.Element == element ? box
        : box.Children.Select(child => Find(child, element)).FirstOrDefault(child => child != null);
}
