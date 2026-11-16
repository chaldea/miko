using Miko.Components.Webcam;
using Miko.Components.Webcam.Mock;

namespace Miko.Docs.Demo;

/// <summary>A provider snapshot for each mounted camera; reconnecting creates a new panorama view.</summary>
public sealed class MockMediaConnection
{
    public IWebcamProvider Provider { get; private set; } = new MockWebcamProvider("http://localhost:8080/media/webcam.m3u8");
    public WebcamDevice Connect(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length != 0)
            throw new ArgumentException("Enter an HTTP(S) HLS or MP4 URL.");
        Provider = new MockWebcamProvider(uri.AbsoluteUri);
        return new WebcamDevice("mock") { Name = "Miko panorama" };
    }
}
