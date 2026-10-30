using System.Reflection;
using Miko.Core;
using Shouldly;

namespace Miko.Tests.Graphics;

/// <summary>
/// ISSUE-162: the rendering backend (SkiaSharp, HarfBuzzSharp, Svg.Skia) is sealed inside the core
/// library so it can be replaced. No public or protected member may expose one of its types; outside
/// assemblies draw through <c>Miko.Graphics</c>. The package references are compile-private as well, so a
/// leak in another assembly does not compile — this test guards the core's own surface.
/// </summary>
public class BackendSealTests
{
    private static readonly string[] BackendNamespaces = ["SkiaSharp", "HarfBuzzSharp", "Svg.Skia", "ShimSkiaSharp"];

    [Fact]
    public void Public_surface_exposes_no_rendering_backend_types()
    {
        var leaks = new List<string>();
        foreach (var type in typeof(MikoEngine).Assembly.GetExportedTypes())
        {
            if (IsBackend(type.BaseType)) leaks.Add($"{type.FullName} : {type.BaseType}");
            leaks.AddRange(type.GetInterfaces().Where(IsBackend).Select(i => $"{type.FullName} : {i}"));

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                       BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var member in type.GetMembers(flags))
            {
                var exposed = member switch
                {
                    FieldInfo f when f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly => [f.FieldType],
                    PropertyInfo p when IsVisible(p.GetMethod) || IsVisible(p.SetMethod) =>
                        [p.PropertyType, .. p.GetIndexParameters().Select(x => x.ParameterType)],
                    EventInfo e when IsVisible(e.AddMethod) => [e.EventHandlerType!],
                    MethodBase m when IsVisible(m) =>
                        [.. m.GetParameters().Select(x => x.ParameterType), .. m is MethodInfo mi ? [mi.ReturnType] : Type.EmptyTypes],
                    _ => Type.EmptyTypes,
                };
                if (exposed.Any(IsBackend)) leaks.Add($"{type.FullName}.{member.Name}");
            }
        }

        leaks.ShouldBeEmpty();
    }

    private static bool IsVisible(MethodBase? method)
        => method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

    private static bool IsBackend(Type? type)
    {
        if (type == null) return false;
        if (type.HasElementType) return IsBackend(type.GetElementType());
        if (type.IsGenericType && type.GetGenericArguments().Any(IsBackend)) return true;
        return type.Namespace is { } ns && BackendNamespaces.Any(b => ns == b || ns.StartsWith(b + ".", StringComparison.Ordinal));
    }
}
