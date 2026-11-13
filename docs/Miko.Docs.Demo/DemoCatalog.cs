using System.Diagnostics.CodeAnalysis;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Drawing.Geometries;
using Miko.Charts;
using Miko.Common;
using Miko.Components;
using Miko.Components.Player;
using Miko.Components.Player.Danmaku;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Styling;

namespace Miko.Docs.Demo;

/// <summary>
/// The demo navigation mirrors the component families in the public documentation.
/// Editable parameters and events are discovered from the component type, not repeated here.
/// </summary>
public static class DemoCatalog
{
    private static readonly Dictionary<string, DemoDescriptor> ById;

    static DemoCatalog()
    {
        All =
        [
            Ionic<IonAccordionGroup>("accordion", "Accordion", "Collapsible sections with single or multiple expansion."),
            Ionic<IonActionSheet>("action-sheet", "Action Sheet", "A sliding sheet that offers a set of contextual actions.",
                preset: P("Header", "Choose an action", "SubHeader", "Your changes are saved automatically", "IsOpen", false,
                    "Buttons", new[]
                    {
                        new IonActionSheetButton { Text = "Share", Icon = Ionicons.Share, Data = "share" },
                        new IonActionSheetButton { Text = "Duplicate", Icon = Ionicons.Copy, Data = "duplicate" },
                        new IonActionSheetButton { Text = "Cancel", Role = "cancel", Icon = Ionicons.Close },
                    })),
            Ionic<IonAlert>("alert", "Alert", "A focused dialog for confirmation, messages and choices.",
                preset: P("Header", "Delete draft?", "SubHeader", "This action cannot be undone", "Message", "The published document will not change.",
                    "IsOpen", false,
                    "Buttons", new[] { new IonAlertButton { Text = "Cancel", Role = "cancel" }, new IonAlertButton { Text = "Delete", Role = "destructive" } })),
            Ionic<IonPage>("app", "App & Page", "The structural containers shared by every Ionic screen.", titleOverride: "App & Page"),
            Ionic<IonAvatar>("avatar", "Avatar", "A circular image for people, contacts and objects."),
            Ionic<IonBackButton>("back-button", "Back Button", "Navigation-aware back action with customizable content.",
                preset: P("Text", "Back", "DefaultHref", "/")),
            Ionic<IonBadge>("badge", "Badge", "A compact status or count indicator.", preset: P("Color", "primary")),
            Ionic<IonBreadcrumbs>("breadcrumbs", "Breadcrumbs", "A hierarchy of links with responsive collapsing."),
            Ionic<IonButton>("button", "Button", "A clickable action with contextual toolbar and icon layouts.",
                scenes:
                [
                    new DemoScene("toolbar", "In a toolbar", "Compact metrics supplied by context"),
                    new DemoScene("icons", "With icons", "Leading, trailing and icon-only slots"),
                ],
                preset: P("Color", "primary", "Fill", "solid", "Size", "default", "Shape", "default")),
            Ionic<IonCard>("card", "Card", "A raised surface for media, text, lists and actions."),
            Ionic<IonCheckbox>("checkbox", "Checkbox", "Independent selections with labels and validation hints.",
                preset: P("Checked", true, "LabelPlacement", "end", "Color", "primary")),
            Ionic<IonChip>("chip", "Chip", "Compact tags, filters and contact tokens.", preset: P("Color", "primary")),
            Ionic<IonContent>("content", "Content", "The scrollable region between page headers and footers."),
            Ionic<IonDatetime>("datetime", "Datetime", "Calendar, time and wheel-based date selection.",
                scenes: [new DemoScene("wheels", "Wheel presentation", "PreferWheel changes the editor presentation", P("PreferWheel", true))],
                preset: P("Value", "2026-11-12", "Presentation", "date", "ShowAdjacentDays", true)),
            Ionic<IonFab>("fab", "Floating Action Button", "A prominent floating action with optional speed-dial actions.",
                scenes: [new DemoScene("list", "Action list", "Coordinated FabButton and FabList children", P("Activated", true))]),
            Ionic<IonFooter>("footer", "Footer", "A persistent bottom region for toolbars and actions."),
            Ionic<IonGrid>("grid", "Grid", "A twelve-column responsive layout system."),
            Ionic<IonHeader>("header", "Header", "A persistent top region containing one or more toolbars."),
            Ionic<IonIcon>("icon", "Icon", "Bundled Ionicons and custom SVG sources.",
                preset: P("Icon", "heart", "Color", "primary", "Size", "large")),
            Ionic<IonInfiniteScroll>("infinite-scroll", "Infinite Scroll", "Loads more rows as the user approaches a scroll boundary.",
                hidden: ["OnInfinite"]),
            Ionic<IonInput>("input", "Input", "Single-line text entry with labels, hints and validation.",
                scenes: [new DemoScene("item", "Inside an item", "Form metrics supplied by IonItem")],
                preset: P("Label", "Display name", "LabelPlacement", "stacked", "Placeholder", "Ada Lovelace", "Value", "Miko",
                    "HelperText", "This name appears on your profile", "ClearInput", true)),
            Ionic<IonInputOtp>("input-otp", "Input OTP", "A row of character boxes for one-time codes.",
                preset: P("Length", 6, "Type", "number", "Value", "2048", "Fill", "outline")),
            Ionic<IonItem>("item", "Item", "List rows with slots, navigation, form controls and swipe actions.",
                scenes: [new DemoScene("sliding", "Sliding actions", "IonItemOptions revealed by a swipe")]),
            Ionic<IonLabel>("label", "Label", "Text styled for items and standalone content."),
            Ionic<IonList>("list", "List", "A vertical container for items, headers and groups."),
            Ionic<IonLoading>("loading", "Loading", "A blocking spinner overlay for work in progress.",
                preset: P("Message", "Preparing your workspace…", "Duration", 0, "IsOpen", false)),
            Ionic<IonMenu>("menu", "Menu", "A controlled navigation drawer with start or end placement.",
                preset: P("Side", "start", "IsOpen", true)),
            Ionic<IonModal>("modal", "Modal", "A full or sheet-style dialog with application content.",
                scenes: [new DemoScene("sheet", "Sheet modal", "Breakpoints and a draggable handle",
                    P("Breakpoints", new[] { 0d, .65d, 1d }, "InitialBreakpoint", .65d, "Handle", true, "IsOpen", true))],
                preset: P("IsOpen", false, "BackdropDismiss", true)),
            Ionic<IonNote>("note", "Note", "Muted secondary text for metadata and timestamps."),
            Ionic<IonPicker>("picker", "Picker", "One or more independent scrollable option wheels."),
            Ionic<IonPopover>("popover", "Popover", "A small anchored panel for contextual content.",
                preset: P("IsOpen", false, "Side", "bottom", "Alignment", "center", "Arrow", true)),
            Ionic<IonProgressBar>("progress-bar", "Progress Bar", "Determinate, buffered and indeterminate progress.",
                scenes: [new DemoScene("toolbar", "In a toolbar", "Progress attached to the toolbar context")],
                preset: P("Type", "determinate", "Value", 0.45, "Buffer", 0.7, "Color", "primary")),
            Ionic<IonRadioGroup>("radio", "Radio", "A single choice within a coordinated radio group.", titleOverride: "Radio",
                preset: P("Value", "miko", "AllowEmptySelection", false)),
            Ionic<IonRange>("range", "Range", "A draggable value slider with labels, ticks and a value pin.",
                scenes: [new DemoScene("item", "Inside an item", "Range aligned with item content")],
                preset: P("Value", 40d, "Min", 0d, "Max", 100d, "Step", 1d, "Label", "Volume", "Pin", true)),
            Ionic<IonRefresher>("refresher", "Refresher", "Pull-to-refresh feedback for scrollable content.",
                hidden: ["OnRefresh"]),
            Ionic<IonReorderGroup>("reorder", "Reorder", "Drag handles that request application-owned list reordering.", titleOverride: "Reorder",
                preset: P("Disabled", false), hidden: ["OnItemReorder"]),
            Ionic<IonSearchbar>("searchbar", "Searchbar", "Search input with clear and cancel behavior.",
                scenes: [new DemoScene("toolbar", "In a toolbar", "Search presentation inside navigation chrome")],
                preset: P("Placeholder", "Search components", "Value", "Button", "ShowClearButton", "focus")),
            Ionic<IonSegment>("segment", "Segment", "A row of exclusive choices with optional coordinated views.",
                scenes: [new DemoScene("views", "Segment views", "Content coordinated through a shared value")],
                preset: P("Value", "overview", "Color", "primary")),
            Ionic<IonSelect>("select", "Select", "Single or multiple choices presented through an overlay.",
                scenes: [new DemoScene("multiple", "Multiple selection", "A values collection with checkboxes",
                    P("Multiple", true, "Values", new[] { "miko", "ionic" }))],
                preset: P("Label", "Favorite framework", "LabelPlacement", "stacked", "Value", "miko", "Placeholder", "Choose one", "Interface", "alert")),
            Ionic<IonSkeletonText>("skeleton-text", "Skeleton Text", "Animated placeholders for content being loaded.",
                preset: P("Animated", true)),
            Ionic<IonSlides>("slides", "Slides", "A swipeable carousel with navigation, pagination and autoplay.",
                scenes: [new DemoScene("fade", "Fade effect", "Cross-fade transitions instead of sliding", P("Effect", "fade", "ActiveIndex", 0))],
                preset: P("ActiveIndex", 0, "Navigation", true, "Pagination", true, "AllowTouchMove", true)),
            Ionic<IonSpinner>("spinner", "Spinner", "Animated activity indicators in several styles.",
                preset: P("Name", "crescent", "Color", "primary")),
            Ionic<IonTabs>("tabs", "Tabs", "Top-level destinations with a top or bottom tab bar.",
                scenes: [new DemoScene("top", "Top bar", "The tab bar supplied through the Top fragment")]),
            Ionic<IonText>("text", "Text", "Inline content colored from the Ionic palette.", preset: P("Color", "primary")),
            Ionic<IonTextarea>("textarea", "Textarea", "Multi-line text entry with auto-grow and validation hints.",
                preset: P("Label", "Project notes", "LabelPlacement", "stacked", "Rows", 4, "AutoGrow", true,
                    "Value", "Miko renders Razor components natively with SkiaSharp.", "Counter", true, "Maxlength", 160)),
            Ionic<IonThumbnail>("thumbnail", "Thumbnail", "A square media preview for items and lists."),
            Ionic<IonTitle>("title", "Title", "The toolbar title for the current screen."),
            Ionic<IonToast>("toast", "Toast", "A lightweight notification with optional actions.",
                scenes: [new DemoScene("positions", "Top position", "The same toast in a different viewport position", P("Position", "top", "IsOpen", true))],
                preset: P("Header", "Miko", "Message", "Your changes have been saved.", "Position", "bottom", "Duration", 0,
                    "Color", "dark", "IsOpen", false,
                    "Buttons", new[] { new IonToastButton { Text = "Undo", Side = "end", Role = "cancel" } })),
            Ionic<IonToggle>("toggle", "Toggle", "A switch for one boolean setting.",
                preset: P("Checked", true, "LabelPlacement", "end", "Color", "primary")),
            Ionic<IonToolbar>("toolbar", "Toolbar", "A horizontal container for titles, buttons and controls.",
                scenes: [new DemoScene("controls", "With controls", "Segment and search content in toolbar context")]),
            Chart<CartesianChart>("cartesian", "Cartesian Chart", "Columns and a line with axes, legends, tooltips and zooming.",
                preset: P(
                    "Series", new ISeries[]
                    {
                        new ColumnSeries<double>([42, 55, 61, 48, 70, 82]) { Name = "Revenue" },
                        new LineSeries<double>([38, 41, 52, 50, 60, 74]) { Name = "Target", Fill = null, GeometrySize = 10 },
                    },
                    "XAxes", new[] { new Axis { Labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"] } },
                    "LegendPosition", LegendPosition.Top, "ZoomMode", ZoomAndPanMode.X)),
            Chart<PieChart>("pie", "Pie Chart", "A doughnut with configurable direction, rotation and angle.",
                preset: P(
                    "Series", new ISeries[]
                    {
                        new PieSeries<double>([42]) { Name = "Desktop", InnerRadius = 50 },
                        new PieSeries<double>([33]) { Name = "Android", InnerRadius = 50 },
                        new PieSeries<double>([18]) { Name = "iOS", InnerRadius = 50 },
                        new PieSeries<double>([7]) { Name = "Other", InnerRadius = 50 },
                    },
                    "LegendPosition", LegendPosition.Right)),
            Chart<PolarChart>("polar", "Polar Chart", "Two closed radar series compared across six axes.",
                preset: P(
                    "Series", new ISeries[]
                    {
                        new PolarLineSeries<double>([4, 3, 5, 2, 4, 3]) { Name = "Team A", IsClosed = true, LineSmoothness = 0 },
                        new PolarLineSeries<double>([2, 5, 3, 4, 2, 5]) { Name = "Team B", IsClosed = true, LineSmoothness = 0 },
                    },
                    "AngleAxes", new[] { new PolarAxis { Labels = ["Speed", "Power", "Range", "Comfort", "Safety", "Price"], MinStep = 1, ForceStepToMin = true } },
                    "LegendPosition", LegendPosition.Top)),
            Chart<GeoMap>("geo-map", "Geo Map", "An embedded world heat map with projection and drag panning.",
                preset: P(
                    "Series", new[]
                    {
                        new HeatLandSeries
                        {
                            Lands =
                            [
                                new HeatLand { Name = "chn", Value = 12 }, new HeatLand { Name = "usa", Value = 9 },
                                new HeatLand { Name = "bra", Value = 6 }, new HeatLand { Name = "ind", Value = 10 },
                                new HeatLand { Name = "deu", Value = 5 }, new HeatLand { Name = "jpn", Value = 7 },
                                new HeatLand { Name = "aus", Value = 3 }, new HeatLand { Name = "zaf", Value = 2 },
                            ],
                        },
                    },
                    "Style", new Style { Height = Length.Px(380) })),
            Component<MikoPlayer>("player", "Player", "Playback chrome, settings, fullscreen and danmaku over a local simulated session.",
                scenes: [new DemoScene("danmaku", "Danmaku & fullscreen", "Overlay comments and viewport fullscreen")],
                preset: P("Source", MediaSource.Parse("demo.mp4"), "MimeType", "video/mp4", "AutoPlay", false,
                    "Loop", true, "Muted", true, "Volume", 0.8f, "PlaybackRate", 1f,
                    "Style", new Style { Width = Length.Percent(100), Height = Length.Px(360) },
                    "Danmaku", new[]
                    {
                        new DanmakuItem(TimeSpan.FromSeconds(1), "Miko renders every pixel natively"),
                        new DanmakuItem(TimeSpan.FromSeconds(4), "A local, deterministic media timeline", DanmakuMode.Top),
                        new DanmakuItem(TimeSpan.FromSeconds(8), "No network or hardware required", DanmakuMode.Bottom),
                    },
                    "DanmakuSettings", new DanmakuSettings { Enabled = true, Opacity = .9f, FontSize = 20, Duration = 8, Area = .7f, MaxVisible = 40 }),
                hidden: ["MimeType", "Poster"]),
        ];
        ById = All.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<DemoDescriptor> All { get; }

    public static DemoDescriptor Get(string id) => ById.TryGetValue(id, out var descriptor)
        ? descriptor
        : throw new KeyNotFoundException($"Unknown demo component '{id}'.");

    public static IEnumerable<DemoDescriptor> Category(DemoCategory category) => All.Where(item => item.Category == category);

    private static Dictionary<string, object?> P(params object?[] values)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var i = 0; i < values.Length; i += 2) result[(string)values[i]!] = values[i + 1];
        return result;
    }

    private static DemoDescriptor Ionic<[DynamicallyAccessedMembers(DemoDescriptor.ComponentActivation)] T>(
        string id,
        string title,
        string description,
        string? titleOverride = null,
        DemoScene[]? scenes = null,
        Dictionary<string, object?>? preset = null,
        string[]? hidden = null) where T : ComponentBase, new()
        => DemoDescriptorFactory.For<T>(id, DemoCategory.Ionic, description, $"components/ionic/{id}.md",
            title: titleOverride ?? title, scenes: scenes, presetParameters: preset, hiddenProperties: hidden);

    private static DemoDescriptor Chart<[DynamicallyAccessedMembers(DemoDescriptor.ComponentActivation)] T>(
        string id,
        string title,
        string description,
        Dictionary<string, object?>? preset = null,
        DemoScene[]? scenes = null) where T : ComponentBase, new()
        => DemoDescriptorFactory.For<T>(id, DemoCategory.Charts, description, "components/charts.md",
            title: title, scenes: scenes, presetParameters: preset, showMode: false);

    private static DemoDescriptor Component<[DynamicallyAccessedMembers(DemoDescriptor.ComponentActivation)] T>(
        string id,
        string title,
        string description,
        Dictionary<string, object?>? preset = null,
        DemoScene[]? scenes = null,
        string[]? hidden = null) where T : ComponentBase, new()
        => DemoDescriptorFactory.For<T>(id, DemoCategory.Components, description, "packages.md",
            title: title, scenes: scenes, presetParameters: preset, hiddenProperties: hidden, showMode: false);
}
