using Miko.Components;
using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Rendering;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

public class IonHeaderTests : IonicComponentTestBase
{
    [Fact]
    public void IonHeader_RendersWithCorrectClass()
    {
        // Act
        var cut = Context.Render<IonHeader>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-header");
    }

    [Fact]
    public void IonHeader_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonHeader>();

        // Assert - DOM structure is the component contract
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-header");
        cut.Root.Children.Count.ShouldBe(0); // No children when ChildContent is empty
    }

    [Fact]
    public void IonHeader_MdShadow_IsVisibleInsideTabsContent()
    {
        Context.ViewportWidth = 400;
        Context.ViewportHeight = 300;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonPage>(page => page.AddChildContent(pageBuilder =>
        {
            pageBuilder.OpenComponent<IonTabs>(0);
            pageBuilder.AddComponentParameter(1, nameof(IonTabs.Content), (RenderFragment)(tabsContent =>
            {
                tabsContent.OpenComponent<IonHeader>(0);
                tabsContent.AddComponentParameter(1, nameof(IonHeader.ChildContent), (RenderFragment)(header =>
                {
                    header.OpenComponent<IonToolbar>(0);
                    header.AddComponentParameter(1, nameof(IonToolbar.ChildContent), (RenderFragment)(toolbar =>
                    {
                        toolbar.OpenComponent<IonTitle>(0);
                        toolbar.AddComponentParameter(1, nameof(IonTitle.ChildContent),
                            (RenderFragment)(title => title.AddContent(0, "Music")));
                        toolbar.CloseComponent();
                    }));
                    header.CloseComponent();
                }));
                tabsContent.CloseComponent();

                tabsContent.OpenComponent<IonContent>(2);
                tabsContent.CloseComponent();
            }));
            pageBuilder.AddComponentParameter(2, nameof(IonTabs.Bottom), (RenderFragment)(tabsBar =>
            {
                tabsBar.OpenComponent<IonTabBar>(0);
                tabsBar.CloseComponent();
            }));
            pageBuilder.CloseComponent();
        }));

        var tabsInner = cut.FindByClass("tabs-inner").Single();
        cut.FindLayoutBox(tabsInner)!.BoxModel.BorderBox.Height.ShouldBe(243f, 0.01f);

        using var bitmap = new SKBitmap(400, 300);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var renderer = new RenderEngine();
        renderer.SetCanvas(canvas);
        renderer.Render(cut.Layout);

        // The 56px toolbar ends at y=56. Its MD elevation must remain visible over IonContent.
        bitmap.GetPixel(200, 58).Red.ShouldBeLessThan((byte)255);
    }
}
