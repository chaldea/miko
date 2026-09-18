using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Layout coverage for the toolbar's default slot. Ordinary children remain in block flow, while
/// a directly slotted progress bar is absolutely pinned across the toolbar's bottom edge.
/// </summary>
public class IonToolbarContentLayoutTests : IonicComponentTestBase
{
    [Fact]
    public void ToolbarContent_ProgressBarAppearsBelowTitle()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;
        Context.ViewportHeight = 300f;

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(b =>
        {
            var __c1 = b.OpenComponent<IonTitle>();
            __c1.ChildContent = (RenderFragment)(t => t.AddContent("Page title"));
            b.CloseComponent();
            var __c2 = b.OpenComponent<IonProgressBar>();
            __c2.Value = 0.4;
            b.CloseComponent();
        }));

        var title = cut.Root.FindByClass("ion-title").First();
        var bar = cut.Root.FindByClass("ion-progress-bar").First();

        var titleBox = cut.GetBoxModel(title)!;
        var barBox = cut.GetBoxModel(bar)!;

        barBox.MarginBox.Top.ShouldBeGreaterThan(titleBox.MarginBox.Bottom - 1f,
            "Progress bar should be pinned below the title, not laid out beside it");
    }

    [Fact]
    public void ToolbarContent_MultipleTitlesStackAndProgressStaysAtBottom()
    {
        // Multiple children in toolbar content should all stack vertically.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;
        Context.ViewportHeight = 300f;

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(b =>
        {
            var __c3 = b.OpenComponent<IonTitle>();
            __c3.ChildContent = (RenderFragment)(t => t.AddContent("Title 1"));
            b.CloseComponent();
            var __c4 = b.OpenComponent<IonTitle>();
            __c4.ChildContent = (RenderFragment)(t => t.AddContent("Title 2"));
            b.CloseComponent();
            var __c5 = b.OpenComponent<IonProgressBar>();
            __c5.Value = 0.5;
            b.CloseComponent();
        }));

        var title1 = cut.Root.FindByClass("ion-title").First();
        var title2 = cut.Root.FindByClass("ion-title").Skip(1).First();
        var bar = cut.Root.FindByClass("ion-progress-bar").First();

        var title1Box = cut.GetBoxModel(title1)!;
        var title2Box = cut.GetBoxModel(title2)!;
        var barBox = cut.GetBoxModel(bar)!;

        // Titles remain in normal block flow; the progress bar is independently pinned below them.
        title2Box.MarginBox.Top.ShouldBeGreaterThan(title1Box.MarginBox.Bottom - 1f);
        barBox.MarginBox.Top.ShouldBeGreaterThan(title2Box.MarginBox.Bottom - 1f);
    }

    [Theory]
    [InlineData(HostPlatform.Android)]  // md mode
    [InlineData(HostPlatform.Ios)]      // ios mode
    public void ToolbarContent_BothModes_PlaceProgressBelowTitle(HostPlatform platform)
    {
        UsePlatform(platform);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;
        Context.ViewportHeight = 300f;

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(b =>
        {
            var __c6 = b.OpenComponent<IonTitle>();
            __c6.ChildContent = (RenderFragment)(t => t.AddContent("Page title"));
            b.CloseComponent();
            var __c7 = b.OpenComponent<IonProgressBar>();
            __c7.Value = 0.3;
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
            var __c8 = b.OpenComponent<IonTitle>();
            __c8.ChildContent = (RenderFragment)(t => t.AddContent("Title"));
            b.CloseComponent();
        }));

        var toolbarContent = cut.Root.FindByClass("toolbar-content").First();
        var style = cut.GetComputedStyle(toolbarContent)!;

        style.Display.ShouldBe(Display.Block,
            ".toolbar-content must keep ordinary default-slot children in block flow");
    }

    [Fact]
    public void ToolbarContainer_UsesAutoHeightForWrappingContent()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 180f;
        Context.ViewportHeight = 400f;

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(content =>
        {
            var __c9 = content.OpenComponent<IonBreadcrumbs>();
            __c9.ChildContent = (RenderFragment)(crumbs =>
            {
                var labels = new[] { "Home", "Electronics", "Photography", "Cameras", "Film", "35 mm" };
                for (var i = 0; i < labels.Length; i++)
                {
                    var label = labels[i];
                    crumbs.OpenComponent<IonBreadcrumb>(i * 3);
                    crumbs.AddComponentParameter(i * 3 + 1, nameof(IonBreadcrumb.ChildContent),
                        (RenderFragment)(text => text.AddContent(0, label)));
                    crumbs.CloseComponent();
                }
            });
            content.CloseComponent();
        }));

        var container = cut.FindByClass("toolbar-container").Single();
        var containerStyle = cut.GetComputedStyle(container)!;
        var containerBox = cut.GetBoxModel(container)!;

        containerStyle.Height.ShouldBe(Length.Auto);
        containerStyle.MinHeight.ShouldBe(Length.Px(IonicTheme.CreateMd().ToolbarMinHeight));
        containerBox.BorderBox.Height.ShouldBeGreaterThan(IonicTheme.CreateMd().ToolbarMinHeight);
    }

    [Theory]
    [InlineData(HostPlatform.Android)]
    [InlineData(HostPlatform.Ios)]
    public void ToolbarProgressBar_IsPinnedAcrossContainerBottom(HostPlatform platform)
    {
        UsePlatform(platform);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        Context.ViewportWidth = 400f;
        Context.ViewportHeight = 300f;

        var cut = Context.Render<IonToolbar>(p =>
        {
            p.Add(nameof(IonToolbar.Start), (RenderFragment)(start =>
            {
                var __c10 = start.OpenComponent<IonBackButton>();
                __c10.DefaultHref = "/";
                start.CloseComponent();
            }));
            p.AddChildContent(content =>
            {
                var __c11 = content.OpenComponent<IonTitle>();
                __c11.ChildContent = 
                    (RenderFragment)(title => title.AddContent("Progress"));
                content.CloseComponent();
                var __c12 = content.OpenComponent<IonProgressBar>();
                __c12.Type = "indeterminate";
                __c12.Color = "dark";
                content.CloseComponent();
            });
        });

        var container = cut.FindByClass("toolbar-container").Single();
        var progress = cut.FindByClass("ion-progress-bar").Single();
        var progressStyle = cut.GetComputedStyle(progress)!;

        progressStyle.Position.ShouldBe(Position.Absolute);
        progressStyle.Left.ShouldBe(Length.Px(0));
        progressStyle.Right.ShouldBe(Length.Px(0));
        progressStyle.Bottom.ShouldBe(Length.Px(0));
        progressStyle.Width.ShouldBe(Length.Auto);

        var containerBox = cut.GetBoxModel(container)!;
        var progressBox = cut.GetBoxModel(progress)!;
        progressBox.BorderBox.Left.ShouldBe(containerBox.PaddingBox.Left, 0.01f);
        progressBox.BorderBox.Right.ShouldBe(containerBox.PaddingBox.Right, 0.01f);
        progressBox.BorderBox.Bottom.ShouldBe(containerBox.PaddingBox.Bottom, 0.01f);
        progressBox.BorderBox.Width.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void NestedProgressBar_IsNotTreatedAsDirectSlottedContent()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonToolbar>(p => p.AddChildContent(content =>
        {
            content.OpenElement<DivElement>();
            content.OpenComponent<IonProgressBar>();
            content.CloseComponent();
            content.CloseElement();
        }));

        var progress = cut.FindByClass("ion-progress-bar").Single();
        cut.GetComputedStyle(progress)!.Position.ShouldBe(Position.Relative);
    }
}
