namespace Miko.Components.Reader;

public sealed record ReaderTexts
{
    public string Back { get; init; } = "Back";
    public string Close { get; init; } = "Done";
    public string Appearance { get; init; } = "Appearance";
    public string Reading { get; init; } = "Reading";
    public string Progress { get; init; } = "Progress";
    public string Previous { get; init; } = "Previous";
    public string Next { get; init; } = "Next";
    public string Dimness { get; init; } = "Screen shade";
    public string Current { get; init; } = "Reading now";
    public string Serif { get; init; } = "Serif";
    public string SansSerif { get; init; } = "Sans serif";
    public string Monospace { get; init; } = "Mono";
    public string Contents { get; init; } = "Contents";
    public string Settings { get; init; } = "Settings";
    public string Loading { get; init; } = "Loading…";
    public string Empty { get; init; } = "No pages";
    public string NoSource { get; init; } = "Choose a book or comic to start reading";
    public string Retry { get; init; } = "Retry";
    public string Font { get; init; } = "Font";
    public string FontSize { get; init; } = "Font size";
    public string LineSpacing { get; init; } = "Line spacing";
    public string ParagraphSpacing { get; init; } = "Paragraph spacing";
    public string Margin { get; init; } = "Page margin";
    public string Theme { get; init; } = "Theme";
    public string Light { get; init; } = "Light";
    public string Sepia { get; init; } = "Sepia";
    public string Dark { get; init; } = "Dark";
    public string EyeCare { get; init; } = "Eye care";
    public string Mode { get; init; } = "Mode";
    public string Paged { get; init; } = "Pages";
    public string Scroll { get; init; } = "Scroll";
    public string Single { get; init; } = "Single page";
    public string Double { get; init; } = "Two pages";
    public string Direction { get; init; } = "Reading direction";
    public string LeftToRight { get; init; } = "Left to right";
    public string RightToLeft { get; init; } = "Right to left";
    public string Orientation { get; init; } = "Page axis";
    public string Horizontal { get; init; } = "Horizontal";
    public string Vertical { get; init; } = "Vertical";
    public string FitWidth { get; init; } = "Fit width";
    public string On { get; init; } = "On";
    public string Off { get; init; } = "Off";
    public string Zoom { get; init; } = "Zoom";

    public static ReaderTexts Chinese { get; } = new()
    {
        Back = "返回书架", Close = "完成", Appearance = "外观", Reading = "阅读", Progress = "阅读进度", Previous = "上一页", Next = "下一页",
        Dimness = "阅读遮光", Current = "正在阅读", Serif = "宋体", SansSerif = "黑体", Monospace = "等宽",
        Contents = "目录", Settings = "设置", Loading = "加载中…", Empty = "暂无内容", NoSource = "选择书籍或漫画开始阅读", Retry = "重试",
        Font = "字体", FontSize = "字号", LineSpacing = "行间距", ParagraphSpacing = "段落间距", Margin = "页面距", Theme = "主题",
        Light = "明亮", Sepia = "纸张", Dark = "夜间", EyeCare = "护眼", Mode = "阅读模式", Paged = "翻页", Scroll = "滚动",
        Single = "单页", Double = "双页", Direction = "阅读顺序", LeftToRight = "从左到右", RightToLeft = "从右到左", Orientation = "翻页方向",
        Horizontal = "横向", Vertical = "纵向", FitWidth = "适应宽度", On = "开", Off = "关", Zoom = "缩放",
    };
}
