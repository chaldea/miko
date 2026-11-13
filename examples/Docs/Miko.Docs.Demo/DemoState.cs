using System.Globalization;
using System.Reflection;
using Miko.Components;
using Miko.Platform;

namespace Miko.Docs.Demo;

public sealed record DemoLogEntry(int Id, string Time, string Kind, string Message);

/// <summary>Shared state for navigation, generated properties, scenes and the interaction console.</summary>
public sealed class DemoState
{
    private const int MaxEvents = 4;
    private readonly Dictionary<string, ComponentState> _components = new(StringComparer.OrdinalIgnoreCase);
    private readonly PlatformInfo _platform;
    private int _nextEventId;

    public DemoState(PlatformInfo platform)
    {
        _platform = platform;
        Current = DemoCatalog.All[0];
    }

    public event Action? Changed;
    /// <summary>Raised only when the preview must render a different component route.</summary>
    public event Action? ComponentChanged;
    /// <summary>Raised when the current preview's properties, scene or platform change.</summary>
    public event Action? PreviewChanged;
    /// <summary>Raised only when the scene selector and preview toolbar need updating.</summary>
    public event Action? SceneChanged;
    /// <summary>Raised when the optional interaction console is expanded or collapsed.</summary>
    public event Action? ConsoleVisibilityChanged;

    public DemoDescriptor Current { get; private set; }
    public ComponentState CurrentComponent => GetComponent(Current);
    public string SceneId => CurrentComponent.SceneId;
    public IReadOnlyList<DemoLogEntry> Events => CurrentComponent.Events;
    public int ClickCount => CurrentComponent.ClickCount;
    public bool IsIos => _platform.Platform == HostPlatform.Ios;
    public string Mode => IsIos ? "ios" : "md";
    public bool ConsoleExpanded { get; private set; }

    public void ToggleConsole()
    {
        ConsoleExpanded = !ConsoleExpanded;
        ConsoleVisibilityChanged?.Invoke();
    }

    public void Activate(string id)
    {
        var descriptor = DemoCatalog.Get(id);
        if (ReferenceEquals(Current, descriptor)) return;
        Current = descriptor;
        Changed?.Invoke();
        ComponentChanged?.Invoke();
    }

    public void SelectScene(string sceneId)
    {
        var component = CurrentComponent;
        if (component.SceneId == sceneId) return;
        var previousScene = Current.Scenes.FirstOrDefault(item => item.Id == component.SceneId);
        if (previousScene?.PresetParameters is not null)
        {
            foreach (var name in previousScene.PresetParameters.Keys)
                component.Values[name] = component.DefaultValues.GetValueOrDefault(name);
        }
        component.SceneId = sceneId;
        var scene = Current.Scenes.FirstOrDefault(item => item.Id == sceneId);
        if (scene?.PresetParameters is not null)
        {
            foreach (var (name, value) in scene.PresetParameters) component.Values[name] = value;
        }
        PreviewChanged?.Invoke();
        SceneChanged?.Invoke();
        Record(component, "scene", scene is null ? "Main component example" : $"Switched to {scene.Name}");
    }

    public void SetMode(string mode)
    {
        var next = mode == "ios" ? HostPlatform.Ios : HostPlatform.Android;
        if (_platform.Platform == next) return;
        _platform.Platform = next;
        PreviewChanged?.Invoke();
        Record(CurrentComponent, "mode", $"Ionic design mode = {mode}");
    }

    public object? Value(string name) => CurrentComponent.Values.GetValueOrDefault(name);

    public T Value<T>(string name, T fallback = default!)
    {
        var value = Value(name);
        if (value is T typed) return typed;
        if (value is null) return fallback;
        try
        {
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }

    public void Set(string name, object? value, bool log = true)
    {
        var component = CurrentComponent;
        if (component.Values.TryGetValue(name, out var previous) && Equals(previous, value)) return;
        component.Values[name] = value;
        PreviewChanged?.Invoke();
        if (log)
        {
            Record(component, "property", $"{SplitName(name)} = {DemoProperty.FormatValue(value)}");
        }
        else
        {
            Changed?.Invoke();
        }
    }

    public void SetText(DemoProperty property, string? text)
    {
        try
        {
            Set(property.Name, property.ConvertText(text));
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or ArgumentException)
        {
            Record(CurrentComponent, "invalid", $"{property.DisplayName}: ‘{text}’ is not a valid value");
        }
    }

    public void Click(string source)
    {
        var component = CurrentComponent;
        component.ClickCount++;
        if (Current.Id == "button") PreviewChanged?.Invoke();
        Record(component, "click", $"{source} activated · total {component.ClickCount}");
    }

    public void Record(string kind, string message) => Record(CurrentComponent, kind, message);

    public void ClearEvents()
    {
        CurrentComponent.Events.Clear();
        Changed?.Invoke();
    }

    public void Reset()
    {
        var component = CurrentComponent;
        ResetComponent(component);
        component.ClickCount = 0;
        component.SceneId = "main";
        PreviewChanged?.Invoke();
        SceneChanged?.Invoke();
        Record(component, "reset", "Component properties restored to their defaults");
    }

    private ComponentState GetComponent(DemoDescriptor descriptor)
    {
        if (_components.TryGetValue(descriptor.Id, out var state)) return state;
        state = new ComponentState();
        state.SceneId = "main";
        ResetComponent(state, descriptor);
        state.Events.Add(CreateEntry("ready", $"{descriptor.Name} playground initialized"));
        _components.Add(descriptor.Id, state);
        return state;
    }

    private void ResetComponent(ComponentState state) => ResetComponent(state, Current);

    private void ResetComponent(ComponentState state, DemoDescriptor descriptor)
    {
        state.Values.Clear();
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(descriptor.ComponentType);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[docs-demo] Could not inspect {descriptor.Name} defaults: {exception.Message}");
        }

        foreach (var property in descriptor.Properties)
        {
            object? value = null;
            if (instance is not null)
            {
                try { value = property.Property.GetValue(instance); }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"[docs-demo] Could not read {descriptor.Name}.{property.Name}: {exception.Message}");
                }
            }
            state.Values[property.Name] = value;
        }

        if (descriptor.PresetParameters is not null)
        {
            foreach (var (name, value) in descriptor.PresetParameters) state.Values[name] = value;
        }

        state.DefaultValues.Clear();
        foreach (var (name, value) in state.Values) state.DefaultValues[name] = value;

        foreach (var eventProperty in descriptor.Events)
        {
            state.Values[eventProperty.Name] = CreateCallback(descriptor, state, eventProperty);
        }
    }

    private object CreateCallback(DemoDescriptor descriptor, ComponentState state, PropertyInfo eventProperty)
    {
        if (eventProperty.PropertyType == typeof(EventCallback))
        {
            return EventCallback.Factory.Create(this, (Action)(() => OnEvent(descriptor, state, eventProperty.Name, null)));
        }

        var argumentType = eventProperty.PropertyType.GetGenericArguments()[0];
        return typeof(DemoState)
            .GetMethod(nameof(CreateGenericCallback), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(argumentType)
            .Invoke(this, [descriptor, state, eventProperty.Name])!;
    }

    private EventCallback<T> CreateGenericCallback<T>(DemoDescriptor descriptor, ComponentState state, string eventName)
        => EventCallback.Factory.Create(this, (Action<T>)(value => OnEvent(descriptor, state, eventName, value)));

    private void OnEvent(DemoDescriptor descriptor, ComponentState state, string eventName, object? value)
    {
        var previewChanged = descriptor.Id == "button" && eventName == "OnClick";
        if (previewChanged) state.ClickCount++;
        if (descriptor.Id == "menu" && eventName == "OnClose")
        {
            state.Values["IsOpen"] = false;
            previewChanged = true;
        }
        if (eventName.EndsWith("Changed", StringComparison.Ordinal) && eventName.Length > "Changed".Length)
        {
            var propertyName = eventName[..^"Changed".Length];
            var property = descriptor.FindProperty(propertyName);
            if (property is not null && IsCompatible(property, value))
            {
                state.Values[propertyName] = value;
                previewChanged = true;
            }
        }

        if (previewChanged) PreviewChanged?.Invoke();
        Record(state, "event", $"{eventName}{FormatEventValue(value)}");
    }

    private static bool IsCompatible(DemoProperty property, object? value)
    {
        if (value is null) return property.IsNullable;
        return property.Property.PropertyType.IsInstanceOfType(value);
    }

    private static string FormatEventValue(object? value) => value switch
    {
        null => string.Empty,
        string text => $" = {text}",
        bool or int or long or float or double or decimal => $" = {DemoProperty.FormatValue(value)}",
        _ => string.Empty,
    };

    private void Record(ComponentState state, string kind, string message)
    {
        state.Events.Insert(0, CreateEntry(kind, message));
        if (state.Events.Count > MaxEvents) state.Events.RemoveAt(state.Events.Count - 1);
        Changed?.Invoke();
    }

    private DemoLogEntry CreateEntry(string kind, string message)
        => new(++_nextEventId, DateTime.Now.ToString("HH:mm:ss"), kind, message);

    private static string SplitName(string value)
    {
        var chars = new List<char>(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1])) chars.Add(' ');
            chars.Add(value[i]);
        }
        return new string(chars.ToArray());
    }

    public sealed class ComponentState
    {
        public Dictionary<string, object?> Values { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, object?> DefaultValues { get; } = new(StringComparer.Ordinal);
        public List<DemoLogEntry> Events { get; } = [];
        public string SceneId { get; set; } = "main";
        public int ClickCount { get; set; }
    }
}
