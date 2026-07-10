# Example: Counter page (blank layout)

The minimal Miko app: one routed page with state, an `@onclick` handler, and a stylesheet built in C#. This is what `dotnet new miko-razor --layout blank` scaffolds.

## `Pages/Home.razor`

```razor
@page "/"
@namespace MyApp.Pages

<div class="home">
    <h1 class="home-title">Welcome to Miko</h1>
    <p class="home-subtitle">Your Razor app is up and running.</p>

    <div class="counter">
        <button class="counter-btn" @onclick="Increment">Clicked @_count times</button>
    </div>
</div>

@code {
    private int _count = 0;
    private void Increment() => _count++;   // component re-renders automatically
}
```

## `MainLayout.razor`

```razor
@inherits Miko.Components.LayoutComponentBase
@namespace MyApp

<div class="layout">
    <div class="main-content">
        @Body
    </div>
</div>
```

## `GlobalStyles.cs`

Styles are C# objects, keyed by CSS-selector strings via `CssObject`:

```csharp
using Miko.Common;
using Miko.Styling;

namespace MyApp;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".layout"]       = new() { Display = Display.Flex, Width = Length.Percent(100), Height = Length.Percent(100) },
            [".main-content"] = new() { FlexGrow = 1, Display = Display.Flex },
            [".home"] = new()
            {
                FlexGrow = 1, Display = Display.Flex, FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
                Padding = new Padding(24),
            },
            [".home-title"]    = new() { FontSize = Length.Px(32), FontWeight = FontWeight.Bold, Color = Color.FromRgb(33, 37, 41), MarginBottom = Length.Px(8) },
            [".home-subtitle"] = new() { FontSize = Length.Px(16), Color = Color.FromRgb(108, 117, 125), MarginBottom = Length.Px(24) },
            [".counter-btn"]   = new()
            {
                Padding = new Padding(10, 20), BackgroundColor = Color.FromRgb(13, 110, 253),
                Color = Color.White, FontSize = Length.Px(14), BorderRadius = new BorderRadius(Length.Px(6)),
                Cursor = Cursor.Pointer,
            },
            [".counter-btn:hover"] = new() { BackgroundColor = Color.FromRgb(11, 94, 215) },
        });
        return sheet;
    }
}
```

The stylesheet is registered in `App.cs` with `builder.AddStyleSheet(GlobalStyles.Create());`.
