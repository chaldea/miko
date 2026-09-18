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
            var __c1 = pageBuilder.OpenComponent<IonTabs>();
            __c1.Content = (RenderFragment)(tabsContent =>
            {
                var __c2 = tabsContent.OpenComponent<IonHeader>();
                __c2.ChildContent = (RenderFragment)(header =>
                {
                    var __c3 = header.OpenComponent<IonToolbar>();
                    __c3.ChildContent = (RenderFragment)(toolbar =>
                    {
                        var __c4 = toolbar.OpenComponent<IonTitle>();
                        __c4.ChildContent = 
                            (RenderFragment)(title => title.AddContent("Music"));
                        toolbar.CloseComponent();
                    });
                    header.CloseComponent();
                });
                tabsContent.CloseComponent();

                tabsContent.OpenComponent<IonContent>();
                tabsContent.CloseComponent();
            });
            __c1.Bottom = (RenderFragment)(tabsBar =>
            {
                tabsBar.OpenComponent<IonTabBar>();
                tabsBar.CloseComponent();
            });
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
