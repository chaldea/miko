using Microsoft.Extensions.DependencyInjection;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-068: every Ionic component stamps the active mode class (<c>md</c> / <c>ios</c>) onto
/// its root element, derived from the injected host <see cref="IPlatformInfo"/>. Switching the
/// platform switches the rendered class so the mode-scoped stylesheet rules apply.
/// </summary>
public class IonicModeTests
{
    private static TestContext ContextFor(HostPlatform platform)
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        return ctx;
    }

    [Fact]
    public void Component_OnAndroid_GetsMdModeClass()
    {
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonHeader>();
        cut.Root.Class.ShouldBe("md ion-header");
    }

    [Fact]
    public void Component_OniOS_GetsIosModeClass()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonHeader>();
        cut.Root.Class.ShouldBe("ios ion-header");
    }

    [Theory]
    [InlineData(HostPlatform.Windows)]
    [InlineData(HostPlatform.Linux)]
    [InlineData(HostPlatform.MacOS)]
    [InlineData(HostPlatform.Android)]
    public void NonApplePlatforms_ResolveToMd(HostPlatform platform)
    {
        using var ctx = ContextFor(platform);
        var cut = ctx.Render<IonHeader>();
        cut.Root.Class.ShouldStartWith("md ");
    }

    [Fact]
    public void NoPlatformService_FallsBackToMd()
    {
        // A bare context with no IPlatformInfo registered: mode defaults to Material Design.
        using var ctx = new TestContext();
        var cut = ctx.Render<IonHeader>();
        cut.Root.Class.ShouldBe("md ion-header");
    }

    [Fact]
    public void ModeClass_PrecedesStateClasses()
    {
        // The mode class is prepended, with state/variant classes following.
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonSegment>(p => p.Add(nameof(IonSegment.Disabled), true));
        cut.Root.Class.ShouldBe("ios ion-segment segment-disabled");
    }

    [Fact]
    public void TabButton_iOS_KeepsSelectedStateAfterMode()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonTabButton>(p => p.Add(nameof(IonTabButton.Selected), true));
        cut.Root.Class.ShouldBe("ios ion-tab-button tab-selected");
    }

    [Fact]
    public void IonItem_iOS_LinesNone()
    {
        using var ctx = ContextFor(HostPlatform.Ios);
        var cut = ctx.Render<IonItem>(p => p.Add(nameof(IonItem.Lines), "none"));
        // Ionic stamps `item-lines-{lines}` on the host (item.tsx); with an explicit lines value
        // the `item-lines-default` marker is not applied.
        cut.Root.Class.ShouldBe("ios ion-item item-lines-none");
    }

    [Fact]
    public void IonModeResolver_MapsPlatformsToModes()
    {
        IonicModeResolver.ForPlatform(HostPlatform.Ios).ShouldBe(IonicMode.Ios);
        IonicModeResolver.ForPlatform(HostPlatform.Android).ShouldBe(IonicMode.Md);
        IonicModeResolver.ForPlatform(HostPlatform.Windows).ShouldBe(IonicMode.Md);
        IonicModeResolver.ResolveClass(null).ShouldBe("md");
        IonicModeResolver.ResolveClass(new PlatformInfo(HostPlatform.Ios)).ShouldBe("ios");
    }
}
