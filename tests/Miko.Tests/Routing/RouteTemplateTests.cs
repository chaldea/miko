using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

/// <summary>
/// ISSUE-164: route templates in Blazor's syntax — parsing, validation and matching.
/// </summary>
public class RouteTemplateTests
{
    // ---- Parsing ----

    [Fact]
    public void Should_Parse_Root_As_No_Segments()
    {
        RouteTemplate.Parse("/").Segments.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Parse_Literals_And_Parameters()
    {
        var segments = RouteTemplate.Parse("/user/{id:int}/{option:bool?}").Segments;

        segments.Count.ShouldBe(3);
        segments[0].IsLiteral.ShouldBeTrue();
        segments[0].Value.ShouldBe("user");

        segments[1].ParameterName.ShouldBe("id");
        segments[1].Constraint.ShouldBe("int");
        segments[1].IsOptional.ShouldBeFalse();

        segments[2].ParameterName.ShouldBe("option");
        segments[2].Constraint.ShouldBe("bool");
        segments[2].IsOptional.ShouldBeTrue();
    }

    [Fact]
    public void Should_Parse_Catch_All()
    {
        var segment = RouteTemplate.Parse("/catch-all/{*pageRoute}").Segments[1];

        segment.ParameterName.ShouldBe("pageRoute");
        segment.IsCatchAll.ShouldBeTrue();
        segment.IsOptional.ShouldBeFalse();
        segment.Constraint.ShouldBeNull();
    }

    [Fact]
    public void Should_Normalise_Constraint_Case()
    {
        RouteTemplate.Parse("/item/{id:Int}").Segments[1].Constraint.ShouldBe("int");
    }

    [Theory]
    [InlineData("user/{id}", "must start with '/'")]
    [InlineData("/files/{*path}/more", "last segment")]
    [InlineData("/files/{*path?}", "cannot be marked optional")]
    [InlineData("/files/{*path:int}", "cannot have a constraint")]
    [InlineData("/user/{id?}/edit", "cannot appear after optional")]
    [InlineData("/user/{id?}/{name}", "cannot appear after optional")]
    [InlineData("/user/{id}/{Id}", "appears more than one time")]
    [InlineData("/item/{id:custom}", "Unsupported constraint 'custom'")]
    [InlineData("/item/{id:int(3)}", "not a valid parameter segment")]
    [InlineData("/item-{id}", "not a valid parameter segment")]
    [InlineData("/item/{}", "not a valid parameter segment")]
    [InlineData("/item/{id", "not a valid parameter segment")]
    [InlineData("/item/id}", "not a valid parameter segment")]
    public void Should_Reject_Invalid_Templates(string template, string reason)
    {
        var error = Should.Throw<ArgumentException>(() => RouteTemplate.Parse(template));
        error.Message.ShouldContain(reason);
        error.Message.ShouldContain(template);
    }

    [Fact]
    public void Should_Allow_Several_Trailing_Optional_Parameters_And_A_Catch_All_After_Them()
    {
        Should.NotThrow(() => RouteTemplate.Parse("/a/{b?}/{c?}"));
        Should.NotThrow(() => RouteTemplate.Parse("/a/{b?}/{*rest}"));
    }

    // ---- Literals ----

    [Fact]
    public void Should_Match_Root_Only_At_Root()
    {
        var template = RouteTemplate.Parse("/");

        template.Match("/").ShouldNotBeNull().ShouldBeEmpty();
        template.Match("/other").ShouldBeNull();
    }

    [Fact]
    public void Should_Match_Literals_Case_Insensitively()
    {
        var template = RouteTemplate.Parse("/user/profile");

        template.Match("/user/profile").ShouldNotBeNull();
        template.Match("/User/PROFILE").ShouldNotBeNull();
        template.Match("/user/settings").ShouldBeNull();
        template.Match("/user").ShouldBeNull();
        template.Match("/user/profile/extra").ShouldBeNull();
    }

    [Theory]
    [InlineData("/user/profile/")]
    [InlineData("/user//profile")]
    [InlineData("/user/profile?tab=posts")]
    [InlineData("/user/profile#top")]
    public void Should_Ignore_Empty_Segments_Query_And_Fragment(string path)
    {
        RouteTemplate.Parse("/user/profile").Match(path).ShouldNotBeNull();
    }

    // ---- Parameters ----

    [Fact]
    public void Should_Capture_An_Unconstrained_Parameter_As_A_String()
    {
        var values = RouteTemplate.Parse("/user/{id}").Match("/user/123").ShouldNotBeNull();

        values["id"].ShouldBeOfType<string>().ShouldBe("123");
    }

    [Fact]
    public void Should_Look_Values_Up_By_Name_Case_Insensitively()
    {
        var values = RouteTemplate.Parse("/user/{Id}").Match("/user/7").ShouldNotBeNull();

        values["id"].ShouldBe("7");
        values["ID"].ShouldBe("7");
    }

    [Fact]
    public void Should_Url_Decode_Values()
    {
        var values = RouteTemplate.Parse("/search/{query}").Match("/search/hello%20world%2Fx").ShouldNotBeNull();

        values["query"].ShouldBe("hello world/x");
    }

    [Theory]
    [InlineData("int", "42", 42)]
    [InlineData("int", "-7", -7)]
    [InlineData("long", "9223372036854775807", long.MaxValue)]
    [InlineData("bool", "true", true)]
    [InlineData("bool", "False", false)]
    [InlineData("double", "19.99", 19.99)]
    [InlineData("float", "1.5", 1.5f)]
    public void Should_Convert_Constrained_Values(string constraint, string text, object expected)
    {
        var values = RouteTemplate.Parse($"/x/{{v:{constraint}}}").Match($"/x/{text}").ShouldNotBeNull();

        values["v"].ShouldBe(expected);
        values["v"]!.GetType().ShouldBe(expected.GetType());
    }

    [Fact]
    public void Should_Convert_Decimal_Guid_And_DateTime()
    {
        RouteTemplate.Parse("/x/{v:decimal}").Match("/x/12.50").ShouldNotBeNull()["v"].ShouldBe(12.50m);

        var guid = Guid.NewGuid();
        RouteTemplate.Parse("/x/{v:guid}").Match($"/x/{guid}").ShouldNotBeNull()["v"].ShouldBe(guid);

        RouteTemplate.Parse("/x/{v:datetime}").Match("/x/2026-11-09").ShouldNotBeNull()["v"]
            .ShouldBe(new DateTime(2026, 11, 9));
    }

    [Theory]
    [InlineData("int", "abc")]
    [InlineData("int", "2147483648")]
    [InlineData("long", "1.5")]
    [InlineData("bool", "yes")]
    [InlineData("guid", "not-a-guid")]
    [InlineData("double", "1.2.3")]
    public void Should_Not_Match_When_The_Constraint_Fails(string constraint, string text)
    {
        RouteTemplate.Parse($"/x/{{v:{constraint}}}").Match($"/x/{text}").ShouldBeNull();
    }

    [Fact]
    public void Should_Parse_Numbers_Independently_Of_The_Current_Culture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            // de-DE writes 19,99 and reads "19.99" as 1999: the URL must mean the same everywhere.
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            RouteTemplate.Parse("/price/{amount:double}").Match("/price/19.99").ShouldNotBeNull()["amount"].ShouldBe(19.99);
            RouteTemplate.Parse("/price/{amount:decimal}").Match("/price/19.99").ShouldNotBeNull()["amount"].ShouldBe(19.99m);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    // ---- Optional parameters ----

    [Fact]
    public void Should_Match_Without_An_Optional_Parameter_And_Leave_It_Out()
    {
        var values = RouteTemplate.Parse("/user/{id:int}/{option:bool?}").Match("/user/123").ShouldNotBeNull();

        values.Count.ShouldBe(1);
        values["id"].ShouldBe(123);
        values.ContainsKey("option").ShouldBeFalse();
    }

    [Fact]
    public void Should_Match_With_An_Optional_Parameter()
    {
        var values = RouteTemplate.Parse("/user/{id:int}/{option:bool?}").Match("/user/123/true").ShouldNotBeNull();

        values["id"].ShouldBe(123);
        values["option"].ShouldBe(true);
    }

    [Fact]
    public void Should_Not_Match_When_A_Present_Optional_Value_Fails_Its_Constraint()
    {
        // Blazor rejects the route rather than ignoring the segment.
        RouteTemplate.Parse("/user/{id:int}/{option:bool?}").Match("/user/123/maybe").ShouldBeNull();
    }

    [Fact]
    public void Should_Match_Several_Trailing_Optional_Parameters()
    {
        var template = RouteTemplate.Parse("/a/{b?}/{c?}");

        template.Match("/a").ShouldNotBeNull().ShouldBeEmpty();
        template.Match("/a/1").ShouldNotBeNull().Keys.Single().ShouldBe("b");
        template.Match("/a/1/2").ShouldNotBeNull().Count.ShouldBe(2);
        template.Match("/a/1/2/3").ShouldBeNull();
    }

    [Fact]
    public void Should_Match_An_Optional_Parameter_As_The_Only_Segment()
    {
        var template = RouteTemplate.Parse("/{page?}");

        template.Match("/").ShouldNotBeNull().ShouldBeEmpty();
        template.Match("/about").ShouldNotBeNull()["page"].ShouldBe("about");
    }

    // ---- Catch-all ----

    [Fact]
    public void Should_Capture_The_Rest_Of_The_Path_In_A_Catch_All()
    {
        var values = RouteTemplate.Parse("/catch-all/{*pageRoute}").Match("/catch-all/docs/2024/report.pdf").ShouldNotBeNull();

        values["pageRoute"].ShouldBe("docs/2024/report.pdf");
    }

    [Fact]
    public void Should_Match_A_Catch_All_With_No_Segments_And_Leave_It_Out()
    {
        // A catch-all matches zero or more segments, as in Blazor.
        var values = RouteTemplate.Parse("/catch-all/{*pageRoute}").Match("/catch-all").ShouldNotBeNull();

        values.ContainsKey("pageRoute").ShouldBeFalse();
    }

    [Fact]
    public void Should_Join_Decoded_Segments_In_A_Catch_All()
    {
        var values = RouteTemplate.Parse("/files/{*path}").Match("/files/my%20docs//a.txt?download=1").ShouldNotBeNull();

        values["path"].ShouldBe("my docs/a.txt");
    }

    [Fact]
    public void Should_Still_Require_The_Segments_Before_A_Catch_All()
    {
        RouteTemplate.Parse("/files/{owner}/{*path}").Match("/files").ShouldBeNull();
        RouteTemplate.Parse("/files/{*path}").Match("/other/x").ShouldBeNull();
    }
}
