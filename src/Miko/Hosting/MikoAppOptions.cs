using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Miko.Core;
using Miko.Routing;
using Miko.Styling;

namespace Miko.Hosting;

public class MikoAppOptions
{
    public string Title { get; set; } = "Miko Application";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public Func<Element>? RootComponentFactory { get; set; }
    public List<StyleSheet> StyleSheets { get; set; } = new();
    public Assembly[]? RouteAssemblies { get; set; }
    public Action<Router>? RouteConfigurator { get; set; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public Type? DefaultLayout { get; set; }

    public List<FontRegistration> Fonts { get; set; } = new();
}

public class FontRegistration
{
    public required string FamilyName { get; init; }
    public required Assembly Assembly { get; init; }
    public required string ResourceName { get; init; }
}
