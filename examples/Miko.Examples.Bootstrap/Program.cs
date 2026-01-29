using Miko.Core;
using Miko.Core.DomElements;
using SkiaSharp;

namespace Miko.Examples.Bootstrap;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("Miko Bootstrap-Style Button Demo");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();
        Console.WriteLine("Rendering Bootstrap-style buttons...");

        // Create output directory
        var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "bootstrap-buttons.png");

        // Create the DOM tree
        var root = CreateButtonDemoDOM();

        // Create Bootstrap stylesheet
        var styleSheet = BootstrapStyles.CreateBootstrapStyleSheet();

        // Initialize MikoEngine
        const int width = 800;
        const int height = 800;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var engine = new MikoEngine();
        engine.Initialize(root, new List<Miko.Styling.StyleSheet> { styleSheet }, canvas, width, height);

        // Save the rendered image
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        Console.WriteLine();
        Console.WriteLine($"Success! Image saved to:");
        Console.WriteLine($"  {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Open the PNG file to view the Bootstrap-style buttons.");
        Console.WriteLine();
        Console.WriteLine("Button variants demonstrated:");
        Console.WriteLine("  - 8 color variants (Primary, Secondary, Success, etc.)");
        Console.WriteLine("  - 3 sizes (Small, Medium, Large)");
        Console.WriteLine("  - 7 outline variants");
        Console.WriteLine();
    }

    static Element CreateButtonDemoDOM()
    {
        return new DivElement
        {
            Class = "container",
            Children =
            {
                new H1Element { TextContent = "Bootstrap Button Examples" },

                // Standard Buttons Section
                new H2Element { TextContent = "Standard Buttons" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Primary", Class = "btn-primary" },
                        new ButtonElement { TextContent = "Secondary", Class = "btn-secondary" },
                        new ButtonElement { TextContent = "Success", Class = "btn-success" },
                        new ButtonElement { TextContent = "Danger", Class = "btn-danger" },
                        new ButtonElement { TextContent = "Warning", Class = "btn-warning" },
                        new ButtonElement { TextContent = "Info", Class = "btn-info" },
                        new ButtonElement { TextContent = "Light", Class = "btn-light" },
                        new ButtonElement { TextContent = "Dark", Class = "btn-dark" },
                    }
                },

                // Outline Buttons Section
                new H2Element { TextContent = "Outline Buttons" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Primary", Class = "btn-outline-primary" },
                        new ButtonElement { TextContent = "Secondary", Class = "btn-outline-secondary" },
                        new ButtonElement { TextContent = "Success", Class = "btn-outline-success" },
                        new ButtonElement { TextContent = "Danger", Class = "btn-outline-danger" },
                        new ButtonElement { TextContent = "Warning", Class = "btn-outline-warning" },
                        new ButtonElement { TextContent = "Info", Class = "btn-outline-info" },
                        new ButtonElement { TextContent = "Dark", Class = "btn-outline-dark" },
                    }
                },

                // Button Sizes Section
                new H2Element { TextContent = "Button Sizes" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Small Button", Class = "btn-primary btn-sm" },
                        new ButtonElement { TextContent = "Medium Button", Class = "btn-primary" },
                        new ButtonElement { TextContent = "Large Button", Class = "btn-primary btn-lg" },
                    }
                },

                // Mixed Examples Section
                new H2Element { TextContent = "Mixed Examples" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Small Success", Class = "btn-success btn-sm" },
                        new ButtonElement { TextContent = "Large Danger", Class = "btn-danger btn-lg" },
                        new ButtonElement { TextContent = "Small Outline", Class = "btn-outline-info btn-sm" },
                    }
                },
            }
        };
    }
}
