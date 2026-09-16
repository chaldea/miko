using Miko.Native.Clipboard;
using Miko.Windowing.Native;
using Shouldly;

namespace Miko.Native.Tests.Desktop;

/// <summary>
/// 桌面剪贴板的真实读写。
/// <para>
/// 独立成一个禁用并行的 collection：Windows 剪贴板是**全进程独占**资源，
/// 并行跑的另一个测试打开剪贴板时，本测试的 <c>OpenClipboard</c> 就会失败。
/// 实现侧已加重试来应对外部进程的抢占，但同进程内的测试没必要互相踩——直接串行。
/// </para>
/// </summary>
[Collection(nameof(DesktopClipboardCollection))]
public class DesktopClipboardServiceTests
{
    [Fact]
    public async Task Should_round_trip_text_on_windows()
    {
        var clipboard = new DesktopClipboardService();
        var text = $"miko-clipboard-{Guid.NewGuid():n}";

        if (!System.OperatingSystem.IsWindows())
        {
            // 非 Windows 桌面没有免依赖的剪贴板栈，按 issue 要求明确抛出而不是返回空串。
            await Should.ThrowAsync<PlatformNotSupportedException>(
                () => clipboard.WriteAsync(new ClipboardWriteOptions { String = text }));
            return;
        }

        await clipboard.WriteAsync(new ClipboardWriteOptions { String = text });
        var read = await clipboard.ReadAsync();

        read.Value.ShouldBe(text);
        read.Type.ShouldBe("text/plain");
    }

    [Fact]
    public async Task Should_write_the_url_when_no_string_is_given()
    {
        if (!System.OperatingSystem.IsWindows()) return;

        var clipboard = new DesktopClipboardService();
        var url = $"https://example.com/{Guid.NewGuid():n}";

        await clipboard.WriteAsync(new ClipboardWriteOptions { Url = url });

        (await clipboard.ReadAsync()).Value.ShouldBe(url);
    }

    [Fact]
    public async Task Should_reject_images_and_empty_payloads()
    {
        if (!System.OperatingSystem.IsWindows()) return;

        var clipboard = new DesktopClipboardService();

        // 图片需要 DIB 编解码，未实现即明确抛出。
        await Should.ThrowAsync<PlatformNotSupportedException>(
            () => clipboard.WriteAsync(new ClipboardWriteOptions { Image = "data:image/png;base64,AA==" }));

        await Should.ThrowAsync<ArgumentException>(
            () => clipboard.WriteAsync(new ClipboardWriteOptions()));
    }
}

/// <summary>剪贴板测试的串行 collection 定义（剪贴板是全进程独占资源）。</summary>
[CollectionDefinition(nameof(DesktopClipboardCollection), DisableParallelization = true)]
public sealed class DesktopClipboardCollection;
