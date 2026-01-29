using Miko.Common;
using Miko.Layout;
using Shouldly;

namespace Miko.Tests.Layout;

public class BoxModelTests
{
    [Fact]
    public void PaddingBox_ShouldIncludePadding()
    {
        var boxModel = new BoxModel
        {
            Content = new RectF(100, 100, 200, 150),
            Padding = new EdgeSizes(10, 20, 30, 40)
        };

        var paddingBox = boxModel.PaddingBox;

        paddingBox.Left.ShouldBe(60);  // 100 - 40 (left padding)
        paddingBox.Top.ShouldBe(90);   // 100 - 10 (top padding)
        paddingBox.Width.ShouldBe(260); // 200 + 40 + 20 (left + right)
        paddingBox.Height.ShouldBe(190); // 150 + 10 + 30 (top + bottom)
    }

    [Fact]
    public void BorderBox_ShouldIncludePaddingAndBorder()
    {
        var boxModel = new BoxModel
        {
            Content = new RectF(100, 100, 200, 150),
            Padding = new EdgeSizes(10, 20, 30, 40),
            Border = new EdgeSizes(5)
        };

        var borderBox = boxModel.BorderBox;

        borderBox.Left.ShouldBe(55);   // 60 - 5 (border)
        borderBox.Top.ShouldBe(85);    // 90 - 5 (border)
        borderBox.Width.ShouldBe(270); // 260 + 5 + 5
        borderBox.Height.ShouldBe(200); // 190 + 5 + 5
    }

    [Fact]
    public void MarginBox_ShouldIncludeAllLayers()
    {
        var boxModel = new BoxModel
        {
            Content = new RectF(100, 100, 200, 150),
            Padding = new EdgeSizes(10),
            Border = new EdgeSizes(5),
            Margin = new EdgeSizes(20)
        };

        var marginBox = boxModel.MarginBox;

        marginBox.Left.ShouldBe(65);   // Border box left (85) - 20 (margin)
        marginBox.Top.ShouldBe(65);    // Border box top (85) - 20 (margin)
        marginBox.Width.ShouldBe(270); // Border box width (230) + 40 (margin)
        marginBox.Height.ShouldBe(220); // Border box height (180) + 40 (margin)
    }

    [Fact]
    public void BoxModel_WithZeroSizes_ShouldWorkCorrectly()
    {
        var boxModel = new BoxModel
        {
            Content = new RectF(50, 50, 100, 100),
            Padding = new EdgeSizes(0),
            Border = new EdgeSizes(0),
            Margin = new EdgeSizes(0)
        };

        var paddingBox = boxModel.PaddingBox;
        var borderBox = boxModel.BorderBox;
        var marginBox = boxModel.MarginBox;

        paddingBox.Left.ShouldBe(50);
        paddingBox.Top.ShouldBe(50);
        paddingBox.Width.ShouldBe(100);
        paddingBox.Height.ShouldBe(100);

        borderBox.Left.ShouldBe(50);
        borderBox.Width.ShouldBe(100);

        marginBox.Left.ShouldBe(50);
        marginBox.Width.ShouldBe(100);
    }

    [Fact]
    public void BoxModel_WithAsymmetricSizes_ShouldCalculateCorrectly()
    {
        var boxModel = new BoxModel
        {
            Content = new RectF(0, 0, 100, 100),
            Padding = new EdgeSizes(5, 10, 15, 20), // top, right, bottom, left
            Border = new EdgeSizes(2, 4, 6, 8),
            Margin = new EdgeSizes(1, 2, 3, 4)
        };

        var paddingBox = boxModel.PaddingBox;
        paddingBox.Left.ShouldBe(-20);
        paddingBox.Top.ShouldBe(-5);
        paddingBox.Width.ShouldBe(130); // 100 + 20 + 10
        paddingBox.Height.ShouldBe(120); // 100 + 5 + 15

        var borderBox = boxModel.BorderBox;
        borderBox.Left.ShouldBe(-28);  // -20 - 8
        borderBox.Top.ShouldBe(-7);    // -5 - 2
        borderBox.Width.ShouldBe(142); // 130 + 8 + 4
        borderBox.Height.ShouldBe(128); // 120 + 2 + 6
    }
}
