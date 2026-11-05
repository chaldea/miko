// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.VisualElements;
using Shouldly;

// Regression for code-only chart titles being clipped at the top in WinForms /
// Blazor / Eto / Console samples.
// Root cause: BaseLabelGeometry defaults HorizontalAlign and VerticalAlign to
// Align.Middle. Chart.AddTitleToChart positions the title at (X, 0) and assumes
// the bbox top-left lives at that point (i.e. Align.Start). With the user-
// provided LabelGeometry going through DrawnLabelVisual(LabelGeometry) unchanged,
// the bbox was centered on Y=0 and the top half rendered at negative Y.
[Collection(ChartsCollection.Name)]
public class DrawnLabelTitleAlignmentTests
{
    public DrawnLabelTitleAlignmentTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void DrawnLabelVisual_Ctor_With_LabelGeometry_Forces_Align_Start()
    {
        var label = new LabelGeometry { Text = "title" };

        // sanity: BaseLabelGeometry's defaults are still Middle, so this test
        // exercises the constructor's override (not pre-set values).
        label.HorizontalAlign.ShouldBe(Align.Middle);
        label.VerticalAlign.ShouldBe(Align.Middle);

        _ = new DrawnLabelVisual(label);

        label.HorizontalAlign.ShouldBe(Align.Start);
        label.VerticalAlign.ShouldBe(Align.Start);
    }
}
