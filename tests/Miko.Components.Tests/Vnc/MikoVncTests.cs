using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components.Vnc;
using Miko.Components.Vnc.Protocol;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Styling;
using Shouldly;

namespace Miko.Components.Tests.Vnc;

/// <summary>
/// The viewer against the scripted server, driven through the real interaction controller (hit testing,
/// focus, key dispatch). The remote screen is 640 × 400, shown at 2× in a 1280 × 800 view: view (x, y) is
/// remote (x / 2, y / 2).
/// </summary>
public sealed class MikoVncTests : IDisposable
{
    private readonly FakeVncServer _server = new(640, 400);
    private readonly ManualClock _clock = new();
    private readonly RecordingSystemBars _systemBars = new();
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    private MikoEngine _engine = null!;
    private RenderSurface _surface = null!;
    private MikoInteractionController _input = null!;
    private PlatformInfo _platform = null!;
    private MikoVnc _vnc = null!;
    private int _width, _height;

    public MikoVncTests() => Create(HostPlatform.MacOS, 1280, 800);

    private void Create(HostPlatform platform, int width, int height, Action<MikoVnc>? configure = null)
    {
        _width = width;
        _height = height;
        _engine = new MikoEngineBuilder().Build();
        _surface = _engine.Graphics.CreateSurface(width, height);
        _platform = new PlatformInfo(platform) { SystemBars = _systemBars };
        _input = new MikoInteractionController(
            Options.Create(new MikoAppOptions()), _services, _engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance,
            platformService: _platform);
        _vnc = new MikoVnc
        {
            Host = "127.0.0.1", Port = _server.Port, Password = "secret", Engine = _engine, Input = _input,
            PlatformService = _platform, Clock = _clock, Texts = VncTexts.Chinese,
        };
        configure?.Invoke(_vnc);
        var root = new DivElement { Style = new Style { Width = Length.Percent(100), Height = Length.Percent(100) } };
        root.Children.Add(_vnc.Build());
        _engine.Initialize(root, [], _surface.Context, width, height);
        Render();
    }

    private void Recreate(HostPlatform platform, int width, int height, Action<MikoVnc>? configure = null)
    {
        _vnc.Dispose();
        _surface.Dispose();
        _server.Drop();
        Create(platform, width, height, configure);
    }

    public void Dispose()
    {
        _vnc.Dispose();
        _surface.Dispose();
        _server.Dispose();
        _services.Dispose();
    }

    private void Render() => _engine.Render(_surface.Context);

    private void WaitFor(Func<bool> condition, string what, int ms = 5000)
    {
        var until = Environment.TickCount64 + ms;
        while (!condition())
        {
            if (Environment.TickCount64 > until) throw new TimeoutException($"timed out waiting for {what} (state {_vnc.State}, {_vnc.ErrorMessage})");
            Render();
            Thread.Sleep(5);
        }
        Render();
    }

    private void Connected()
    {
        WaitFor(() => _vnc.State == VncState.Connected, "the session");
        _server.Next(m => m.Kind == "Update" && !m.Incremental).ShouldNotBeNull();
        _server.Drain(50);
    }

    // A red remote screen with a blue square at remote (100, 50) – (139, 69).
    private void PaintRemote()
    {
        long before = _vnc.Client!.UpdateCount;
        _server.Update(
            (0, 0, 640, 400, VncEncoding.Rre, Rre(Pixel(200, 0, 0), (100, 50, 40, 20, Pixel(0, 0, 255)))));
        WaitFor(() => _vnc.Client!.UpdateCount > before, "the update");
        for (int i = 0; i < 3; i++) Render();
    }

    private static byte[] Pixel(byte r, byte g, byte b) => FakeVncServer.Pixel(r, g, b);

    private static byte[] Rre(byte[] background, params (int X, int Y, int W, int H, byte[] Pixel)[] rects)
    {
        var ms = new MemoryStream();
        ms.Write([0, 0, 0, (byte)rects.Length]);
        ms.Write(background);
        foreach (var r in rects)
        {
            ms.Write(r.Pixel);
            ms.Write([(byte)(r.X >> 8), (byte)r.X, (byte)(r.Y >> 8), (byte)r.Y, (byte)(r.W >> 8), (byte)r.W, (byte)(r.H >> 8), (byte)r.H]);
        }
        return ms.ToArray();
    }

    private Element Single(string className) => _engine.GetRoot()!.FindByClass(className).Single();

    private LayoutBox? LayoutOf(Element element)
    {
        static LayoutBox? Find(LayoutBox box, Element element)
            => box.Element == element ? box : box.Children.Select(c => Find(c, element)).FirstOrDefault(b => b != null);
        return Find(_engine.GetCurrentLayout()!, element);
    }

    private bool IsShown(string className) => LayoutOf(Single(className)) is { } box && box.ComputedStyle.Display != Display.None;

    private void Click(string className)
    {
        var box = LayoutOf(Single(className))!.BoxModel.BorderBox;
        Click(box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    private void Click(float x, float y, MouseButton button = MouseButton.Left, PointerType type = PointerType.Mouse)
    {
        _input.OnPointerDown(x, y, button, type);
        _input.OnPointerUp(x, y, button, type);
        Render();
    }

    private List<ClientMessage> Sent(string kind) => _server.Drain(250).Where(m => m.Kind == kind).ToList();

    // ---- session -------------------------------------------------------------------

    [Fact]
    public void Should_Connect_And_Paint_The_Remote_Screen_Fitted()
    {
        Connected();
        _vnc.Desktop!.Name.ShouldBe("fake-desktop");
        (_vnc.Desktop.Width, _vnc.Desktop.Height).ShouldBe((640, 400));
        PaintRemote();

        // 2×: remote (120, 60) is view (240, 120).
        var blue = _surface.GetPixel(240, 120);
        (blue.R, blue.G, blue.B).ShouldBe(((byte)0, (byte)0, (byte)255));
        var red = _surface.GetPixel(600, 500);
        (red.R, red.G, red.B).ShouldBe(((byte)200, (byte)0, (byte)0));
        IsShown("miko-vnc-toolbar").ShouldBeTrue();
        IsShown("miko-vnc-status").ShouldBeFalse();
    }

    [Fact]
    public void Should_Show_The_Reason_And_Reconnect_When_The_Server_Drops()
    {
        Connected();
        _server.Drop();
        WaitFor(() => _vnc.State == VncState.Disconnected, "the drop");
        _vnc.ErrorMessage.ShouldBe(VncTexts.Chinese.ServerClosed);
        IsShown("miko-vnc-status").ShouldBeTrue();
        ((ButtonElement)Single("miko-vnc-status-button")).TextContent.ShouldBe("重新连接");

        Click("miko-vnc-status-button");
        WaitFor(() => _vnc.State == VncState.Connected, "the reconnect");
        _server.Connections.ShouldBe(2);
    }

    // ---- mouse ---------------------------------------------------------------------

    [Fact]
    public void Should_Map_Mouse_Buttons_And_Position_To_The_Remote_Screen()
    {
        Connected();
        PaintRemote();
        _input.OnPointerMove(400, 300);
        Click(400, 300);
        Sent("Pointer").ShouldBe([
            new ClientMessage("Pointer", Buttons: 0, X: 200, Y: 150),
            new ClientMessage("Pointer", Buttons: 1, X: 200, Y: 150),
            new ClientMessage("Pointer", Buttons: 0, X: 200, Y: 150),
        ]);

        // Right and middle buttons reach the screen too (no click, no menu of our own).
        Click(100, 60, MouseButton.Right);
        Click(102, 62, MouseButton.Middle);
        Sent("Pointer").ShouldBe([
            new ClientMessage("Pointer", Buttons: 4, X: 50, Y: 30),
            new ClientMessage("Pointer", Buttons: 0, X: 50, Y: 30),
            new ClientMessage("Pointer", Buttons: 2, X: 51, Y: 31),
            new ClientMessage("Pointer", Buttons: 0, X: 51, Y: 31),
        ]);
    }

    [Fact]
    public void Should_Turn_Wheel_Notches_Into_Button_4_And_5_Clicks()
    {
        Connected();
        _input.OnPointerMove(200, 200);
        _server.Drain(100);
        _input.OnScroll(200, 200, 0, 80);    // two notches down
        _input.OnScroll(200, 200, 0, -20);   // half a notch up: waits for more
        _input.OnScroll(200, 200, 0, -20);
        Sent("Pointer").Select(p => p.Buttons).ShouldBe([16, 0, 16, 0, 8, 0]);
    }

    [Fact]
    public void Should_Draw_The_Remote_Pointer_And_Hide_The_System_One()
    {
        Connected();
        var shape = FakeVncServer.RawPixels(4, 4, (_, _) => (0, 255, 0));
        _server.Update((0, 0, 4, 4, VncEncoding.Cursor, [.. shape, 0xF0, 0xF0, 0xF0, 0xF0]));
        WaitFor(() => LayoutOf(Single("miko-vnc-screen"))!.ComputedStyle.Cursor == Cursor.None, "cursor: none");
        _input.OnPointerMove(600, 600);
        Render();
        var green = _surface.GetPixel(603, 603);
        (green.R, green.G, green.B).ShouldBe(((byte)0, (byte)255, (byte)0));
    }

    // ---- keyboard ------------------------------------------------------------------

    [Fact]
    public void Should_Send_Typed_Text_And_Named_Keys_After_A_Click_On_The_Screen()
    {
        Connected();
        Click(400, 300);
        _vnc.IsKeyboardActive.ShouldBeTrue("a click gives the remote screen the keyboard");
        _server.Drain(100);

        _input.OnKeyDown(MikoKey.A, MikoKeyModifiers.None);     // its text arrives separately
        _input.OnTextInput("a");
        _input.OnKeyUp(MikoKey.A, MikoKeyModifiers.None);
        _input.OnTextInput("中");
        _input.OnKeyDown(MikoKey.Enter, MikoKeyModifiers.None);
        _input.OnKeyUp(MikoKey.Enter, MikoKeyModifiers.None);
        _input.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);
        _input.OnKeyUp(MikoKey.Backspace, MikoKeyModifiers.None);

        Sent("Key").Select(k => (k.Down, k.Keysym)).ShouldBe([
            (true, 0x61u), (false, 0x61u),
            (true, 0x01004E2Du), (false, 0x01004E2Du),
            (true, 0xFF0Du), (false, 0xFF0Du),
            (true, 0xFF08u), (false, 0xFF08u),
        ]);
    }

    [Fact]
    public void Should_Send_Shortcuts_By_Key_With_The_Modifier_Held()
    {
        Connected();
        Click(400, 300);
        _server.Drain(100);

        _input.OnKeyDown(MikoKey.ControlLeft, MikoKeyModifiers.Control);
        _input.OnKeyDown(MikoKey.C, MikoKeyModifiers.Control);
        _input.OnTextInput("c");   // a platform that still delivers the text must not type it twice
        _input.OnKeyUp(MikoKey.C, MikoKeyModifiers.Control);
        _input.OnKeyUp(MikoKey.ControlLeft, MikoKeyModifiers.None);

        Sent("Key").Select(k => (k.Down, k.Keysym)).ShouldBe([
            (true, 0xFFE3u), (true, 0x63u), (false, 0x63u), (false, 0xFFE3u),
        ]);
    }

    [Fact]
    public void Should_Release_Held_Keys_When_The_Screen_Loses_Focus()
    {
        Connected();
        Click(400, 300);
        _server.Drain(100);
        _input.OnKeyDown(MikoKey.ShiftLeft, MikoKeyModifiers.Shift);
        _input.SetFocus(null);
        Sent("Key").Select(k => (k.Down, k.Keysym)).ShouldBe([(true, 0xFFE1u), (false, 0xFFE1u)]);
        _vnc.IsKeyboardActive.ShouldBeFalse();
    }

    [Fact]
    public void Should_Latch_Modifiers_From_The_Keys_Bar_For_One_Key()
    {
        Connected();
        Click("miko-vnc-keys-button");
        IsShown("miko-vnc-keys").ShouldBeTrue();
        _server.Drain(100);
        ClickKey("Ctrl");
        ClickKey("Alt");
        _input.SetFocus(Single("miko-vnc-screen"));
        _input.OnTextInput("t");
        ClickKey("Esc");

        Sent("Key").Select(k => (k.Down, k.Keysym)).ShouldBe([
            (true, 0xFFE3u), (true, 0xFFE9u),           // latched
            (true, 0x74u), (false, 0x74u),              // Ctrl+Alt+t
            (false, 0xFFE3u), (false, 0xFFE9u),         // released after one key
            (true, 0xFF1Bu), (false, 0xFF1Bu),
        ]);
    }

    private void ClickKey(string label)
    {
        var key = _engine.GetRoot()!.FindByClass("miko-vnc-key").Single(k => k.TextContent == label);
        var box = LayoutOf(key)!.BoxModel.BorderBox;
        Click(box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    [Fact]
    public void Should_Send_Ctrl_Alt_Del_In_Order()
    {
        Connected();
        _server.Drain(100);
        _vnc.SendCtrlAltDel();
        Sent("Key").Select(k => (k.Down, k.Keysym)).ShouldBe([
            (true, 0xFFE3u), (true, 0xFFE9u), (true, 0xFFFFu), (false, 0xFFFFu), (false, 0xFFE9u), (false, 0xFFE3u),
        ]);
        _vnc.Hint.ShouldBe("已发送 Ctrl+Alt+Del");
    }

    [Fact]
    public void Should_Send_Nothing_In_View_Only_Mode()
    {
        Connected();
        _vnc.SetViewOnly(true);
        _server.Drain(100);
        Click(400, 300);
        _input.OnTextInput("x");
        _vnc.SendCtrlAltDel();
        _server.Drain(250).Where(m => m.Kind is "Key" or "Pointer").ShouldBeEmpty();
    }

    // ---- password ------------------------------------------------------------------

    [Fact]
    public void Should_Ask_For_The_Password_And_Ask_Again_When_It_Is_Wrong()
    {
        Recreate(HostPlatform.MacOS, 1280, 800, v => v.Password = null);
        WaitFor(() => _vnc.IsPasswordDialogOpen, "the dialog");
        int connections = _server.Connections;
        _vnc.State.ShouldBe(VncState.Authenticating);
        IsShown("miko-vnc-password").ShouldBeTrue();

        TypePassword("wrong");
        WaitFor(() => _vnc.IsPasswordDialogOpen, "the second prompt");
        ((SpanElement)Single("miko-vnc-password-error")).TextContent.ShouldBe("密码错误，请重新输入");

        TypePassword("secret");
        WaitFor(() => _vnc.State == VncState.Connected, "the session");
        _server.Connections.ShouldBe(connections + 1, "a refused password ends the connection: the retry opens a new one");
        IsShown("miko-vnc-password").ShouldBeFalse();
    }

    private void TypePassword(string password)
    {
        Click("miko-vnc-password-input");
        _input.OnTextInput(password);
        _input.OnKeyDown(MikoKey.Enter, MikoKeyModifiers.None);
        Render();
    }

    [Fact]
    public void Should_Stop_When_The_Password_Dialog_Is_Cancelled()
    {
        Recreate(HostPlatform.MacOS, 1280, 800, v => v.Password = null);
        WaitFor(() => _vnc.IsPasswordDialogOpen, "the dialog");
        Click("miko-vnc-password-cancel");
        WaitFor(() => _vnc.State == VncState.Disconnected, "cancelled");
        _vnc.ErrorMessage.ShouldBe("已取消连接");
    }

    // ---- connect form --------------------------------------------------------------

    [Fact]
    public void Should_Connect_From_Its_Form_Without_A_Host()
    {
        Recreate(HostPlatform.MacOS, 1280, 800, v => v.Host = null);
        _vnc.State.ShouldBe(VncState.Idle);
        IsShown("miko-vnc-connect").ShouldBeTrue();

        Click("miko-vnc-address");
        _input.OnTextInput("nowhere:bad");
        Click("miko-vnc-connect-button");
        ((SpanElement)Single("miko-vnc-form-error")).TextContent.ShouldBe(VncTexts.Chinese.InvalidAddress);

        ((InputElement)Single("miko-vnc-address")).Value = $"127.0.0.1::{_server.Port}";
        Click("miko-vnc-form-password");
        _input.OnTextInput("secret");
        Click("miko-vnc-connect-button");
        WaitFor(() => _vnc.State == VncState.Connected, "the session");
        IsShown("miko-vnc-connect").ShouldBeFalse();

        // Disconnecting goes back to the form, the address kept.
        Click("miko-vnc-disconnect");
        _vnc.State.ShouldBe(VncState.Idle);
        IsShown("miko-vnc-connect").ShouldBeTrue();
        ((InputElement)Single("miko-vnc-address")).Value.ShouldBe($"127.0.0.1::{_server.Port}");
    }

    // ---- toolbar -------------------------------------------------------------------

    [Fact]
    public void Should_Zoom_From_The_Toolbar_And_Collapse_It()
    {
        Connected();
        Click("miko-vnc-zoom-in");
        _vnc.Zoom.ShouldBe(1.5f);
        Click("miko-vnc-zoom-in");
        _vnc.Zoom.ShouldBe(2f);
        Click("miko-vnc-zoom-out");
        _vnc.Zoom.ShouldBe(1.5f);

        Click("miko-vnc-collapse");
        IsShown("miko-vnc-toolbar").ShouldBeFalse();
        IsShown("miko-vnc-handle").ShouldBeTrue();
        Click("miko-vnc-handle");
        IsShown("miko-vnc-toolbar").ShouldBeTrue();
    }

    [Fact]
    public void Should_Change_Quality_During_The_Session()
    {
        Connected();
        Click("miko-vnc-settings-button");
        _vnc.IsSettingsOpen.ShouldBeTrue();
        var chip = _engine.GetRoot()!.FindByClass("miko-vnc-chip").First(c => c.TextContent == "无损");
        var box = LayoutOf(chip)!.BoxModel.BorderBox;
        Click(box.X + box.Width / 2, box.Y + box.Height / 2);
        _vnc.CurrentQuality.ShouldBe(VncQuality.Lossless);
        var encodings = _server.Next(m => m.Kind == "SetEncodings")!.Encodings!;
        encodings.ShouldNotContain(e => e >= -32 && e <= -23, "lossless: no JPEG quality level");
        encodings[1].ShouldBe(VncEncoding.Zrle);
    }

    [Fact]
    public void Should_Leave_Escape_To_The_Remote_Side_In_Desktop_Fullscreen()
    {
        Connected();
        _vnc.ToggleFullscreen();
        _vnc.IsFullscreen.ShouldBeTrue();
        _systemBars.HiddenDepth.ShouldBe(1);
        Click(400, 300);
        _server.Drain(100);
        _input.OnKeyDown(MikoKey.Escape, MikoKeyModifiers.None);
        _vnc.IsFullscreen.ShouldBeTrue("Escape is a key for the remote desktop, not a way out");
        Sent("Key").ShouldContain(k => k.Down && k.Keysym == 0xFF1B);
        _vnc.ToggleFullscreen();
        _systemBars.HiddenDepth.ShouldBe(0);
    }

    // ---- touch ---------------------------------------------------------------------

    [Fact]
    public void Touch_Should_Tap_To_Click_And_Hold_To_Right_Click()
    {
        Recreate(HostPlatform.Android, 800, 500);
        Connected();
        // 800 × 500 view, 640 × 400 screen: fitted at 1.25×.
        Click(250, 125, type: PointerType.Touch);
        Sent("Pointer").ShouldBe([
            new ClientMessage("Pointer", Buttons: 0, X: 200, Y: 100),
            new ClientMessage("Pointer", Buttons: 1, X: 200, Y: 100),
            new ClientMessage("Pointer", Buttons: 0, X: 200, Y: 100),
        ]);

        _input.OnPointerDown(500, 250, MouseButton.Left, PointerType.Touch);
        _clock.Advance(TimeSpan.FromMilliseconds(600));
        Render();
        _input.OnPointerUp(500, 250, MouseButton.Left, PointerType.Touch);
        Render();
        Sent("Pointer").Select(p => (p.Buttons, p.X, p.Y)).ShouldBe([(0, 400, 200), (4, 400, 200), (0, 400, 200)]);
    }

    [Fact]
    public void Touch_Should_Drag_With_The_Left_Button_After_A_Hold_And_Scroll_Otherwise()
    {
        Recreate(HostPlatform.Android, 800, 500);
        Connected();
        _input.OnPointerDown(100, 100, MouseButton.Left, PointerType.Touch);
        _clock.Advance(TimeSpan.FromMilliseconds(600));
        Render();
        _input.OnPointerMove(150, 100);
        _input.OnPointerMove(200, 100);
        _input.OnPointerUp(200, 100, MouseButton.Left, PointerType.Touch);
        Render();
        var drag = Sent("Pointer");
        drag.First().ShouldBe(new ClientMessage("Pointer", Buttons: 1, X: 80, Y: 80));
        drag.Last().ShouldBe(new ClientMessage("Pointer", Buttons: 0, X: 160, Y: 80));

        // No hold: a drag over a picture that fits scrolls (finger up = wheel down).
        _input.OnPointerDown(300, 400, MouseButton.Left, PointerType.Touch);
        _input.OnPointerMove(300, 380);
        _input.OnPointerMove(300, 340);
        _input.OnPointerUp(300, 340, MouseButton.Left, PointerType.Touch);
        Render();
        Sent("Pointer").Where(p => p.Buttons != 0).Select(p => p.Buttons).ShouldAllBe(b => b == 16);
    }

    [Fact]
    public void Touch_Keyboard_Button_Should_Bring_Up_The_Soft_Keyboard_And_The_Keys_Bar()
    {
        Recreate(HostPlatform.Android, 800, 500);
        Connected();
        IsShown("miko-vnc-keyboard").ShouldBeTrue();
        IsShown("miko-vnc-keys").ShouldBeFalse();
        Click("miko-vnc-keyboard");
        _vnc.IsKeyboardActive.ShouldBeTrue();
        IsShown("miko-vnc-keys").ShouldBeTrue("Esc, Tab, Ctrl… come with the soft keyboard");

        // Tapping the picture keeps the keyboard (the press does not take focus away).
        Click(300, 300, type: PointerType.Touch);
        _vnc.IsKeyboardActive.ShouldBeTrue();
        _server.Drain(100);
        _input.OnTextInput("ok\n");
        Sent("Key").Where(k => k.Down).Select(k => k.Keysym).ShouldBe([0x6Fu, 0x6Bu, 0xFF0Du]);
    }

    private sealed class RecordingSystemBars : ISystemBarsService
    {
        public int HiddenDepth { get; private set; }
        public void Hide() => HiddenDepth++;
        public void Show() => HiddenDepth = Math.Max(0, HiddenDepth - 1);
    }
}
