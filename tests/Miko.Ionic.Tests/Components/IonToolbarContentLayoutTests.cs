using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for ion-toolbar.md §3: the <c>.toolbar-content</c> div must have <c>display: block</c>
/// so that children (title, progress bar, etc.) stack vertically instead of horizontally.
/// Without it, children inherit the parent <c>.toolbar-container</c>'s <c>display: flex</c> and
/// lay out side-by-side.
/// </summary>
public class IonToolbarContentLayoutTests : IonicComponentTestBase
{
    [Fact]
    public void ToolbarContent_StacksChildrenVertically()
    {
        // Repro for ion-toolbar.md §3: a title + progress bar inside the toolbar should stack
        // vertically (title on top, bar below), but without toolbar-content having display:block
        // or flex-direction:column, they render side-by-side.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;
        Context.ViewportHeight = 300f;

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(b =>
        {
            b.OpenComponent<IonTitle>(0);
            b.AddComponentParameter(1, nameof(IonTitle.ChildContent), (RenderFragment)(t => t.AddContent(0, "Page title")));
            b.CloseComponent();
            b.OpenComponent<IonProgressBar>(2);
            b.AddComponentParameter(3, nameof(IonProgressBar.Value), 0.4);
            b.CloseComponent();
        }));

        var title = cut.Root.FindByClass("ion-title").First();
        var bar = cut.Root.FindByClass("ion-progress-bar").First();

        var titleBox = cut.GetBoxModel(title)!;
        var barBox = cut.GetBoxModel(bar)!;

        // Vertical stacking: the bar's top should be below the title's bottom.
        barBox.MarginBox.Top.ShouldBeGreaterThan(titleBox.MarginBox.Bottom - 1f,
            "Progress bar should render below the title, not beside it");
    }

    [Fact]
    public void ToolbarContent_MultipleChildren_AllStackVertically()
    {
        // Multiple children in toolbar content should all stack vertically.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;
        Context.ViewportHeight = 300f;

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(b =>
        {
            b.OpenComponent<IonTitle>(0);
            b.AddComponentParameter(1, nameof(IonTitle.ChildContent), (RenderFragment)(t => t.AddContent(0, "Title 1")));
            b.CloseComponent();
            b.OpenComponent<IonTitle>(2);
            b.AddComponentParameter(3, nameof(IonTitle.ChildContent), (RenderFragment)(t => t.AddContent(0, "Title 2")));
            b.CloseComponent();
            b.OpenComponent<IonProgressBar>(4);
            b.AddComponentParameter(5, nameof(IonProgressBar.Value), 0.5);
            b.CloseComponent();
        }));

        var title1 = cut.Root.FindByClass("ion-title").First();
        var title2 = cut.Root.FindByClass("ion-title").Skip(1).First();
        var bar = cut.Root.FindByClass("ion-progress-bar").First();

        var title1Box = cut.GetBoxModel(title1)!;
        var title2Box = cut.GetBoxModel(title2)!;
        var barBox = cut.GetBoxModel(bar)!;

        // All three should stack vertically
        title2Box.MarginBox.Top.ShouldBeGreaterThan(title1Box.MarginBox.Bottom - 1f);
        barBox.MarginBox.Top.ShouldBeGreaterThan(title2Box.MarginBox.Bottom - 1f);
    }

    [Theory]
    [InlineData(HostPlatform.Android)]  // md mode
    [InlineData(HostPlatform.Ios)]      // ios mode
    public void ToolbarContent_BothModes_StacksVertically(HostPlatform platform)
    {
        // The display:block fix applies to both md and ios modes.
        UsePlatform(platform);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;
        Context.ViewportHeight = 300f;

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(b =>
        {
            b.OpenComponent<IonTitle>(0);
            b.AddComponentParameter(1, nameof(IonTitle.ChildContent), (RenderFragment)(t => t.AddContent(0, "Page title")));
            b.CloseComponent();
            b.OpenComponent<IonProgressBar>(2);
            b.AddComponentParameter(3, nameof(IonProgressBar.Value), 0.3);
            b.CloseComponent();
        }));

        var title = cut.Root.FindByClass("ion-title").First();
        var bar = cut.Root.FindByClass("ion-progress-bar").First();

        var titleBox = cut.GetBoxModel(title)!;
        var barBox = cut.GetBoxModel(bar)!;

        barBox.MarginBox.Top.ShouldBeGreaterThan(titleBox.MarginBox.Bottom - 1f,
            $"{platform} platform: Progress bar should render below the title");
    }

    [Fact]
    public void ToolbarContent_HasDisplayBlock()
    {
        // The .toolbar-content element itself should have display:block.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(b =>
        {
            b.OpenComponent<IonTitle>(0);
            b.AddComponentParameter(1, nameof(IonTitle.ChildContent), (RenderFragment)(t => t.AddContent(0, "Title")));
            b.CloseComponent();
        }));

        var toolbarContent = cut.Root.FindByClass("toolbar-content").First();
        var style = cut.GetComputedStyle(toolbarContent)!;

        style.Display.ShouldBe(Display.Block,
            ".toolbar-content must have display:block so children stack vertically");
    }
}
