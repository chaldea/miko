using Miko.Core;
using Miko.Examples.Bootstrap.Examples;
using SkiaSharp;

namespace Miko.Examples.Bootstrap;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("Miko Bootstrap-Style Examples");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();

        // Create output directory
        var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(outputDir);

        // Create Bootstrap stylesheet
        var styleSheet = BootstrapStyles.CreateBootstrapStyleSheet();
        var styleSheets = new List<Miko.Styling.StyleSheet> { styleSheet };

        // Render Button Example
        RenderExample(
            ButtonExample.Title,
            ButtonExample.CreateDOM(),
            styleSheets,
            Path.Combine(outputDir, ButtonExample.OutputFileName),
            800, 800
        );

        Console.WriteLine();

        // Render Form Control Example
        RenderExample(
            FormControlExample.Title,
            FormControlExample.CreateDOM(),
            styleSheets,
            Path.Combine(outputDir, FormControlExample.OutputFileName),
            800, 1800
        );

        Console.WriteLine();

        // Render List Example
        RenderExample(
            ListExample.Title,
            ListExample.CreateDOM(),
            styleSheets,
            Path.Combine(outputDir, ListExample.OutputFileName),
            800, 1900
        );

        Console.WriteLine();

        // Render Table Example
        RenderExample(
            TableExample.Title,
            TableExample.CreateDOM(),
            styleSheets,
            Path.Combine(outputDir, TableExample.OutputFileName),
            800, 2100
        );

        Console.WriteLine();
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("All examples rendered successfully!");
        Console.WriteLine($"Output directory: {outputDir}");
        Console.WriteLine("=".PadRight(60, '='));
    }

    static void RenderExample(
        string title,
        Element root,
        List<Miko.Styling.StyleSheet> styleSheets,
        string outputPath,
        int width,
        int height)
    {
        Console.WriteLine($"Rendering: {title}");

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var engine = new MikoEngine();
        engine.Initialize(root, styleSheets, canvas, width, height);

        // Save the rendered image
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        Console.WriteLine($"  -> Saved to: {Path.GetFileName(outputPath)}");
    }
}
