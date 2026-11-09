using System.Net.Sockets;
using Miko.Components.Vnc.Protocol;

namespace Miko.Components.Vnc;

/// <summary>The texts of <see cref="MikoVnc"/>. English by default; <see cref="Chinese"/> is built in.</summary>
public sealed record VncTexts
{
    // Connect form
    public string ConnectTitle { get; init; } = "Remote desktop";
    public string ConnectDescription { get; init; } = "Connect to a VNC server";
    public string Address { get; init; } = "Address";
    public string AddressPlaceholder { get; init; } = "host, host:display or host::port";
    public string Password { get; init; } = "Password";
    public string PasswordPlaceholder { get; init; } = "Asked for when needed";
    public string Connect { get; init; } = "Connect";
    public string InvalidAddress { get; init; } = "Enter an address such as 192.168.1.10 or desktop:1.";

    // Status
    public string Connecting { get; init; } = "Connecting to {0}…";
    public string Disconnected { get; init; } = "Disconnected";
    public string Reconnect { get; init; } = "Reconnect";
    public string Cancelled { get; init; } = "Connection cancelled";

    // Password dialog
    public string PasswordTitle { get; init; } = "Password required";
    public string PasswordPrompt { get; init; } = "{0} needs a password";
    public string WrongPassword { get; init; } = "The password is wrong. Try again.";
    public string Cancel { get; init; } = "Cancel";
    public string OK { get; init; } = "Connect";

    // Toolbar and settings
    public string Keyboard { get; init; } = "Keyboard";
    public string SpecialKeys { get; init; } = "Keys";
    public string ZoomIn { get; init; } = "Zoom in";
    public string ZoomOut { get; init; } = "Zoom out";
    public string FitToScreen { get; init; } = "Fit";
    public string Settings { get; init; } = "Settings";
    public string Fullscreen { get; init; } = "Fullscreen";
    public string Disconnect { get; init; } = "Disconnect";
    public string Quality { get; init; } = "Picture";
    public string Lossless { get; init; } = "Lossless";
    public string High { get; init; } = "High";
    public string Balanced { get; init; } = "Balanced";
    public string Low { get; init; } = "Low";
    public string Scaling { get; init; } = "Size";
    public string Fit { get; init; } = "Fit";
    public string ActualSize { get; init; } = "1:1";
    public string ViewOnly { get; init; } = "View only";
    public string On { get; init; } = "On";
    public string Off { get; init; } = "Off";
    public string Refresh { get; init; } = "Refresh";

    // Hints
    public string CtrlAltDelSent { get; init; } = "Sent Ctrl+Alt+Del";
    public string TouchHint { get; init; } = "Tap to click · hold for right click · hold and drag to drag";
    public string ViewOnlyHint { get; init; } = "View only: input is not sent";

    // Errors
    public string Unreachable { get; init; } = "Cannot reach {0}.";
    public string Timeout { get; init; } = "{0} did not answer.";
    public string ConnectionLost { get; init; } = "The connection was lost.";
    public string ServerClosed { get; init; } = "The server closed the connection.";
    public string Refused { get; init; } = "The server refused the connection: {0}";

    /// <summary>The texts in Simplified Chinese.</summary>
    public static VncTexts Chinese { get; } = new()
    {
        ConnectTitle = "远程桌面",
        ConnectDescription = "连接到 VNC 服务器",
        Address = "地址",
        AddressPlaceholder = "主机、主机:显示号 或 主机::端口",
        Password = "密码",
        PasswordPlaceholder = "需要时再询问",
        Connect = "连接",
        InvalidAddress = "请输入地址，例如 192.168.1.10 或 desktop:1",
        Connecting = "正在连接 {0}…",
        Disconnected = "连接已断开",
        Reconnect = "重新连接",
        Cancelled = "已取消连接",
        PasswordTitle = "需要密码",
        PasswordPrompt = "{0} 需要输入密码",
        WrongPassword = "密码错误，请重新输入",
        Cancel = "取消",
        OK = "连接",
        Keyboard = "键盘",
        SpecialKeys = "按键",
        ZoomIn = "放大",
        ZoomOut = "缩小",
        FitToScreen = "适应",
        Settings = "设置",
        Fullscreen = "全屏",
        Disconnect = "断开",
        Quality = "画质",
        Lossless = "无损",
        High = "高",
        Balanced = "均衡",
        Low = "省流",
        Scaling = "尺寸",
        Fit = "适应窗口",
        ActualSize = "原始大小",
        ViewOnly = "仅查看",
        On = "开",
        Off = "关",
        Refresh = "刷新画面",
        CtrlAltDelSent = "已发送 Ctrl+Alt+Del",
        TouchHint = "轻点为单击 · 长按为右键 · 长按后拖动为拖拽",
        ViewOnlyHint = "仅查看模式：不发送输入",
        Unreachable = "无法连接到 {0}",
        Timeout = "{0} 没有响应",
        ConnectionLost = "连接中断",
        ServerClosed = "服务器关闭了连接",
        Refused = "服务器拒绝连接：{0}",
    };

    /// <summary>The text shown for a failure.</summary>
    public string Describe(Exception error, string server) => error switch
    {
        VncAuthenticationException { PasswordRejected: true } => WrongPassword,
        VncAuthenticationException auth => string.Format(Refused, auth.Message),
        VncException { InnerException: SocketException } => string.Format(Unreachable, server),
        VncException { InnerException: TimeoutException } or VncException { InnerException: IOException { InnerException: SocketException { SocketErrorCode: SocketError.TimedOut } } }
            => string.Format(Timeout, server),
        VncException { InnerException: EndOfStreamException } or EndOfStreamException => ServerClosed,
        IOException or SocketException => ConnectionLost,
        _ => error.Message,
    };
}
