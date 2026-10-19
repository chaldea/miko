using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Hosting;
using Miko.Ionic;
using Shouldly;
using TestContext = Miko.Testing.TestContext;

namespace Miko.Docs.Tests;

/// <summary>
/// Every example shown in the component docs is a compiled component in this assembly (see the
/// csproj). Compiling proves it matches the API; rendering it here, with the services
/// <c>AddIonic()</c> registers, proves it also builds a tree and lays out without throwing.
/// </summary>
public class ExampleRenderTests
{
    private const string ExampleNamespace = "Miko.Docs.Tests.Examples";

    public static TheoryData<string> Examples()
    {
        var data = new TheoryData<string>();
        foreach (var type in ExampleTypes())
            data.Add(type.FullName![(ExampleNamespace.Length + 1)..]);
        return data;
    }

    private static IEnumerable<Type> ExampleTypes() =>
        typeof(ExampleRenderTests).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(ExampleNamespace, StringComparison.Ordinal) == true
                           && typeof(ComponentBase).IsAssignableFrom(type)
                           && !type.IsAbstract
                           && !type.IsNested)
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

    [Fact]
    public void Should_compile_every_example_file()
    {
        var files = Directory.EnumerateFiles(Path.Combine(RepoPaths.Root, "docs", "components"), "*.razor", SearchOption.AllDirectories)
            .Where(file => !Path.GetFileName(file).StartsWith('_'))
            .Count();
        files.ShouldBeGreaterThan(0);
        ExampleTypes().Count().ShouldBe(files);
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void Should_render_example(string example)
    {
        var type = typeof(ExampleRenderTests).Assembly.GetType($"{ExampleNamespace}.{example}", throwOnError: true)!;
        using var context = CreateContext();

        var render = typeof(TestContext).GetMethod(nameof(TestContext.Render))!.MakeGenericMethod(type);
        var cut = (Miko.Testing.ComponentUnderTest)render.Invoke(context, [null])!;

        cut.Root.ShouldNotBeNull();
    }

    private static TestContext CreateContext()
    {
        // Exactly the services a real app has: MikoAppBuilder.CreateDefault() plus AddIonic().
        var app = MikoAppBuilder.CreateDefault();
        app.AddIonic();

        var context = new TestContext();
        foreach (var descriptor in app.Services)
            context.Services.Add(descriptor);
        foreach (var registry in app.Services.Select(d => d.ImplementationInstance).OfType<IonicStyleRegistry>())
            context.AddStyleSheet(registry.StyleSheet);
        return context;
    }
}
