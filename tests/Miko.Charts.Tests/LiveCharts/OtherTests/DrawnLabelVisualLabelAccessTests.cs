// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.VisualElements;
using LiveChartsCore.Painting;
using LiveChartsCore.VisualElements;
using Shouldly;

// Regression for #2124: DrawnLabelVisual hid its underlying LabelGeometry
// behind a protected internal DrawnElement, so callers that received a
// DrawnLabelVisual (e.g. chart.Title) had no way to read Text / TextSize /
// Paint / Padding back without reflection.
[Collection(ChartsCollection.Name)]
public class DrawnLabelVisualLabelAccessTests
{
    public DrawnLabelVisualLabelAccessTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void Label_Exposes_Default_Geometry_For_Parameterless_Ctor()
    {
        var visual = new DrawnLabelVisual();

        visual.Label.ShouldNotBeNull();
        visual.Label.Text.ShouldBe(string.Empty);
    }

    [Fact]
    public void Label_Returns_The_LabelGeometry_Passed_To_The_Ctor()
    {
        var geometry = new LabelGeometry { Text = "Hello" };
        var visual = new DrawnLabelVisual(geometry);

        visual.Label.ShouldBeSameAs(geometry);
    }

    [Fact]
    public void Label_Mutations_Are_Visible_Through_The_Property()
    {
        var visual = new DrawnLabelVisual();
        var padding = new Padding(4f);
        Paint paint = new SolidColorPaint(TestColors.Red);

        visual.Label.Text = "Revenue";
        visual.Label.TextSize = 16f;
        visual.Label.Padding = padding;
        visual.Label.Paint = paint;

        visual.Label.Text.ShouldBe("Revenue");
        visual.Label.TextSize.ShouldBe(16f);
        visual.Label.Padding.ShouldBeSameAs(padding);
        visual.Label.Paint.ShouldBeSameAs(paint);
    }

    [Fact]
    public void Label_Roundtrips_Through_Untyped_Reference()
    {
        // Models the user's reported use case: setting chart.Title (typed as
        // Visual?), then reading it back later through a pattern match.
        var padding = new Padding(2f);
        var paint = new SolidColorPaint(TestColors.Black);
        Visual title = new DrawnLabelVisual(new LabelGeometry
        {
            Text = "Revenue",
            TextSize = 16f,
            Paint = paint,
            Padding = padding
        });

        title.ShouldBeAssignableTo<DrawnLabelVisual>();
        var label = ((DrawnLabelVisual)title).Label;

        label.Text.ShouldBe("Revenue");
        label.TextSize.ShouldBe(16f);
        label.Paint.ShouldBeSameAs(paint);
        label.Padding.ShouldBeSameAs(padding);
    }
}
