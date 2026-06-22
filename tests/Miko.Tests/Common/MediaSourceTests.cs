using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

/// <summary>
/// <see cref="MediaSource"/> 的协议解析与隐式字符串转换。纯解析，无 I/O。
/// </summary>
public class MediaSourceTests
{
    [Fact]
    public void Parse_Null_IsEmpty()
    {
        var s = MediaSource.Parse(null);
        s.IsEmpty.ShouldBeTrue();
        s.Scheme.ShouldBe(MediaSourceScheme.None);
    }

    [Fact]
    public void Parse_EmptyString_IsEmpty()
    {
        MediaSource.Parse("").IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Parse_BarePath_IsFile()
    {
        var s = MediaSource.Parse("movie.mp4");
        s.Scheme.ShouldBe(MediaSourceScheme.File);
        s.Value.ShouldBe("movie.mp4");
        s.IsNetwork.ShouldBeFalse();
    }

    [Fact]
    public void Parse_FileScheme_StripsPrefix()
    {
        MediaSource.Parse("file://assets/a.png").Value.ShouldBe("assets/a.png");
    }

    [Fact]
    public void Parse_FileScheme_TripleSlashWindowsDrive_StripsLeadingSlash()
    {
        var s = MediaSource.Parse("file:///C:/imgs/a.png");
        s.Scheme.ShouldBe(MediaSourceScheme.File);
        s.Value.ShouldBe("C:/imgs/a.png");
    }

    [Fact]
    public void Parse_ResScheme()
    {
        var s = MediaSource.Parse("res://Assets/spinner.png");
        s.Scheme.ShouldBe(MediaSourceScheme.Resource);
        s.Value.ShouldBe("Assets/spinner.png");
    }

    [Fact]
    public void Parse_Http_KeepsFullUrl()
    {
        var s = MediaSource.Parse("http://localhost:5050/a.png");
        s.Scheme.ShouldBe(MediaSourceScheme.Http);
        s.Value.ShouldBe("http://localhost:5050/a.png");
        s.IsNetwork.ShouldBeTrue();
    }

    [Fact]
    public void Parse_Https_KeepsFullUrl()
    {
        var s = MediaSource.Parse("https://example.com/a.png");
        s.Scheme.ShouldBe(MediaSourceScheme.Https);
        s.Value.ShouldBe("https://example.com/a.png");
        s.IsNetwork.ShouldBeTrue();
    }

    [Fact]
    public void Parse_DataUri()
    {
        var s = MediaSource.Parse("data:image/png;base64,AAAA");
        s.Scheme.ShouldBe(MediaSourceScheme.Data);
        s.Value.ShouldBe("data:image/png;base64,AAAA");
    }

    [Fact]
    public void Parse_SchemeIsCaseInsensitive()
    {
        MediaSource.Parse("HTTPS://example.com/a.png").Scheme.ShouldBe(MediaSourceScheme.Https);
        MediaSource.Parse("Res://a.png").Scheme.ShouldBe(MediaSourceScheme.Resource);
    }

    [Fact]
    public void ImplicitStringConversion_ParsesScheme()
    {
        MediaSource s = "res://a.png";
        s.Scheme.ShouldBe(MediaSourceScheme.Resource);
    }

    [Fact]
    public void ImplicitNullConversion_IsEmpty()
    {
        MediaSource s = (string?)null;
        s.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void ToString_ReturnsRaw()
    {
        MediaSource s = "res://Assets/a.png";
        s.ToString().ShouldBe("res://Assets/a.png");
    }

    [Fact]
    public void ToUri_NetworkAndFile_RoundTrip()
    {
        ((MediaSource)"http://x/a.png").ToUri().ShouldBe("http://x/a.png");
        ((MediaSource)"movie.mp4").ToUri().ShouldBe("movie.mp4");
    }

    [Fact]
    public void Empty_Sentinel_IsEmpty()
    {
        MediaSource.Empty.IsEmpty.ShouldBeTrue();
        default(MediaSource).IsEmpty.ShouldBeTrue();
    }
}
