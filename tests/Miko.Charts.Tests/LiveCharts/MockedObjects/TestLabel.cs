// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.MockedObjects;

using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing.Geometries;

public class TestLabel : LabelGeometry
{
    public TestLabel()
    {
        Background = new LvcColor(200, 200, 200, 50);
    }
}
