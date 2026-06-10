using Miko.Common;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class ComputedStyleCursorTests
{
    [Fact]
    public void FromStyle_WithCursorSet_ResolvesCursor()
    {
        var style = new Style { Cursor = Cursor.Pointer };

        var computed = ComputedStyle.FromStyle(style);

        computed.Cursor.ShouldBe(Cursor.Pointer);
    }

    [Fact]
    public void FromStyle_WithoutCursor_DefaultsToDefault()
    {
        var computed = ComputedStyle.FromStyle(new Style());

        computed.Cursor.ShouldBe(Cursor.Default);
    }

    [Fact]
    public void FromStyle_NullStyle_DefaultsToDefault()
    {
        var computed = ComputedStyle.FromStyle(null);

        computed.Cursor.ShouldBe(Cursor.Default);
    }
}
