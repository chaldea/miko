using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Platform.Resources;
using Miko.Styling;
using Miko.Testing;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonIconTests : IonicComponentTestBase
{
    // Embedded in this test assembly (Resources/*.svg → Miko.Ionic.Tests.Resources.*).
    private const string TestIconRes = "res://Miko.Ionic.Tests.Resources.test-icon.svg";
    private const string TestIcon2Res = "res://Miko.Ionic.Tests.Resources.test-icon-2.svg";
    private const string MissingIconRes = "res://Miko.Ionic.Tests.Resources.missing-icon.svg";

    /// <summary>
    /// Registers a resource provider that searches this test assembly — the test stand-in for
    /// the app's <c>builder.AddResourceAssembly(typeof(App).Assembly)</c>.
    /// </summary>
    private void UseTestResourceAssembly() =>
        Context.Services.AddSingleton<IResourceAssemblyProvider>(
            new StubResourceAssemblyProvider(typeof(IonIconTests).Assembly));

    [Fact]
    public void IonIcon_RendersWithCorrectClass()
    {
        // Act
        var cut = Context.Render<IonIcon>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-icon");
    }

    [Fact]
    public void IonIcon_HasNoStyle_WhenIconIsNull()
    {
        // Act
        var cut = Context.Render<IonIcon>();

        // Assert
        cut.Root.Style.ShouldBeNull();
    }

    [Fact]
    public void IonIcon_AppliesBackgroundStyle_WhenIconIsProvided()
    {
        // Act
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        // Assert - Key style attribute
        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
        cut.Root.Style.BackgroundSize.ShouldBe(BackgroundSize.Contain);
        cut.Root.Style.BackgroundPosition.ShouldBe(BackgroundPosition.Center);
        cut.Root.Style.BackgroundRepeat.ShouldBe(BackgroundRepeat.NoRepeat);
    }

    [Fact]
    public void IonIcon_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-icon");
        cut.Root.Children.Count.ShouldBe(0); // Icon is rendered via background-image, not children
    }

    [Fact]
    public void IonIcon_PreservesColor_WhenMergingIconStyle()
    {
        // Color drives the icon tint (CSS fill: currentColor) and must survive the
        // background-image merge — the icon style only sets background properties.
        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Style), new Style { Color = Color.FromRgb(255, 255, 255) }));

        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
        cut.Root.Style.Color.ShouldBe(Color.FromRgb(255, 255, 255));
    }

    [Fact]
    public void IonIcon_MarksBackgroundImageAsTemplate()
    {
        // Ionicons glyphs are monochrome masks — the resolved image must be tintable.
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        cut.Root.Style!.BackgroundImage!.Value.Value.IsTemplate.ShouldBeTrue();
    }

    [Fact]
    public void IonIcon_UpdatesStyle_WhenIconParameterChanges()
    {
        // Act - First render with no icon
        var cut1 = Context.Render<IonIcon>();

        // Assert
        cut1.Root.Style.ShouldBeNull();

        // Act - Second render with icon
        var cut2 = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "triangle"));

        // Assert - State change assertion
        cut2.Root.Style.ShouldNotBeNull();
        cut2.Root.Style!.BackgroundImage.ShouldNotBeNull();
    }

    // ---- color / size / slot classes ----------------------------------------

    [Fact]
    public void IonIcon_StampsColorClass()
    {
        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Color), "primary"));

        cut.Root.Class.ShouldBe("md ion-icon ion-color-primary");
    }

    [Theory]
    [InlineData("small", "icon-small")]
    [InlineData("large", "icon-large")]
    public void IonIcon_StampsSizeClass(string size, string expected)
    {
        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Size), size));

        cut.Root.Class.ShouldBe($"md ion-icon {expected}");
    }

    [Fact]
    public void IonIcon_StampsSlotClass()
    {
        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Slot), "start"));

        cut.Root.ShouldHaveClass("ion-slot-start");
    }

    // ---- mode-specific names (ios / md props) --------------------------------

    [Fact]
    public void IonIcon_UsesMdName_InMdMode()
    {
        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Md), "triangle")
            .Add(nameof(IonIcon.Ios), "does-not-exist"));

        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
    }

    [Fact]
    public void IonIcon_UsesIosName_InIosMode()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Ios), "triangle")
            .Add(nameof(IonIcon.Md), "does-not-exist"));

        cut.Root.Class.ShouldStartWith("ios ion-icon");
        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
    }

    [Fact]
    public void IonIcon_IgnoresMdName_InIosMode()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        // Only Md is set: in ios mode name resolution finds nothing → empty box.
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Md), "triangle"));

        cut.Root.Style.ShouldBeNull();
    }

    [Fact]
    public void IonIcon_ModeSpecificName_BeatsIconSrc()
    {
        // getUrl ordering: name resolution (Ios) succeeds, so the src-looking Icon is not used.
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Ios), "triangle")
            .Add(nameof(IonIcon.Icon), TestIconRes));

        var image = cut.Root.Style?.BackgroundImage?.Value;
        image.ShouldNotBeNull();
        image!.IsTemplate.ShouldBeTrue(); // built-in glyph, not the external src
    }

    // ---- external sources (src prop / src-looking icon) -----------------------

    [Fact]
    public void IonIcon_Src_LoadsThroughRegisteredResourceAssembly()
    {
        // The issue: res:// sources must resolve via the assemblies registered at startup
        // (builder.AddResourceAssembly), not the Miko.Ionic assembly.
        UseTestResourceAssembly();

        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Src), TestIconRes));

        var image = cut.Root.Style?.BackgroundImage?.Value;
        image.ShouldNotBeNull();
        // External images render as-is — no monochrome tinting.
        image!.IsTemplate.ShouldBeFalse();
        image.OriginalWidth.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void IonIcon_Icon_WithSrcValue_LoadsThroughRegisteredResourceAssembly()
    {
        // Ionic's icon prop doubles as name-or-src: a value containing '/' or '.' is a src.
        UseTestResourceAssembly();

        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), TestIconRes));

        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
    }

    [Fact]
    public void IonIcon_Src_TakesPrecedenceOverIcon()
    {
        UseTestResourceAssembly();

        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Src), TestIconRes)
            .Add(nameof(IonIcon.Icon), "!!!not-a-name!!!"));

        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.BackgroundImage.ShouldNotBeNull();
    }

    [Fact]
    public void IonIcon_Src_WithoutResourceProvider_RendersEmpty()
    {
        // No IResourceAssemblyProvider registered (bare service scope) — the res:// source
        // cannot resolve and the icon renders as an empty box, like an unknown name.
        // (test-icon-2 keeps this case out of the resolver's static cache shared with the
        // provider-backed tests.)
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Src), TestIcon2Res));

        cut.Root.Style.ShouldBeNull();
    }

    [Fact]
    public void IonIcon_Src_MissingResource_RendersEmpty()
    {
        UseTestResourceAssembly();

        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Src), MissingIconRes));

        cut.Root.Style.ShouldBeNull();
    }

    // ---- name validation -------------------------------------------------------

    [Fact]
    public void IonIcon_InvalidName_RendersEmpty()
    {
        // getName rejects anything outside [a-z0-9-] — no lookup, no style.
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "foo bar"));

        cut.Root.Style.ShouldBeNull();
    }

    [Fact]
    public void IonIcon_UnknownName_RendersEmpty()
    {
        var cut = Context.Render<IonIcon>(parameters =>
            parameters.Add(nameof(IonIcon.Icon), "does-not-exist"));

        cut.Root.Style.ShouldBeNull();
    }

    private sealed class StubResourceAssemblyProvider : IResourceAssemblyProvider
    {
        private readonly Assembly[] _assemblies;

        public StubResourceAssemblyProvider(params Assembly[] assemblies) => _assemblies = assemblies;

        public IEnumerable<Assembly> GetResourceAssemblies() => _assemblies;
    }
}
