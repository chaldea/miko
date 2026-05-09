using Miko.Common;
using Miko.Core;
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

    [Fact]
    public void SelectElement_Dropdown_ShouldRenderAboveSubsequentElements()
    {
        // A select dropdown should render ON TOP of elements that come after it in the DOM.
        // This tests that the dropdown is painted as an overlay, not inline.
        var selectElement = new SelectElement();
        selectElement.AddChild(new OptionElement { TextContent = "Option 1", Value = "1" });
        selectElement.AddChild(new OptionElement { TextContent = "Option 2", Value = "2" });
        selectElement.AddChild(new OptionElement { TextContent = "Option 3", Value = "3" });
        selectElement.SelectedIndex = 0;
        selectElement.IsOpen = true;

        // Place a bright red div right below the select — the dropdown should cover it
        var redDiv = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                BackgroundColor = new Color(255, 0, 0)
            }
        };

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
            },
            Children = { selectElement, redDiv }
        };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("select"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = Color.White,
                        BorderWidth = Length.Px(1),
                        BorderColor = Color.Gray,
                        BorderStyle = BorderStyle.Solid,
                        FontSize = Length.Px(14)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 400, 300);

        _canvas.Clear(SKColors.White);
        _renderEngine.Render(layoutRoot);

        // The dropdown should cover the red div area.
        // The select is 30px tall, so the red div starts at y=30.
        // The dropdown also starts at y=30 (borderBox.Bottom).
        // Check a point in the dropdown area — it should NOT be red.
        // Dropdown has white background, so we should see white (not red).
        bool foundRedInDropdownArea = false;
        for (int y = 32; y < 60; y++)
        {
            for (int x = 10; x < 190; x += 20)
            {
                var pixel = _bitmap.GetPixel(x, y);
                if (pixel.Red == 255 && pixel.Green == 0 && pixel.Blue == 0)
                {
                    foundRedInDropdownArea = true;
                    break;
                }
            }
            if (foundRedInDropdownArea) break;
        }

        foundRedInDropdownArea.ShouldBeFalse(
            "The dropdown overlay should cover subsequent elements. " +
            "Red pixels from the div below should not be visible through the dropdown.");
    }

    [Fact]
    public void SelectElement_Dropdown_ShouldPositionBelowSelectElement()
    {
        // The dropdown should start at the bottom edge of the select, not overlap it.
        var selectElement = new SelectElement();
        selectElement.AddChild(new OptionElement { TextContent = "Option 1", Value = "1" });
        selectElement.AddChild(new OptionElement { TextContent = "Option 2", Value = "2" });
        selectElement.SelectedIndex = 0;
        selectElement.IsOpen = true;

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
            },
            Children = { selectElement }
        };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("select"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = new Color(200, 200, 200),
                        BorderWidth = Length.Px(1),
                        BorderColor = new Color(100, 100, 100),
                        BorderStyle = BorderStyle.Solid,
                        FontSize = Length.Px(14)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 400, 300);

        _canvas.Clear(new SKColor(0, 0, 255)); // Blue background

        _renderEngine.Render(layoutRoot);

        // The select element occupies y=0..30 (height 30 + 1px border top/bottom = ~32)
        // The dropdown should start at y = borderBox.Bottom (approximately 32)
        // Check that the dropdown border is visible just below the select
        // The dropdown has a 1px border drawn by DrawBorder

        // Check that there's NO dropdown content inside the select area (y=5..25)
        // The select has gray background (200,200,200), not the dropdown white
        var insideSelectPixel = _bitmap.GetPixel(100, 15);
        insideSelectPixel.Red.ShouldBe((byte)200,
            "Inside the select element area should show the select's own background, not dropdown content");

        // Check that there IS dropdown content below the select (y=35..60)
        // The dropdown has white background, so it should not be blue (page bg)
        var belowSelectPixel = _bitmap.GetPixel(100, 40);
        belowSelectPixel.ShouldNotBe(new SKColor(0, 0, 255),
            "Below the select element, the dropdown should be rendered (not page background)");
    }

    [Fact]
    public void SelectElement_Dropdown_ShouldHaveOpaqueBackground()
    {
        // The dropdown background must be fully opaque — elements behind it should not show through.
        var selectElement = new SelectElement();
        selectElement.AddChild(new OptionElement { TextContent = "A", Value = "a" });
        selectElement.AddChild(new OptionElement { TextContent = "B", Value = "b" });
        selectElement.AddChild(new OptionElement { TextContent = "C", Value = "c" });
        selectElement.SelectedIndex = 0;
        selectElement.IsOpen = true;

        // A green div that sits behind the dropdown area
        var greenDiv = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                BackgroundColor = new Color(0, 255, 0)
            }
        };

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
            },
            Children = { selectElement, greenDiv }
        };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("select"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = Color.White,
                        BorderWidth = Length.Px(1),
                        BorderColor = Color.Gray,
                        BorderStyle = BorderStyle.Solid,
                        FontSize = Length.Px(14)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 400, 300);

        _canvas.Clear(SKColors.White);
        _renderEngine.Render(layoutRoot);

        // The green div is at y=30..130. The dropdown also covers y=30..~96 (3 options * 22px).
        // Sample multiple points in the dropdown area — none should be green.
        for (int y = 33; y < 80; y += 10)
        {
            for (int x = 5; x < 195; x += 30)
            {
                var pixel = _bitmap.GetPixel(x, y);
                bool isGreen = pixel.Green == 255 && pixel.Red == 0 && pixel.Blue == 0;
                isGreen.ShouldBeFalse(
                    $"Pixel at ({x},{y}) is green — the dropdown background is not opaque and the element behind is showing through.");
            }
        }
    }

    [Fact]
    public void SelectElement_Dropdown_InScrolledContainer_ShouldRenderAtScreenPosition()
    {
        // When a select is inside a scrolled container, the dropdown should render
        // at the correct screen position, not at the layout position.
        var selectElement = new SelectElement();
        selectElement.AddChild(new OptionElement { TextContent = "Option 1", Value = "1" });
        selectElement.AddChild(new OptionElement { TextContent = "Option 2", Value = "2" });
        selectElement.SelectedIndex = 0;
        selectElement.IsOpen = true;

        var spacer = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(300),
                Height = Length.Px(100),
            }
        };

        var filler = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(300),
                Height = Length.Px(500),
            }
        };

        var scrollContainer = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(300),
                Height = Length.Px(200),
                OverflowY = Overflow.Auto,
            },
            Children = { spacer, selectElement, filler }
        };

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
            },
            Children = { scrollContainer }
        };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("select"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = Color.White,
                        BorderWidth = Length.Px(1),
                        BorderColor = new Color(100, 100, 100),
                        BorderStyle = BorderStyle.Solid,
                        FontSize = Length.Px(14)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 400, 300);

        // Scroll down by 80px — the select (at layout y=100) should now appear at screen y=20
        var scrollBox = layoutRoot.Children[0];
        scrollBox.ScrollTop = 80;

        _canvas.Clear(new SKColor(0, 0, 255)); // Blue background

        _renderEngine.Render(layoutRoot);

        // Select is at layout y=100, scrolled by 80, so screen y=20
        // Select height=30+2(border)=32, so select bottom at screen y≈52
        // Dropdown should start at screen y≈52, not at layout y=132

        // Check that dropdown content exists at screen y≈55 (just below select on screen)
        var dropdownPixel = _bitmap.GetPixel(100, 55);
        dropdownPixel.ShouldNotBe(new SKColor(0, 0, 255),
            "Dropdown should render at the screen position of the select's bottom edge, " +
            "accounting for scroll offset");

        // Check that there's NO dropdown at the layout position (y=132) if that's different
        // from the screen position. At layout y=132, screen y=132-80=52 which is close to
        // where it should be. Let's verify there's no dropdown at a clearly wrong position.
        // If scroll wasn't accounted for, dropdown would render at layout y=132 on screen.
        // That would be at screen y=132 (wrong). Check that area is blue (background).
        var wrongPositionPixel = _bitmap.GetPixel(100, 140);
        wrongPositionPixel.ShouldBe(new SKColor(0, 0, 255),
            "Dropdown should NOT render at the un-adjusted layout position when container is scrolled");
    }

    [Fact]
    public void SelectElement_WhenOpen_OptionsShouldNotRenderInsideSelectArea()
    {
        // When the dropdown is open, option children must NOT be rendered as inline
        // children overlapping the select element itself. Only the dropdown overlay
        // (below the select) should show option content.
        var selectElement = new SelectElement();
        selectElement.AddChild(new OptionElement { TextContent = "First Option", Value = "1" });
        selectElement.AddChild(new OptionElement { TextContent = "Second Option", Value = "2" });
        selectElement.SelectedIndex = 0;
        selectElement.IsOpen = true;

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
            },
            Children = { selectElement }
        };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("select"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(200),
                        Height = Length.Px(40),
                        BackgroundColor = new Color(0, 128, 0), // Dark green
                        BorderWidth = Length.Px(1),
                        BorderColor = Color.Gray,
                        BorderStyle = BorderStyle.Solid,
                        FontSize = Length.Px(14)
                    }
                },
                new StyleRule
                {
                    Selector = new TagSelector("option"),
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Height = Length.Px(30),
                        BackgroundColor = new Color(255, 0, 0) // Red - easy to detect
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { styleSheet }, 400, 300);
        _canvas.Clear(SKColors.White);

        _renderEngine.Render(layoutRoot);

        // The select element occupies y=0..42 (40 height + 1px border each side)
        // If options are incorrectly rendered as inline children, their red background
        // would appear inside the select area (y=1..41)
        bool foundRedInsideSelect = false;
        for (int y = 2; y < 40; y += 5)
        {
            for (int x = 5; x < 195; x += 20)
            {
                var pixel = _bitmap.GetPixel(x, y);
                if (pixel.Red > 200 && pixel.Green < 50 && pixel.Blue < 50)
                {
                    foundRedInsideSelect = true;
                    break;
                }
            }
            if (foundRedInsideSelect) break;
        }

        foundRedInsideSelect.ShouldBeFalse(
            "Option elements should NOT be rendered inside the select element area when the dropdown is open. " +
            "Options should only appear in the dropdown overlay below the select.");

        // Verify the select area still shows its own green background
        var selectCenter = _bitmap.GetPixel(100, 20);
        selectCenter.Green.ShouldBeGreaterThan((byte)100,
            "The select element should show its own background, not be covered by option rendering");
    }
}
