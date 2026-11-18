using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components.Reader;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Styling;
using Shouldly;

namespace Miko.Components.Tests.Reader;

public sealed class ReaderComponentTests : IDisposable
{
    private readonly MikoEngine _engine = new MikoEngineBuilder().Build();
    private readonly RenderSurface _surface;
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    private readonly MikoInteractionController _input;
    private readonly ManualClock _clock = new();
    private readonly PlatformInfo _platform = new(HostPlatform.Android);
    private ReaderComponentBase? _reader;
    private Element _root = null!;

    public ReaderComponentTests()
    {
        _surface = _engine.Graphics.CreateSurface(400, 600);
        _input = new MikoInteractionController(Options.Create(new MikoAppOptions()), _services, _engine,
            new EventDispatcher(), new MikoDispatcher(), new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance);
    }

    private void Show(ReaderComponentBase reader)
    {
        _reader = reader;
        reader.Engine = _engine; reader.Input = _input; reader.Clock = _clock;
        reader.PlatformService = _platform;
        reader.Texts = ReaderTexts.Chinese;
        reader.Style = new() { Width = Length.Percent(100), Height = Length.Percent(100) };
        _root = reader.Build();
        _engine.Initialize(_root, [], _surface.Context, 400, 600);
        Render();
    }

    private void Render() { for (int i = 0; i < 4; i++) _engine.Render(_surface.Context); }
    private Element Element(string name) => _root.FindByClass(name).Single();
    private static IReaderSource BookSource() => new ReaderControllerTests.Source(2)
    {
        Read = (entry, _) => Task.FromResult<ReaderContent>(new ReaderTextContent(string.Join('\n', Enumerable.Range(0, 80).Select(i => $"第{entry.Id}章，第{i}段：你好，世界。This is a paragraph in a book.")))),
    };

    [Fact]
    public async Task Book_Paginates_At_Viewport_Size_And_Turns_Back_To_Last_Page_Of_Previous_Chapter()
    {
        var reader = new BookReader { Source = BookSource() };
        Show(reader);
        reader.Snapshot.State.ShouldBe(ReaderState.Ready);
        reader.PageCount.ShouldBeGreaterThan(1);
        Element("miko-reader-viewport").OffsetHeight.ShouldBe(548, .5f);
        Element("miko-reader-previous").IsDisabled.ShouldBeTrue();
        await reader.NextAsync(); Render();
        reader.CurrentIndex.ShouldBe(0);
        reader.PageIndex.ShouldBe(1);
        await reader.GoToAsync(1); Render();
        reader.PageIndex.ShouldBe(0);
        await reader.PreviousAsync(); Render();
        reader.CurrentIndex.ShouldBe(0);
        reader.PageIndex.ShouldBe(reader.PageCount - 1);
    }

    [Fact]
    public async Task Typography_And_Resize_Reflow_Pages_Without_Resetting_The_Chapter()
    {
        var reader = new BookReader { Source = BookSource() };
        Show(reader);
        await reader.NextAsync(); Render();
        int pages = reader.PageCount;
        reader.FontSize = 30; reader.LineSpacing = 2; reader.PageMargin = 32;
        reader.Build(); Render();
        reader.PageCount.ShouldBeGreaterThan(pages);
        reader.PageIndex.ShouldBeGreaterThan(0);
        _engine.SetViewportSize(280, 600); Render();
        reader.CurrentIndex.ShouldBe(0);
        reader.PageCount.ShouldBeGreaterThan(pages);
        // Real settings controls render over the page and can be closed without losing the position.
        int page = reader.PageIndex;
        reader.ToggleSettings(); Render();
        _root.FindByClass("miko-reader-choice").Count().ShouldBe(5);
        reader.ToggleSettings(); Render();
        reader.PageIndex.ShouldBe(page);
    }

    [Fact]
    public void Book_Scroll_Uses_Wheel_And_Theme_Changes_Reach_The_Canvas()
    {
        var reader = new BookReader { Source = BookSource(), Mode = BookReadingMode.Scroll, Theme = ReaderTheme.Sepia };
        Show(reader);
        var viewport = Element("miko-reader-viewport");
        viewport.OnWheel!(new() { Target = viewport, DeltaY = 800 }); Render();
        reader.PageIndex.ShouldBeGreaterThan(0);
        reader.EyeCare = true; reader.Build(); Render();
        _surface.GetPixel(1, 100).ShouldBe(Color.FromHex("#dce8d0"));
    }

    [Fact]
    public async Task Comic_Spread_Order_Direction_Keys_And_Double_Tap_Work_Through_The_Engine()
    {
        var source = ImageSource();
        var reader = new ComicReader { Source = source, PageMode = ComicPageMode.Double, Direction = ReaderDirection.RightToLeft };
        Show(reader);
        // Source page 0 is red and occupies the right half in RTL; page 1 is blue.
        _surface.GetPixel(100, 280).ShouldBe(Color.FromHex("#0000ff"));
        _surface.GetPixel(300, 280).ShouldBe(Color.FromHex("#ff0000"));
        Tap(300, 280); _clock.Advance(TimeSpan.FromMilliseconds(100)); Tap(300, 280);
        reader.Zoom.ShouldBe(2);
        Tap(300, 280); _clock.Advance(TimeSpan.FromMilliseconds(100)); Tap(300, 280);
        reader.Zoom.ShouldBe(1);
        _input.OnKeyDown(MikoKey.Left, MikoKeyModifiers.None); Render();
        reader.CurrentIndex.ShouldBe(2);
        reader.Snapshot.Content.Count.ShouldBe(1);
        await reader.PreviousAsync(); Render();
        reader.CurrentIndex.ShouldBe(0);
        reader.Direction = ReaderDirection.LeftToRight;
        reader.Build(); Render();
        _surface.GetPixel(100, 280).ShouldBe(Color.FromHex("#ff0000"));
        _surface.GetPixel(300, 280).ShouldBe(Color.FromHex("#0000ff"));
    }

    [Fact]
    public void Vertical_Fit_Width_Spread_Starts_At_Top_And_Pans_Without_Overlapping_Pages()
    {
        var reader = new ComicReader { Source = ImageSource(), Orientation = ReaderOrientation.Vertical, PageMode = ComicPageMode.Double, FitWidth = true };
        Show(reader);
        _surface.GetPixel(200, 50).ShouldBe(Color.FromHex("#ff0000"));
        _surface.GetPixel(200, 530).ShouldBe(Color.FromHex("#ff0000"));
        var viewport = Element("miko-reader-viewport");
        viewport.OnWheel!(new() { Target = viewport, DeltaY = 700 }); Render();
        _surface.GetPixel(200, 50).ShouldBe(Color.FromHex("#0000ff"));
        _surface.GetPixel(200, 530).ShouldBe(Color.FromHex("#0000ff"));
    }

    [Fact]
    public void Comic_Vertical_Swipe_Advances_And_Invalid_Images_Show_An_Error()
    {
        var reader = new ComicReader { Source = ImageSource(), Orientation = ReaderOrientation.Vertical };
        Show(reader);
        _input.OnPointerDown(200, 380, MouseButton.Left, PointerType.Touch);
        _input.OnPointerMove(200, 260);
        _input.OnPointerUp(200, 260, MouseButton.Left, PointerType.Touch);
        Render();
        reader.CurrentIndex.ShouldBe(1);
        reader.Source = new ReaderControllerTests.Source(1) { Read = (_, _) => Task.FromResult<ReaderContent>(new ReaderImageContent(new byte[] { 1, 2, 3 })) };
        reader.Build(); Render();
        Element("miko-reader-status").Children[0].TextContent!.ShouldContain("Cannot decode");
        Element("miko-reader-retry").OffsetHeight.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Controlled_Index_Is_Reported_Once_And_Disposal_Releases_Keyboard_Focus()
    {
        var indices = new List<int>();
        var reader = new ComicReader
        {
            Source = ImageSource(),
            IndexChanged = EventCallback.Factory.Create<int>(this, index => indices.Add(index)),
        };
        Show(reader);
        await reader.GoToAsync(1); Render();
        reader.Index = 1; reader.Build(); Render();
        indices.ShouldBe([0, 1]);
        Tap(200, 280);
        var viewport = Element("miko-reader-viewport");
        viewport.HasState(ElementState.Focus).ShouldBeTrue();
        reader.Dispose();
        viewport.HasState(ElementState.Focus).ShouldBeFalse();
        _input.OnKeyDown(MikoKey.Right, MikoKeyModifiers.None).ShouldBeFalse();
    }

    [Fact]
    public void Caller_Style_Overrides_Default_Height_And_Width()
    {
        var reader = new BookReader { Source = BookSource() };
        Show(reader);
        reader.Style = new() { Width = Length.Px(320), Height = Length.Px(450) };
        reader.Build(); Render();
        _root.OffsetWidth.ShouldBe(320, .5f);
        _root.OffsetHeight.ShouldBe(450, .5f);
        Element("miko-reader-viewport").OffsetHeight.ShouldBe(398, .5f);
    }

    [Fact]
    public void Tap_Toggles_Chrome_While_Double_Tap_Only_Zooms_And_Disposal_Cancels_Pending_Tap()
    {
        var reader = new ComicReader { Source = ImageSource() };
        Show(reader);
        reader.AreControlsVisible.ShouldBeFalse();
        Tap(200, 280);
        _clock.Advance(TimeSpan.FromMilliseconds(349)); Render();
        reader.AreControlsVisible.ShouldBeFalse();
        _clock.Advance(TimeSpan.FromMilliseconds(1)); Render();
        reader.AreControlsVisible.ShouldBeTrue();
        Tap(200, 280); _clock.Advance(TimeSpan.FromMilliseconds(350)); Render();
        reader.AreControlsVisible.ShouldBeFalse();

        Tap(200, 280); _clock.Advance(TimeSpan.FromMilliseconds(100)); Tap(200, 280);
        _clock.Advance(TimeSpan.FromMilliseconds(500)); Render();
        reader.Zoom.ShouldBe(2);
        reader.AreControlsVisible.ShouldBeFalse();

        Tap(200, 280);
        reader.Dispose();
        _clock.Advance(TimeSpan.FromMilliseconds(500)); Render();
        reader.AreControlsVisible.ShouldBeFalse();
        _platform.Back.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Panels_Preserve_Pagination_And_Back_Dismisses_In_Order()
    {
        int exits = 0;
        var reader = new BookReader { Source = BookSource(), BackRequested = EventCallback.Factory.Create(this, () => exits++) };
        Show(reader);
        await reader.NextAsync(); Render();
        var viewport = Bounds(Element("miko-reader-viewport"));
        int page = reader.PageIndex, count = reader.PageCount;
        foreach (var open in new Action[] { reader.ShowControls, reader.ToggleSettings, reader.ToggleAppearance, reader.ToggleContents })
        {
            open(); Render();
            Bounds(Element("miko-reader-viewport")).ShouldBe(viewport);
            reader.PageIndex.ShouldBe(page);
            reader.PageCount.ShouldBe(count);
        }
        // Tapping the exposed scrim closes the sheet without navigating or turning a page.
        Tap(200, Bounds(Element("miko-reader-panel")).Y / 2);
        reader.IsContentsOpen.ShouldBeFalse();
        reader.AreControlsVisible.ShouldBeTrue();
        reader.PageIndex.ShouldBe(page);
        exits.ShouldBe(0);

        reader.ToggleSettings(); Render();
        _platform.Back.Dispatch().ShouldBeTrue(); Render();
        reader.IsSettingsOpen.ShouldBeFalse();
        reader.AreControlsVisible.ShouldBeTrue();
        _platform.Back.Dispatch().ShouldBeTrue(); Render();
        reader.AreControlsVisible.ShouldBeFalse();
        exits.ShouldBe(0);
        _platform.Back.Dispatch().ShouldBeTrue();
        exits.ShouldBe(1);
        reader.Dispose();
        _platform.Back.Dispatch().ShouldBeFalse();
    }

    [Theory]
    [InlineData(320, 568, 0, 24, 0, 20)]
    [InlineData(390, 844, 0, 44, 0, 34)]
    [InlineData(844, 390, 44, 0, 44, 21)]
    [InlineData(568, 320, 0, 20, 0, 20)]
    public void Phone_Controls_Respect_Safe_Areas_And_Settings_Remain_Scrollable(int width, int height, int left, int top, int right, int bottom)
    {
        var reader = new BookReader { Source = BookSource(), Immersive = false };
        Show(reader);
        _engine.SetViewportSize(width, height);
        _engine.SetSafeAreaInsets(new SafeAreaInsets(left, top, right, bottom)); Render();
        var back = Bounds(Element("miko-reader-back"));
        back.Y.ShouldBeGreaterThanOrEqualTo(top);
        back.X.ShouldBeGreaterThanOrEqualTo(left);
        foreach (var name in new[] { "miko-reader-back", "miko-reader-previous", "miko-reader-next", "miko-reader-settings", "miko-reader-progress" })
        {
            var bounds = Bounds(Element(name));
            bounds.Width.ShouldBeGreaterThanOrEqualTo(44);
            bounds.Height.ShouldBeGreaterThanOrEqualTo(44);
            bounds.Right.ShouldBeLessThanOrEqualTo(width - right);
            bounds.Bottom.ShouldBeLessThanOrEqualTo(height - bottom);
        }
        reader.ToggleSettings(); Render();
        var panel = Bounds(Element("miko-reader-panel"));
        panel.Y.ShouldBeGreaterThanOrEqualTo(top);
        panel.Bottom.ShouldBeLessThanOrEqualTo(height - bottom - 126 + .5f);
        var body = Element("miko-reader-panel-body");
        body.OffsetHeight.ShouldBeGreaterThan(0);
        body.Style!.OverflowY.ShouldBe(Overflow.Auto);
        var close = Bounds(Element("miko-reader-panel-close"));
        Tap(close.X + close.Width / 2, close.Y + close.Height / 2);
        reader.IsSettingsOpen.ShouldBeFalse();
    }

    [Fact]
    public void Progress_Previews_Without_Loading_And_Commits_The_Final_Text_Page_On_Release()
    {
        var source = (ReaderControllerTests.Source)BookSource();
        var reader = new BookReader { Source = source, Immersive = false };
        Show(reader);
        var slider = Bounds(Element("miko-reader-progress"));
        float y = slider.Y + slider.Height / 2;
        _input.OnPointerDown(slider.X + 12, y, MouseButton.Left, PointerType.Touch);
        for (int i = 1; i <= 4; i++)
        {
            _input.OnPointerMove(slider.X + 12 + (slider.Width - 24) * i / 4, y); Render();
        }
        source.Reads.ShouldBe(["0"]);
        reader.CurrentIndex.ShouldBe(0);
        Element("miko-reader-progress-text").TextContent.ShouldBe("100%");
        _input.OnPointerUp(slider.Right - 12, y, MouseButton.Left, PointerType.Touch); Render();
        source.Reads.ShouldBe(["0", "1"]);
        reader.CurrentIndex.ShouldBe(1);
        reader.PageIndex.ShouldBe(reader.PageCount - 1);
        Element("miko-reader-progress-text").TextContent.ShouldBe("100%");
    }

    private RectF Bounds(Element element) => Find(_engine.GetCurrentLayout()!, element)!.BoxModel.BorderBox;
    private static LayoutBox? Find(LayoutBox box, Element element) => box.Element == element ? box
        : box.Children.Select(child => Find(child, element)).FirstOrDefault(child => child != null);

    [Fact]
    public void Book_Layout_Contains_Every_Line_And_Page_Height_Does_Not_Clip_Lines()
    {
        var text = string.Join('\n', Enumerable.Repeat("你好 world 👩🏽‍💻 café مرحبا", 15));
        var layout = new BookLayout(text, new("sans-serif", 18, 1.6f, 12, 20), 140, 300, _engine.Graphics);
        layout.Lines.Count.ShouldBeGreaterThan(15);
        for (int page = 0; page < layout.Pages.Count; page++)
        {
            int start = layout.Pages[page];
            int end = page + 1 < layout.Pages.Count ? layout.Pages[page + 1] : layout.Lines.Count;
            (layout.Lines[end - 1].Top + layout.Lines[end - 1].Height - layout.Lines[start].Top).ShouldBeLessThanOrEqualTo(layout.ContentHeight);
        }
        layout.Lines.Count(l => l.Text.Text.Contains("café")).ShouldBe(15);
    }

    private void Tap(float x, float y)
    {
        _input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
        _input.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch);
        Render();
    }

    private IReaderSource ImageSource()
    {
        using var page = _engine.Graphics.CreateSurface(120, 180);
        var data = new List<ReaderContent>();
        foreach (string color in new[] { "#ff0000", "#0000ff", "#00ff00" })
        {
            page.Context.Clear(Color.FromHex(color));
            data.Add(new ReaderImageContent(page.EncodePng()));
        }
        return new ReaderControllerTests.Source(3) { Read = (entry, _) => Task.FromResult(data[int.Parse(entry.Id)]) };
    }

    public void Dispose() { _reader?.Dispose(); _surface.Dispose(); _services.Dispose(); }
}
