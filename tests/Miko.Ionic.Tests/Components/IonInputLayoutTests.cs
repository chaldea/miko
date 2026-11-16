using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

public class IonInputLayoutTests : IonicComponentTestBase
{
    [Theory]
    [InlineData(HostPlatform.Android, true)]
    [InlineData(HostPlatform.Ios, true)]
    [InlineData(HostPlatform.Android, false)]
    [InlineData(HostPlatform.Ios, false)]
    public void StackedLabel_HasTopSpacingAndDoesNotOverlapInput(HostPlatform platform, bool inItem)
    {
        UsePlatform(platform);
        Context.ViewportWidth = 390;
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        // Distinguish the label and value in the actual rendered output. Transforms affect
        // painting, not BoxModel, so a layout-only assertion would miss the original defect.
        var colors = new StyleSheet();
        colors.Add(new CssObject
        {
            [".ion-input .label-text"] = new() { Color = Color.Red },
            [".ion-input .native-input"] = new() { Color = Color.Blue },
        });
        Context.AddStyleSheet(colors);

        RenderFragment input = builder =>
        {
            var component = builder.OpenComponent<IonInput>();
            component.Label = "First Name";
            component.LabelPlacement = "stacked";
            component.Value = "0000000000";
            builder.CloseComponent();
        };
        var cut = inItem
            ? Context.Render<IonItem>(p => p.Add(nameof(IonItem.ChildContent), input))
            : Context.Render<IonInput>(p => p
                .Add(nameof(IonInput.Label), "First Name")
                .Add(nameof(IonInput.LabelPlacement), "stacked")
                .Add(nameof(IonInput.Value), "0000000000"));

        var host = cut.FindByClass("ion-input").Single();
        var wrapper = host.Children.Single(e => e.HasClass("input-wrapper"));
        var hostBox = cut.GetBoxModel(host)!;
        var wrapperBox = cut.GetBoxModel(wrapper)!;
        wrapperBox.BorderBox.Height.ShouldBe(hostBox.Content.Height, 0.5f);
        wrapperBox.BorderBox.Height.ShouldBeGreaterThanOrEqualTo(56f);

        using var bitmap = new SKBitmap(390, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(cut.Root, Context.StyleSheets, canvas, 390, 100);
        engine.Render(canvas);

        var labelRows = InkRows(bitmap, label: true);
        var valueRows = InkRows(bitmap, label: false);
        labelRows.ShouldNotBeEmpty();
        valueRows.ShouldNotBeEmpty();
        labelRows.Min().ShouldBeGreaterThanOrEqualTo((int)hostBox.Content.Y + 8);
        labelRows.Max().ShouldBeLessThan(valueRows.Min());
        labelRows.Count.ShouldBeLessThan(valueRows.Count);
        valueRows.Max().ShouldBeLessThan((int)wrapperBox.BorderBox.Bottom);
    }

    [Theory]
    [InlineData(HostPlatform.Android, true)]
    [InlineData(HostPlatform.Ios, true)]
    [InlineData(HostPlatform.Android, false)]
    [InlineData(HostPlatform.Ios, false)]
    public void ItemStyles_DoNotOverrideInputAlignment_WhenRegisteredInEitherOrder(
        HostPlatform platform, bool inputFirst)
    {
        UsePlatform(platform);
        var registry = new IonicStyleRegistry();
        Context.Services.AddSingleton(registry);
        Context.AddStyleSheet(registry.StyleSheet);
        if (inputFirst) Context.Render<IonInput>();

        var cut = Context.Render<IonItem>(p => p.Add(nameof(IonItem.ChildContent),
            (RenderFragment)(builder =>
            {
                var input = builder.OpenComponent<IonInput>();
                input.Label = "First Name";
                builder.CloseComponent();
            })));
        var host = cut.FindByClass("ion-input").Single();
        var wrapper = host.Children.Single(e => e.HasClass("input-wrapper"));
        cut.GetComputedStyle(wrapper)!.AlignItems.ShouldBe(AlignItems.Stretch);

        var label = cut.GetBoxModel(cut.FindByClass("label-text-wrapper").Single())!;
        label.Content.Height.ShouldBe(cut.GetBoxModel(wrapper)!.Content.Height, 0.5f);
    }

    private static List<int> InkRows(SKBitmap bitmap, bool label)
    {
        var rows = new List<int>();
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            if (label ? pixel.Red > 150 && pixel.Green < 100 && pixel.Blue < 100
                      : pixel.Blue > 150 && pixel.Red < 100 && pixel.Green < 100)
            {
                rows.Add(y);
                break;
            }
        }
        return rows;
    }
}
