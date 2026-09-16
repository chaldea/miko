using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Miko.Native;
using Miko.Native.Clipboard;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面剪贴板。Windows 走 Win32 剪贴板 API；Linux/macOS 没有无依赖的系统栈
/// （需要 X11/Wayland 或 AppKit 绑定），按要求抛 <see cref="PlatformNotSupportedException"/>
/// 而不是静默返回空串。
/// <para>
/// 当前仅支持文本（<c>CF_UNICODETEXT</c>）。图片 Data URL 需要 DIB 编解码，未实现即抛。
/// </para>
/// </summary>
internal sealed class DesktopClipboardService : NativeServiceBase, IClipboardService
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    public Task WriteAsync(ClipboardWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!System.OperatingSystem.IsWindows()) throw Unsupported();

        // 图片需要把 Data URL 解码为 DIB 并以 CF_DIB 写入，当前未实现。
        if (options.Image is not null)
            throw new PlatformNotSupportedException(
                $"{nameof(DesktopClipboardService)} cannot write images; only text and URLs are supported.");

        var text = options.String ?? options.Url;
        if (text is null)
            throw new ArgumentException(
                "ClipboardWriteOptions must set String or Url.", nameof(options));

        RunOnStaThread(() => WriteWindowsText(text));
        return Task.CompletedTask;
    }

    public Task<ClipboardReadResult> ReadAsync()
    {
        if (!System.OperatingSystem.IsWindows()) throw Unsupported();

        var text = string.Empty;
        RunOnStaThread(() => text = ReadWindowsText());

        return Task.FromResult(new ClipboardReadResult(text, "text/plain"));
    }

    /// <summary>
    /// 在 STA 线程上执行剪贴板操作。
    /// <para>
    /// Win32 剪贴板要求调用线程是 STA：<c>OpenClipboard</c> 把所有权绑定到**当前线程**，
    /// 而 <c>await</c> 之后恢复的线程可能是任意一个 MTA 线程池线程。所有权落在那种随时会消失的
    /// 线程上，写入的内容随即失效——表现为「复制了却粘不出来」。
    /// </para>
    /// </summary>
    private static void RunOnStaThread(Action action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        ExceptionDispatchInfo? failure = null;

        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        failure?.Throw();
    }

    private static void WriteWindowsText(string text)
    {
        OpenClipboardWithRetry();

        try
        {
            EmptyClipboard();

            // 剪贴板取得内存所有权，因此成功之后不能再 GlobalFree。
            var bytes = (text.Length + 1) * 2; // UTF-16 + 终止符
            var handle = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
            if (handle == IntPtr.Zero)
                throw new OutOfMemoryException("GlobalAlloc failed for the clipboard buffer.");

            var target = GlobalLock(handle);
            if (target == IntPtr.Zero)
            {
                GlobalFree(handle);
                throw new InvalidOperationException("GlobalLock failed for the clipboard buffer.");
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * 2, 0);
            }
            finally
            {
                GlobalUnlock(handle);
            }

            if (SetClipboardData(CF_UNICODETEXT, handle) == IntPtr.Zero)
            {
                // 失败时所有权仍在我们手里，必须自己释放。
                GlobalFree(handle);
                throw new InvalidOperationException("SetClipboardData failed.");
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static string ReadWindowsText()
    {
        if (!IsClipboardFormatAvailable(CF_UNICODETEXT)) return string.Empty;

        OpenClipboardWithRetry();

        try
        {
            var handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero) return string.Empty;

            var source = GlobalLock(handle);
            if (source == IntPtr.Zero) return string.Empty;

            try
            {
                return Marshal.PtrToStringUni(source) ?? string.Empty;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>
    /// 打开剪贴板，失败时短暂重试。
    /// <para>
    /// Windows 剪贴板是全进程独占资源：任意一个进程（输入法、剪贴板管理器、另一个
    /// Miko 实例）只要正持有它，<c>OpenClipboard</c> 就返回 <c>false</c>。这是**常态**而非异常，
    /// 首次失败即放弃会让写入/读取随机失败，所以必须重试而不是直接抛。
    /// </para>
    /// </summary>
    private static void OpenClipboardWithRetry()
    {
        const int attempts = 10;
        const int delayMs = 10;

        for (var i = 0; i < attempts; i++)
        {
            if (OpenClipboard(IntPtr.Zero)) return;
            Thread.Sleep(delayMs);
        }

        throw new InvalidOperationException(
            $"Failed to open the Windows clipboard after {attempts} attempts; another process is holding it.");
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);
}
