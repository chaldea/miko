using Miko.Core;
using Miko.Ionic.Tests.Fixtures;
using Miko.Ionic.Tests.Fixtures.Routed;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// <see cref="Miko.Ionic.Components.IonTextarea.AutoGrow"/> (P0-C1): the field grows a line at a
/// time as the text needs it and shrinks back — the chat / comment box. It used to only stamp a
/// class. Typed through the interaction controller into a textarea inside an item.
/// </summary>
public sealed class IonTextareaAutoGrowTests : IDisposable
{
    private readonly RoutedApp _app = new(("/", typeof(TextareaPageFixture)));

    public void Dispose() => _app.Dispose();

    private Element Native(string textarea) => _app.Single(textarea).FindByClass("native-textarea").Single();

    private float HeightOf(string textarea) => _app.BorderBoxOf(_app.Single(textarea)).Height;

    private void Type(string textarea, string text)
    {
        _app.Tap(Native(textarea));
        _app.App.Controller.OnTextInput(text);
        _app.Render();
    }

    [Fact]
    public void Should_grow_a_line_for_each_new_line_of_text()
    {
        var empty = HeightOf("growing");
        float Field() => _app.BorderBoxOf(Native("growing")).Height;
        var emptyField = Field();

        Type("growing", "first");
        var oneLine = Field();
        _app.App.Controller.OnTextInput("\nsecond");
        _app.Render();
        var twoLines = Field();
        _app.App.Controller.OnTextInput("\nthird");
        _app.Render();
        var threeLines = Field();

        oneLine.ShouldBe(emptyField, 0.5f, "one line fits the single row");
        twoLines.ShouldBeGreaterThan(oneLine + 1);
        (threeLines - twoLines).ShouldBe(twoLines - oneLine, 0.5f, "every line adds one line height");
        HeightOf("growing").ShouldBeGreaterThan(empty + 1, "the item grows with the field");
    }

    [Fact]
    public void Should_grow_when_a_long_line_wraps()
    {
        var empty = HeightOf("growing");

        Type("growing", string.Join(' ', Enumerable.Repeat("wrapping", 40)));

        HeightOf("growing").ShouldBeGreaterThan(empty + 1);
    }

    [Fact]
    public void Should_shrink_back_when_the_text_is_deleted()
    {
        var empty = HeightOf("growing");
        Type("growing", "first\nsecond");
        HeightOf("growing").ShouldBeGreaterThan(empty + 1);

        for (var i = 0; i < "\nsecond".Length; i++)
            _app.App.Controller.OnKeyDown(Miko.Platform.MikoKey.Backspace, Miko.Platform.MikoKeyModifiers.None);
        _app.Render();

        HeightOf("growing").ShouldBe(empty, 0.5f);
    }

    /// <summary>
    /// The field is as tall as its rows. Before P0-C1 it collapsed to its padding in an item: the
    /// stylesheet capped it at <c>max-height: 100%</c> of wrappers with no definite height, so typed
    /// text was clipped away and neither <c>Rows</c> nor <c>AutoGrow</c> could show.
    /// </summary>
    [Fact]
    public void Should_be_as_tall_as_its_rows()
    {
        var one = _app.BorderBoxOf(Native("fixed")).Height;
        var three = _app.BorderBoxOf(Native("three-rows")).Height;

        one.ShouldBeGreaterThan(12f, "one row holds a line of text");
        three.ShouldBeGreaterThan(one * 2);
    }

    [Fact]
    public void Should_keep_its_rows_without_auto_grow()
    {
        var empty = HeightOf("fixed");

        Type("fixed", "first\nsecond\nthird");

        HeightOf("fixed").ShouldBe(empty, 0.5f);
    }
}
