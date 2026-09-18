using System.Reflection;
using Miko.Platform.Resources;
using Shouldly;

namespace Miko.Tests.Platform;

/// <summary>
/// <c>res://</c> 逻辑路径 → 清单资源名的换算（ISSUE-139）。
///
/// <para>用户书写的路径不含程序集名、用 <c>/</c> 分隔（<c>res://TestAssets/Resources/logo.svg</c>），
/// 而 MSBuild 生成的清单名是 <c>Miko.Tests.TestAssets.Resources.logo.svg</c>。换算只在
/// <see cref="EmbeddedResources"/> 内发生，不外泄到用户端——把程序集名写进路径会让「换程序集名」
/// 变成全仓改路径。</para>
///
/// <para>素材由 csproj 的 <c>EmbeddedResource Include="TestAssets\Resources\**\*"</c> 嵌入。</para>
/// </summary>
public class EmbeddedResourcesTests
{
    private static readonly Assembly TestAssembly = typeof(EmbeddedResourcesTests).Assembly;

    private const string LogoManifest = "Miko.Tests.TestAssets.Resources.logo.svg";
    private const string BadgeManifest = "Miko.Tests.TestAssets.Resources.Nested.badge.svg";

    private sealed class StubProvider : IResourceAssemblyProvider
    {
        private readonly Assembly[] _assemblies;
        public StubProvider(params Assembly[] assemblies) => _assemblies = assemblies;
        public IEnumerable<Assembly> GetResourceAssemblies() => _assemblies;
    }

    // ---- NormalizePath ------------------------------------------------------

    [Theory]
    [InlineData("Assets/logo.svg", "Assets.logo.svg")]
    [InlineData("Assets\\logo.svg", "Assets.logo.svg")]
    [InlineData("Assets/Icons/logo.svg", "Assets.Icons.logo.svg")]
    [InlineData("logo.svg", "logo.svg")]
    // 已是点分形式（旧写法）原样通过，这正是新旧写法共存的基础。
    [InlineData("MyApp.Assets.logo.svg", "MyApp.Assets.logo.svg")]
    // 前导 "./" 与 "/" 不影响语义，与 file:// 的书写习惯对齐。
    [InlineData("./Assets/logo.svg", "Assets.logo.svg")]
    [InlineData("/Assets/logo.svg", "Assets.logo.svg")]
    public void NormalizePath_ConvertsSeparatorsToDots(string input, string expected)
    {
        EmbeddedResources.NormalizePath(input).ShouldBe(expected);
    }

    [Fact]
    public void NormalizePath_EmptyInput_ReturnsEmpty()
    {
        EmbeddedResources.NormalizePath("").ShouldBe("");
    }

    // ---- ResolveName --------------------------------------------------------

    [Fact]
    public void ResolveName_AssemblyAgnosticPath_FindsManifestResource()
    {
        // 这是 issue 要求的新写法：路径里没有 "Miko.Tests."。
        EmbeddedResources.ResolveName(TestAssembly, "TestAssets/Resources/logo.svg")
            .ShouldBe(LogoManifest);
    }

    [Fact]
    public void ResolveName_NestedPath_FindsManifestResource()
    {
        EmbeddedResources.ResolveName(TestAssembly, "TestAssets/Resources/Nested/badge.svg")
            .ShouldBe(BadgeManifest);
    }

    [Fact]
    public void ResolveName_FullManifestName_StillResolves()
    {
        // 旧写法（含程序集名）必须继续可用：它对自己的程序集恰好是精确匹配。
        EmbeddedResources.ResolveName(TestAssembly, LogoManifest).ShouldBe(LogoManifest);
    }

    [Fact]
    public void ResolveName_DottedOldStylePath_StillResolves()
    {
        // 旧写法的另一种书写：点分但从中间某段开始。
        EmbeddedResources.ResolveName(TestAssembly, "TestAssets.Resources.logo.svg")
            .ShouldBe(LogoManifest);
    }

    [Fact]
    public void ResolveName_BareFileName_ResolvesOnDotBoundary()
    {
        // 后缀匹配以点为边界，"logo.svg" 命中 "….Resources.logo.svg"。
        EmbeddedResources.ResolveName(TestAssembly, "logo.svg").ShouldBe(LogoManifest);
    }

    [Fact]
    public void ResolveName_PartialSegmentSuffix_DoesNotMatch()
    {
        // 点边界是必须的：裸后缀比较会让 "ogo.svg" 命中 "….logo.svg"。
        EmbeddedResources.ResolveName(TestAssembly, "ogo.svg").ShouldBeNull();
    }

    [Fact]
    public void ResolveName_MissingResource_ReturnsNull()
    {
        EmbeddedResources.ResolveName(TestAssembly, "TestAssets/Resources/nope.svg").ShouldBeNull();
    }

    // ---- Locate -------------------------------------------------------------

    [Fact]
    public void Locate_SearchesRegisteredAssembliesInOrder()
    {
        // 第一个程序集（Shouldly）不含该资源，应继续到下一个。
        var provider = new StubProvider(typeof(ShouldBeTestExtensions).Assembly, TestAssembly);

        var located = EmbeddedResources.Locate(provider, "TestAssets/Resources/logo.svg");

        located.ShouldNotBeNull();
        located!.Value.Assembly.ShouldBe(TestAssembly);
        located.Value.ManifestName.ShouldBe(LogoManifest);
    }

    [Fact]
    public void Locate_NullProvider_ReturnsNull()
    {
        EmbeddedResources.Locate(null, "TestAssets/Resources/logo.svg").ShouldBeNull();
    }

    [Fact]
    public void Locate_MissingResource_ReturnsNull()
    {
        var provider = new StubProvider(TestAssembly);
        EmbeddedResources.Locate(provider, "nope.svg").ShouldBeNull();
    }

    // ---- Enumerate（DevTools 资源面板的数据源）-------------------------------

    [Fact]
    public void Enumerate_ReportsLogicalPathAssemblyAndSize()
    {
        var provider = new StubProvider(TestAssembly);

        var all = EmbeddedResources.Enumerate(provider);
        var logo = all.FirstOrDefault(r => r.ManifestName == LogoManifest);

        logo.ShouldNotBeNull();
        // 面板展示的是可书写的路径，不是清单名。
        logo!.LogicalPath.ShouldBe("TestAssets/Resources/logo.svg");
        logo.AssemblyName.ShouldBe("Miko.Tests");
        logo.ByteCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Enumerate_LogicalPathRoundTripsThroughResolveName()
    {
        // 面板给出的路径必须真的能用——否则复制出来的 res:// 是死链。
        var provider = new StubProvider(TestAssembly);

        foreach (var resource in EmbeddedResources.Enumerate(provider))
        {
            EmbeddedResources.ResolveName(TestAssembly, resource.LogicalPath)
                .ShouldBe(resource.ManifestName, $"logical path '{resource.LogicalPath}' did not resolve back");
        }
    }

    [Fact]
    public void Enumerate_DeduplicatesRepeatedAssemblies()
    {
        // 入口程序集会被默认提供器加入，应用再显式 AddResourceAssembly 同一个程序集时
        // 清单不能列两遍。
        var once = EmbeddedResources.Enumerate(new StubProvider(TestAssembly));
        var twice = EmbeddedResources.Enumerate(new StubProvider(TestAssembly, TestAssembly));

        twice.Count.ShouldBe(once.Count);
    }

    [Fact]
    public void Enumerate_NullProvider_ReturnsEmpty()
    {
        EmbeddedResources.Enumerate(null).ShouldBeEmpty();
    }

    // ---- ToLogicalPath ------------------------------------------------------

    [Theory]
    [InlineData("MyApp.Assets.logo.svg", "MyApp", "Assets/logo.svg")]
    [InlineData("MyApp.Assets.Icons.logo.svg", "MyApp", "Assets/Icons/logo.svg")]
    [InlineData("MyApp.logo.svg", "MyApp", "logo.svg")]
    // 根命名空间与程序集名不一致时剥不掉前缀，保留整名——它对该程序集仍是精确匹配。
    [InlineData("Other.Assets.logo.svg", "MyApp", "Other/Assets/logo.svg")]
    public void ToLogicalPath_StripsAssemblyPrefixAndRestoresSeparators(
        string manifestName, string assemblyName, string expected)
    {
        EmbeddedResources.ToLogicalPath(manifestName, assemblyName).ShouldBe(expected);
    }
}
