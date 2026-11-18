using Miko.Common;
using Miko.Components.Reader;

namespace MikoApp.Reader;

public sealed class ShelfBook(string title, string description, bool isComic, IReaderSource source, string color)
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public bool IsComic { get; } = isComic;
    public IReaderSource Source { get; } = source;
    public Color Color { get; } = Color.FromHex(color);
    public int Index { get; set; }
    public float FontSize { get; set; } = 20;
    public string FontFamily { get; set; } = "serif";
    public float LineSpacing { get; set; } = 1.7f;
    public float ParagraphSpacing { get; set; } = 12;
    public float PageMargin { get; set; } = 24;
    public ReaderTheme Theme { get; set; } = ReaderTheme.Sepia;
    public bool EyeCare { get; set; }
    public BookReadingMode Mode { get; set; }
    public ComicPageMode PageMode { get; set; }
    public ReaderDirection Direction { get; set; }
    public ReaderOrientation Orientation { get; set; }
    public bool FitWidth { get; set; }
    public Color ComicBackground { get; set; } = Color.FromHex("#202124");
    public float Dimness { get; set; }

    public static List<ShelfBook> Samples() =>
    [
        new("山间来信", "在一段安静的山路上，读一个缓慢的故事。", false, SampleSource.Create(false), "#6d8978"),
        new("山间来信 · 绘本", "跟随画面，走进山谷的四季。", true, SampleSource.Create(true), "#cf9b68"),
        new("雨后的街角", "关于雨、咖啡和重逢的短篇。", false, SampleSource.Create(false, "雨后的街角", "雨停了，街角的咖啡馆打开了门。阳光落在旧木桌上，窗外有人收起伞，在玻璃上留下了一道细细的水痕。"), "#7992a0"),
        new("向海而行", "从右向左，翻开旅途中的风景。", true, SampleSource.Create(true, "向海而行"), "#4e777c") { Direction = ReaderDirection.RightToLeft },
    ];
}
