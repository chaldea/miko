using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using Miko.Components;
using Miko.Core;

namespace Miko.DocGen;

public sealed record ElementAttribute(string Name, string Property);

public sealed record ElementTag(string Tag, Type ElementType, IReadOnlyList<ElementAttribute> Attributes);

public sealed record BindMapping(string Element, string? InputType, string? Suffix, string ValueAttribute, string ChangeAttribute);

/// <summary>
/// What markup can actually say. The Razor compiler resolves tags and attributes against a
/// fixed table (<c>MikoElements</c> in Miko.Razor.Compiler, mirrored by
/// <c>RenderTreeBuilder</c> at run time); any other attribute is dropped without a warning. That
/// table — not the element classes' properties — is what the elements reference must describe,
/// so it is read straight out of the built compiler.
/// </summary>
public sealed class ElementCatalog
{
    private const string MikoElementsType = "Microsoft.AspNetCore.Razor.Language.Components.MikoElements";

    private ElementCatalog(IReadOnlyList<ElementTag> tags, IReadOnlyList<ElementAttribute> common, IReadOnlyList<BindMapping> binds)
    {
        Tags = tags;
        CommonAttributes = common;
        Binds = binds;
    }

    public IReadOnlyList<ElementTag> Tags { get; }

    /// <summary>Attributes every element accepts (<c>class</c>, <c>id</c>, <c>style</c>, events).</summary>
    public IReadOnlyList<ElementAttribute> CommonAttributes { get; }

    public IReadOnlyList<BindMapping> Binds { get; }

    /// <summary>Concrete element classes no tag maps to — usable only when building the DOM in C#.</summary>
    public IEnumerable<Type> CodeOnlyElements =>
        typeof(Element).Assembly.GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && type.IsSubclassOf(typeof(Element))
                           && type.Namespace == "Miko.Core.DomElements"
                           && Tags.All(tag => tag.ElementType != type))
            .Where(type => TagNameOf(type) is { } tag && char.IsLetter(tag[0]) && tag != "fragment")
            .OrderBy(type => type.Name, StringComparer.Ordinal);

    public static string? TagNameOf(Type elementType)
    {
        try
        {
            return elementType.GetConstructor(Type.EmptyTypes) is null ? null : ((Element)Activator.CreateInstance(elementType)!).TagName;
        }
        catch
        {
            return null;
        }
    }

    public static ElementCatalog Load(string compilerPath)
    {
        var context = new AssemblyLoadContext("miko-docgen-razor", isCollectible: true);
        try
        {
            var assembly = context.LoadFromAssemblyPath(compilerPath);
            var table = assembly.GetType(MikoElementsType, throwOnError: true)!;

            object Field(string name) =>
                table.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                ?? throw new InvalidOperationException($"{MikoElementsType}.{name} not found — update ElementCatalog to the compiler's current table.");

            static string PropertyOf(object slot) =>
                (string)slot.GetType().GetProperty("PropertyName")!.GetValue(slot)!;

            static IEnumerable<ElementAttribute> Slots(object dictionary) =>
                ((IEnumerable)dictionary).Cast<object>().Select(entry =>
                {
                    var key = (string)entry.GetType().GetProperty("Key")!.GetValue(entry)!;
                    var slot = entry.GetType().GetProperty("Value")!.GetValue(entry)!;
                    return new ElementAttribute(key, PropertyOf(slot));
                });

            var tagTypes = ((IEnumerable)Field("s_tagTypes")).Cast<object>().ToDictionary(
                entry => (string)entry.GetType().GetProperty("Key")!.GetValue(entry)!,
                entry => (string)entry.GetType().GetProperty("Value")!.GetValue(entry)!);
            var tagSlots = ((IEnumerable)Field("s_tagSlots")).Cast<object>().ToDictionary(
                entry => (string)entry.GetType().GetProperty("Key")!.GetValue(entry)!,
                entry => Slots(entry.GetType().GetProperty("Value")!.GetValue(entry)!).ToList(),
                StringComparer.OrdinalIgnoreCase);
            // `style` and `Style` are the same slot; list it once.
            var common = Slots(Field("s_commonSlots")).Where(a => a.Name != "Style").ToList();

            var tags = tagTypes.Select(pair =>
            {
                var typeName = pair.Value.Replace("global::", "");
                var type = typeof(Element).Assembly.GetType(typeName, throwOnError: true)!;
                tagSlots.TryGetValue(pair.Key, out var attributes);
                return new ElementTag(pair.Key, type, attributes ?? []);
            }).ToList();

            return new ElementCatalog(tags, common, BindMappings());
        }
        finally
        {
            context.Unload();
        }
    }

    private static IReadOnlyList<BindMapping> BindMappings()
    {
        var result = new List<BindMapping>();
        foreach (var attribute in typeof(BindAttributes).GetCustomAttributes(inherit: false))
        {
            switch (attribute)
            {
                case BindInputElementAttribute input:
                    result.Add(new BindMapping("input", input.Type, input.Suffix, input.ValueAttribute, input.ChangeAttribute));
                    break;
                case BindElementAttribute element:
                    result.Add(new BindMapping(element.Element, null, element.Suffix, element.ValueAttribute, element.ChangeAttribute));
                    break;
            }
        }
        return result;
    }
}
