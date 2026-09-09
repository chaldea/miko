using Miko.Styling;
using Miko.Ionic.Components;

namespace Miko.Ionic;

/// <summary>
/// Registers component styles lazily and returns the class that isolates a resolved theme's
/// rule set. One rule set is retained for each component family, mode, and effective theme.
/// </summary>
public sealed class IonicStyleRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _scopes = new(StringComparer.Ordinal);

    /// <summary>The stylesheet attached once during <see cref="IonicExtensions.AddIonic"/>.</summary>
    public StyleSheet StyleSheet { get; } = IonicStyleSheetFactory.CreateGlobal();

    /// <summary>
    /// Ensures the styles for <paramref name="componentType"/> and returns their scope class.
    /// Components without generated styles return <see langword="null"/>.
    /// </summary>
    public string? Register(Type componentType, IonicMode mode, IonicTheme theme)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(theme);

        string? component = null;
        for (var type = componentType; type != null && component == null; type = type.BaseType)
            component = GetComponentFamily(type.Name);
        if (component is null)
            return null;

        var key = theme.GetStyleKey(component, mode);
        lock (_gate)
        {
            if (_scopes.TryGetValue(key, out var scope))
                return scope;

            var css = IonicStyleSheetFactory.CreateComponentStyle(component, mode == IonicMode.Ios ? "ios" : "md", theme);
            if (css is null)
                return null;

            scope = $"ion-theme-{key}";
            if (component == "Item")
                IonicStyleScope.AddTo(StyleSheet, BaseItemStyles.GenStyle(mode == IonicMode.Ios ? "ios" : "md", theme), component, scope);
            IonicStyleScope.AddTo(StyleSheet, css, component, scope);
            _scopes.Add(key, scope);
            return scope;
        }
    }

    private static string? GetComponentFamily(string componentTypeName) => componentTypeName switch
    {
        "IonAccordion" or "IonAccordionGroup" => "Accordion",
        "IonActionSheet" => "ActionSheet",
        "IonAlert" => "Alert",
        "IonAvatar" => "Avatar",
        "IonBackButton" => "BackButton",
        "IonBadge" => "Badge",
        "IonBreadcrumb" or "IonBreadcrumbs" => "Breadcrumb",
        "IonButton" => "Button",
        "IonButtons" or "IonApp" or "IonMenu" or "IonMenuButton" => "Menu",
        "IonCard" or "IonCardContent" or "IonCardHeader" or "IonCardSubtitle" or "IonCardTitle" => "Card",
        "IonCheckbox" => "Checkbox",
        "IonChip" => "Chip",
        "IonContent" => "Content",
        "IonDatetime" or "IonDatetimeButton" => "Datetime",
        "IonFab" or "IonFabButton" or "IonFabList" => "Fab",
        "IonFooter" => "Footer",
        "IonGrid" or "IonRow" or "IonCol" => "Grid",
        "IonHeader" => "Header",
        "IonIcon" => "Icon",
        "IonInfiniteScroll" or "IonInfiniteScrollContent" => "InfiniteScroll",
        "IonInput" => "Input",
        "IonInputOtp" => "InputOtp",
        "IonItem" or "IonItemDivider" or "IonItemGroup" or "IonItemOption" or "IonItemOptions" or "IonItemSliding" => "Item",
        "IonLabel" => "Label",
        "IonList" or "IonListHeader" => "List",
        "IonLoading" => "Loading",
        "IonModal" => "Modal",
        "IonNote" => "Note",
        "IonOverlayHost" => "Overlay",
        "IonPage" => "Page",
        "IonPicker" or "IonPickerColumn" or "IonPickerColumnOption" => "Picker",
        "IonPopover" => "Popover",
        "IonProgressBar" => "ProgressBar",
        "IonRadio" or "IonRadioGroup" => "Radio",
        "IonRange" => "Range",
        "IonRefresher" or "IonRefresherContent" => "Refresher",
        "IonReorder" or "IonReorderGroup" => "Reorder",
        "IonSearchbar" => "Searchbar",
        "IonSegment" or "IonSegmentButton" or "IonSegmentContent" or "IonSegmentView" => "Segment",
        "IonSelect" or "IonSelectModal" or "IonSelectOption" or "IonSelectPopover" => "Select",
        "IonSkeletonText" => "SkeletonText",
        "IonSlide" or "IonSlides" => "Slides",
        "IonSpinner" => "Spinner",
        "IonTabBar" or "IonTabButton" or "IonTabs" => "Tab",
        "IonText" => "Text",
        "IonTextarea" => "Textarea",
        "IonThumbnail" => "Thumbnail",
        "IonTitle" => "Title",
        "IonToast" => "Toast",
        "IonToggle" => "Toggle",
        "IonToolbar" => "Toolbar",
        _ => null,
    };
}
