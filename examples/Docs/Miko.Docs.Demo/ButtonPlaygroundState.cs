namespace Miko.Docs.Demo;

public enum ButtonDemoScene
{
    Component,
    Toolbar,
    Icons,
}

public enum ButtonIconPosition
{
    Start,
    End,
    IconOnly,
}

public sealed record PlaygroundLogEntry(int Id, string Time, string Kind, string Message);

/// <summary>Designer state shared by the property panel, preview and interaction console.</summary>
public sealed class ButtonPlaygroundState
{
    private const int MaxEvents = 4;
    private readonly List<PlaygroundLogEntry> _events = [];
    private int _nextEventId;

    public event Action? Changed;

    public ButtonPlaygroundState() => Record("ready", "IonButton playground initialized");

    public ButtonDemoScene Scene { get; set; } = ButtonDemoScene.Component;
    public string Color { get; set; } = "primary";
    public string Fill { get; set; } = "solid";
    public string Size { get; set; } = "default";
    public string Shape { get; set; } = "default";
    public string? Expand { get; set; }
    public bool Strong { get; set; }
    public bool Disabled { get; set; }
    public string? Icon { get; set; }
    public ButtonIconPosition IconPosition { get; set; } = ButtonIconPosition.Start;
    public string ToolbarColor { get; set; } = "light";
    public int ClickCount { get; private set; }
    public IReadOnlyList<PlaygroundLogEntry> Events => _events;

    public string? ExpandValue => Expand;
    public string? ShapeValue => Shape == "round" ? "round" : null;

    public void Record(string kind, string message)
    {
        _nextEventId++;
        _events.Insert(0, new PlaygroundLogEntry(_nextEventId, DateTime.Now.ToString("HH:mm:ss"), kind, message));
        if (_events.Count > MaxEvents) _events.RemoveAt(_events.Count - 1);
        Changed?.Invoke();
    }

    public void Click(string source)
    {
        ClickCount++;
        Record("click", $"{source} activated · total {ClickCount}");
    }

    public void ClearEvents() => _events.Clear();

    public void Reset()
    {
        Color = "primary";
        Fill = "solid";
        Size = "default";
        Shape = "default";
        Expand = null;
        Strong = false;
        Disabled = false;
        Icon = null;
        IconPosition = ButtonIconPosition.Start;
        ToolbarColor = "light";
        ClickCount = 0;
        Record("reset", "Component properties restored to their defaults");
    }
}
