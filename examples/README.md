# Miko Examples

This folder contains example applications demonstrating how to use the Miko rendering engine.

## Available Examples

### Bootstrap-Style Buttons (`Miko.Examples.Bootstrap`)

A console application that demonstrates Bootstrap-inspired button styling using Miko's rendering engine.

**Features:**
- 8 button color variants (Primary, Secondary, Success, Danger, Warning, Info, Light, Dark)
- 3 button sizes (Small, Medium, Large)
- 7 outline button variants
- Flexbox layout for organizing buttons
- Renders to PNG image file

**How to run:**

```bash
# From the repository root
dotnet run --project examples/Miko.Examples.Bootstrap/Miko.Examples.Bootstrap.csproj

# Or navigate to the example directory
cd examples/Miko.Examples.Bootstrap
dotnet run
```

**Output:**
The example generates a PNG image at `examples/Miko.Examples.Bootstrap/output/bootstrap-buttons.png` showing all button variants.

**What it demonstrates:**
- Creating a DOM tree with container and button elements
- Applying styles using StyleSheet with class selectors
- Using Bootstrap-inspired color palette
- Flexbox layout for organizing elements
- Rendering to an image file using SkiaSharp

## Creating Your Own Examples

To create a new example:

1. **Create a new console project:**
   ```bash
   dotnet new console -n Miko.Examples.YourExample -o examples/Miko.Examples.YourExample
   ```

2. **Add reference to Miko:**
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\..\src\Miko\Miko.csproj" />
   </ItemGroup>
   ```

3. **Add SkiaSharp package:**
   ```xml
   <ItemGroup>
     <PackageReference Include="SkiaSharp" Version="3.119.1" />
   </ItemGroup>
   ```

4. **Add to solution file** (`miko.slnx`):
   ```xml
   <Folder Name="/examples/">
     <Project Path="examples/Miko.Examples.YourExample/Miko.Examples.YourExample.csproj" />
   </Folder>
   ```

5. **Implement your example** following this pattern:
   ```csharp
   using Miko.Core;
   using Miko.Core.DomElements;
   using Miko.Styling;
   using SkiaSharp;

   // Create DOM tree
   var root = new DivElement { /* ... */ };

   // Create stylesheet
   var styleSheet = new StyleSheet();
   // Add style rules...

   // Initialize engine and render
   using var surface = SKSurface.Create(new SKImageInfo(width, height));
   var canvas = surface.Canvas;
   var engine = new MikoEngine();
   engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, width, height);

   // Save output
   using var image = surface.Snapshot();
   using var data = image.Encode(SKEncodedImageFormat.Png, 100);
   using var stream = File.OpenWrite(outputPath);
   data.SaveTo(stream);
   ```

## Example Ideas

Here are some ideas for additional examples:

- **Forms Example** - Input fields, labels, form layouts
- **Cards Example** - Bootstrap-style card components
- **Navigation Example** - Navigation bars and menus
- **Grid Layout Example** - Multi-column grid layouts
- **Typography Example** - Headings, paragraphs, text styling
- **Flexbox Example** - Advanced flexbox layouts
- **Image Gallery Example** - Image elements with captions
- **Dashboard Example** - Complex layout with multiple components

## Resources

- [Miko Architecture Documentation](../docs/ARCHITECTURE.md)
- [Miko Requirements](../docs/REQUIREMENT.md)
- [Project Instructions](../CLAUDE.md)
