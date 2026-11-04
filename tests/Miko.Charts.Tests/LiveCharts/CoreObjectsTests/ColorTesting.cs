// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.CoreObjectsTests;

using LiveChartsCore.Drawing;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class ColorTesting
{
    public ColorTesting() => CoreTestsSetup.Ensure();

    [Fact]
    public void Compare()
    {
        (new LvcColor(123, 30, 29) != new LvcColor(123, 32, 23)).ShouldBeTrue();
        (new LvcColor(123, 30, 29) == new LvcColor(123, 30, 29)).ShouldBeTrue();
        (LvcColor.Empty != new LvcColor(123, 30, 29)).ShouldBeTrue();
        (LvcColor.Empty == LvcColor.Empty).ShouldBeTrue();

        var a = LvcColor.Empty;
        a.R = 1;
        a.G = 1;
        a.B = 1;
        a.A = 1;

        (a != new LvcColor(1, 1, 1, 1)).ShouldBeTrue();
    }
}
