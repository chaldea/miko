// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.CoreObjectsTests;

using System.Globalization;
using LiveChartsCore;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class LabelerTesting
{
    public LabelerTesting() => CoreTestsSetup.Ensure();

    [Fact]
    public void NamedLabeler_NullEntry_ReturnsEmpty()
    {
        // regression for #1826: a null entry in Axis.Labels would flow as a null
        // string into the rendering pipeline. The named labeler must coerce
        // nulls to string.Empty so the chart renders the position blank instead
        // of failing silently downstream.
        var labeler = Labelers.BuildNamedLabeler(new List<string> { "A", null!, "C" });

        labeler(0).ShouldBe("A");
        labeler(1).ShouldBe(string.Empty);
        labeler(2).ShouldBe("C");
        labeler(3).ShouldBe(string.Empty);  // out of range
        labeler(-1).ShouldBe(string.Empty); // out of range
    }

    [Fact]
    public void Million()
    {
        string result;

        result = Labelers.SixRepresentativeDigits(100000000);
        (result == "100 M").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(10000000);
        (result == "10 M").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(1000000);
        (result == "1 M").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(100000);
        (result == "100000").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(-1000000);
        (result == "-1 M").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(-10000000);
        (result == "-10 M").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(-100000000);
        (result == "-100 M").ShouldBeTrue();
    }

    [Fact]
    public void Micra()
    {
        string result;

        result = Labelers.SixRepresentativeDigits(0.00000001);
        (result == "0.01 µ").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(0.0000001);
        (result == "0.1 µ").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(0.000001);
        (result == "1 µ").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(0.00001);
        (result == "0.00001").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(-0.000001);
        (result == "-1 µ").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(-0.00000001);
        (result == "-0.01 µ").ShouldBeTrue();
        result = Labelers.SixRepresentativeDigits(-0.0000001);
        (result == "-0.1 µ").ShouldBeTrue();
    }

    [Fact]
    public void Currency()
    {
        // Ported: the expectations use "$", i.e. the original assumed an en-US machine culture; pin it so the test
        // does not depend on the host (with LANG=C.UTF-8 the current culture is invariant and its symbol is "¤").
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            AssertCurrency();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static void AssertCurrency()
    {
        string result;

        result = Labelers.FormatCurrency(-1000000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$1 Q").ShouldBeTrue();
        result = Labelers.FormatCurrency(-100000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$100 T").ShouldBeTrue();
        result = Labelers.FormatCurrency(-10000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$10 T").ShouldBeTrue();
        result = Labelers.FormatCurrency(-1000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$1 T").ShouldBeTrue();
        result = Labelers.FormatCurrency(-100000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$100 B").ShouldBeTrue();
        result = Labelers.FormatCurrency(-10000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$10 B").ShouldBeTrue();
        result = Labelers.FormatCurrency(-1000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$1 B").ShouldBeTrue();
        result = Labelers.FormatCurrency(-100000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$100 M").ShouldBeTrue();
        result = Labelers.FormatCurrency(-10000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$10 M").ShouldBeTrue();
        result = Labelers.FormatCurrency(-1000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "-$1 M").ShouldBeTrue();
        result = Labelers.FormatCurrency(1000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$1 M").ShouldBeTrue();
        result = Labelers.FormatCurrency(10000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$10 M").ShouldBeTrue();
        result = Labelers.FormatCurrency(100000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$100 M").ShouldBeTrue();
        result = Labelers.FormatCurrency(1000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$1 B").ShouldBeTrue();
        result = Labelers.FormatCurrency(10000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$10 B").ShouldBeTrue();
        result = Labelers.FormatCurrency(100000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$100 B").ShouldBeTrue();
        result = Labelers.FormatCurrency(1000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$1 T").ShouldBeTrue();
        result = Labelers.FormatCurrency(10000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$10 T").ShouldBeTrue();
        result = Labelers.FormatCurrency(100000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$100 T").ShouldBeTrue();
        result = Labelers.FormatCurrency(1000000000000000, ",", ".", NumberFormatInfo.CurrentInfo.CurrencySymbol);
        (result == "$1 Q").ShouldBeTrue();
    }
}
