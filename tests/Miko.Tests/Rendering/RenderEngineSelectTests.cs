using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// Tests for RenderEngine behavior with SelectElement
/// </summary>
public class RenderEngineSelectTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public RenderEngineSelectTests()
    {
        _bitmap = new SKBitmap(400, 300);
        _canvas = new SKCanvas(_bitmap);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
        _layoutEngine = new LayoutEngine();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>
    /// Creates a SelectElement with options and performs layout
    /// </summary>
    private LayoutBox CreateSelectWithOptions(bool isOpen, out SelectElement selectElement)
    {
        selectElement = new SelectElement();
        selectElement.AddChild(new OptionElement { TextContent = "Option 1", Value = "1" });
        selectElement.AddChild(new OptionElement { TextContent = "Option 2", Value = "2" });
        selectElement.AddChild(new OptionElement { TextContent = "Option 3", Value = "3" });
        selectElement.SelectedIndex = 0;
        selectElement.IsOpen = isOpen;

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("select"),
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = Color.White,
                        BorderWidth = Length.Px(1),
                        BorderColor = Color.Gray,
                        BorderStyle = BorderStyle.Solid
                    }
                },
                new StyleRule
                {
                    Selector = new TagSelector("option"),
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Height = Length.Px(24),
                        BackgroundColor = Color.LightGray
                    }
                }
            }
        };

        return _layoutEngine.Layout(selectElement, new List<StyleSheet> { styleSheet }, 400, 300);
    }

    [Fact]
    public void SelectElement_WhenClosed_ShouldNotRenderOptionChildren()
    {
        // Arrange
        var layoutRoot = CreateSelectWithOptions(isOpen: false, out var selectElement);

        // Clear to white background
        _canvas.Clear(SKColors.White);

        // Act
        _renderEngine.Render(layoutRoot);

        // Assert - Check that select element itself was rendered (border should be visible)
        // The select element is at the top-left, check its border area
        var selectBorderPixel = _bitmap.GetPixel(0, 15); // Left edge of select border

        // The option elements should NOT be rendered below the select
        // Options would be at y=30 (below the select which has height 30)
        // If options were rendered, we'd see their light gray background at y=35
        var belowSelectPixel = _bitmap.GetPixel(100, 35);

        // Below the select should be white (background), not light gray (option background)
        // This verifies that option children are NOT being rendered when select is closed
        belowSelectPixel.ShouldBe(SKColors.White,
            "Option elements should not be rendered when SelectElement.IsOpen is false");
    }

    [Fact]
    public void SelectElement_WhenOpen_ShouldRenderDropdownList()
    {
        // Arrange
        var layoutRoot = CreateSelectWithOptions(isOpen: true, out var selectElement);

        // Clear to white background
        _canvas.Clear(SKColors.White);

        // Act
        _renderEngine.Render(layoutRoot);

        // Assert - When open, the dropdown should be rendered below the select
        // The dropdown is rendered by RenderSelectDropdown with a white background and border
        var belowSelectPixel = _bitmap.GetPixel(100, 35);

        // When dropdown is rendered, it should NOT be the white page background
        // (it will either be the dropdown white or an option's background)
        // The key point is that SOMETHING is rendered there

        // Note: The dropdown is rendered by RenderSelectDropdown, which uses
        // Painter.DrawSelectDropdown with a white background. So we check
        // that we have dropdown content by verifying the border is drawn.
        var dropdownBorderLeft = _bitmap.GetPixel(0, 40);

        // The dropdown border should be visible (gray color from style)
        dropdownBorderLeft.ShouldNotBe(SKColors.White,
            "Dropdown should be rendered when SelectElement.IsOpen is true");
    }

    [Fact]
    public void SelectElement_IsOpen_DefaultsToFalse()
    {
        // This test confirms the default state
        var selectElement = new SelectElement();
        selectElement.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void SelectElement_OptionChildren_ShouldBeInLayoutTree()
    {
        // This test verifies that option children ARE included in the layout tree
        // (the fix should be in rendering, not layout)
        var layoutRoot = CreateSelectWithOptions(isOpen: false, out var selectElement);

        // Layout tree should include the option children
        layoutRoot.Children.Count.ShouldBe(3,
            "Option children should be in the layout tree for layout calculations");
    }

    [Fact]
    public void SelectElement_WhenClosed_ChildLayoutBoxesShouldExist()
    {
        // Verify that closing the select doesn't affect the layout tree structure
        var layoutRoot = CreateSelectWithOptions(isOpen: false, out var selectElement);

        // All option elements should have LayoutBox assigned
        foreach (var option in selectElement.GetAllOptions())
        {
            option.LayoutBox.ShouldNotBeNull();
        }
    }

    [Fact]
    public void SelectElement_WhenClosed_OptionBackgroundShouldNotBeRendered()
    {
        // Arrange - Use a very distinct color for option background
        var selectElement = new SelectElement();
        selectElement.AddChild(new OptionElement { TextContent = "Option 1", Value = "1" });
        selectElement.AddChild(new OptionElement { TextContent = "Option 2", Value = "2" });
        selectElement.SelectedIndex = 0;
        selectElement.IsOpen = false; // Closed state

        // Use a bright red color for options to make them easy to detect
        var optionBgColor = new Color(255, 0, 0); // Bright red

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("select"),
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = new Color(200, 200, 200) // Light gray for select
                    }
                },
                new StyleRule
                {
                    Selector = new TagSelector("option"),
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = optionBgColor // Bright red
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(selectElement, new List<StyleSheet> { styleSheet }, 400, 300);

        // Clear canvas to blue background (easy to distinguish)
        _canvas.Clear(new SKColor(0, 0, 255)); // Blue

        // Act
        _renderEngine.Render(layoutRoot);

        // Assert - Check multiple points below the select to find red pixels
        // If OptionElement is being rendered, we will see red pixels
        // Options should be laid out below the select (which is 30px high)
        bool foundRedPixel = false;
        for (int y = 31; y < 100; y++)
        {
            for (int x = 10; x < 190; x += 10)
            {
                var pixel = _bitmap.GetPixel(x, y);
                if (pixel.Red == 255 && pixel.Green == 0 && pixel.Blue == 0)
                {
                    foundRedPixel = true;
                    break;
                }
            }
            if (foundRedPixel) break;
        }

        // When SelectElement is CLOSED, we should NOT see any red pixels (option background)
        // because the option children should not be rendered
        foundRedPixel.ShouldBeFalse(
            "Option elements with red background should NOT be rendered when SelectElement.IsOpen is false. " +
            "This is inconsistent with browser behavior where options are only shown when the dropdown is open.");
    }
}
