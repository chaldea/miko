using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Miko.Components;

namespace Miko.Docs.Demo;

public enum DemoCategory
{
    Ionic,
    Charts,
    Components,
}

public sealed record DemoScene(string Id, string Name, string Description,
    IReadOnlyDictionary<string, object?>? PresetParameters = null);

/// <summary>Documentation entry and the metadata needed to host one live component.</summary>
public sealed class DemoDescriptor
{
    public const DynamicallyAccessedMemberTypes ComponentActivation =
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicPropertiesWithInherited;

    private IReadOnlyList<DemoProperty>? _properties;
    private IReadOnlyList<PropertyInfo>? _events;

    public DemoDescriptor(
        string id,
        string name,
        DemoCategory category,
        [DynamicallyAccessedMembers(DemoDescriptor.ComponentActivation)] Type componentType,
        string description,
        string documentationPath,
        string? title = null,
        IEnumerable<DemoScene>? scenes = null,
        IReadOnlyDictionary<string, object?>? presetParameters = null,
        IEnumerable<string>? hiddenProperties = null,
        bool showMode = true)
    {
        Id = id;
        Name = name;
        Title = title ?? name;
        Category = category;
        ComponentType = componentType;
        Description = description;
        DocumentationPath = documentationPath;
        Scenes = scenes?.ToArray() ?? [];
        PresetParameters = presetParameters;
        HiddenProperties = new HashSet<string>(hiddenProperties ?? [], StringComparer.Ordinal);
        ShowMode = showMode && category == DemoCategory.Ionic;
    }

    public string Id { get; }
    public string Name { get; }
    public string Title { get; }
    public DemoCategory Category { get; }

    [DynamicallyAccessedMembers(DemoDescriptor.ComponentActivation)]
    public Type ComponentType { get; }

    public string Description { get; }
    public string DocumentationPath { get; }
    public IReadOnlyList<DemoScene> Scenes { get; }
    public IReadOnlyDictionary<string, object?>? PresetParameters { get; }
    public HashSet<string> HiddenProperties { get; }
    public bool ShowMode { get; }

    public IReadOnlyList<DemoProperty> Properties => _properties ??= ComponentType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(DemoProperty.Create)
        .Where(property => property is not null && !HiddenProperties.Contains(property.Name) &&
                           property.Name is not "Mode" and not "Class" and not "Id")
        .Cast<DemoProperty>()
        .OrderBy(property => property.Name, StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyList<PropertyInfo> Events => _events ??= ComponentType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.CanWrite && property.SetMethod?.IsPublic == true &&
                           property.GetCustomAttribute<ParameterAttribute>(inherit: true) is not null &&
                           DemoProperty.IsEventCallback(property.PropertyType) &&
                           property.Name is not "UpdateFinished" and not "HoveredPointsChanged" &&
                           !HiddenProperties.Contains(property.Name))
        .OrderBy(property => property.Name, StringComparer.Ordinal)
        .ToArray();

    public DemoProperty? FindProperty(string name) =>
        Properties.FirstOrDefault(property => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

public static class DemoDescriptorFactory
{
    public static DemoDescriptor For<[DynamicallyAccessedMembers(DemoDescriptor.ComponentActivation)] T>(
        string id,
        DemoCategory category,
        string description,
        string documentationPath,
        string? title = null,
        IEnumerable<DemoScene>? scenes = null,
        IReadOnlyDictionary<string, object?>? presetParameters = null,
        IEnumerable<string>? hiddenProperties = null,
        bool showMode = true) where T : ComponentBase, new()
        => new(id, typeof(T).Name, category, typeof(T), description, documentationPath, title,
            scenes, presetParameters, hiddenProperties, showMode);
}
