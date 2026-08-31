using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Miko.Windowing.Video.Linux;

/// <summary>
/// GStreamer 1.x 的最小 P/Invoke 声明集 —— 只覆盖「构建 pipeline、从 appsink 拉帧、
/// 定位/暂停」所需的 C API。GStreamer 是主流发行版的标准组件，通过 VAAPI/V4L2 插件
/// 使用 GPU 硬件解码。
/// <para>
/// 依赖运行时存在 <c>libgstreamer-1.0.so.0</c> 及 <c>libgstapp-1.0.so.0</c>；
/// 缺失时 <see cref="GStreamerVideoBackend.IsAvailable"/> 返回 false，视频降级为 poster。
/// </para>
/// </summary>
[SupportedOSPlatform("linux")]
internal static class GStreamerInterop
{
    private const string Gst = "libgstreamer-1.0.so.0";
    private const string GstApp = "libgstapp-1.0.so.0";
    private const string GLib = "libglib-2.0.so.0";
    private const string GObject = "libgobject-2.0.so.0";

    // ---- 生命周期 ----------------------------------------------------------

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern void gst_init(IntPtr argc, IntPtr argv);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern bool gst_is_initialized();

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern IntPtr gst_parse_launch(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string description, out IntPtr error);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern IntPtr gst_bin_get_by_name(IntPtr bin, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern GstStateChangeReturn gst_element_set_state(IntPtr element, GstState state);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern GstStateChangeReturn gst_element_get_state(
        IntPtr element, out GstState state, out GstState pending, ulong timeoutNanos);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern bool gst_element_query_duration(IntPtr element, GstFormat format, out long duration);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern bool gst_element_query_position(IntPtr element, GstFormat format, out long position);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern bool gst_element_seek_simple(
        IntPtr element, GstFormat format, GstSeekFlags flags, long position);

    // ---- appsink：从 pipeline 拉取解码后的帧 --------------------------------

    /// <summary>阻塞拉取一帧，超时返回 NULL。超时使拖动/停止能及时响应。</summary>
    [DllImport(GstApp, ExactSpelling = true)]
    internal static extern IntPtr gst_app_sink_try_pull_sample(IntPtr appsink, ulong timeoutNanos);

    [DllImport(GstApp, ExactSpelling = true)]
    internal static extern bool gst_app_sink_is_eos(IntPtr appsink);

    [DllImport(GstApp, ExactSpelling = true)]
    internal static extern void gst_app_sink_set_max_buffers(IntPtr appsink, uint max);

    [DllImport(GstApp, ExactSpelling = true)]
    internal static extern void gst_app_sink_set_drop(IntPtr appsink, bool drop);

    // ---- sample / buffer / caps -------------------------------------------

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern IntPtr gst_sample_get_buffer(IntPtr sample);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern IntPtr gst_sample_get_caps(IntPtr sample);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern void gst_sample_unref(IntPtr sample);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern bool gst_buffer_map(IntPtr buffer, out GstMapInfo info, GstMapFlags flags);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern void gst_buffer_unmap(IntPtr buffer, ref GstMapInfo info);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern IntPtr gst_caps_get_structure(IntPtr caps, uint index);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern bool gst_structure_get_int(
        IntPtr structure, [MarshalAs(UnmanagedType.LPUTF8Str)] string field, out int value);

    [DllImport(Gst, ExactSpelling = true)]
    internal static extern IntPtr gst_structure_get_string(
        IntPtr structure, [MarshalAs(UnmanagedType.LPUTF8Str)] string field);

    // ---- 对象管理 ----------------------------------------------------------

    [DllImport(GObject, ExactSpelling = true)]
    internal static extern void g_object_unref(IntPtr obj);

    [DllImport(GLib, ExactSpelling = true)]
    internal static extern void g_error_free(IntPtr error);

    [DllImport(GLib, ExactSpelling = true)]
    internal static extern void g_free(IntPtr mem);

    // ---- 类型 --------------------------------------------------------------

    internal enum GstState
    {
        VoidPending = 0,
        Null = 1,
        Ready = 2,
        Paused = 3,
        Playing = 4,
    }

    internal enum GstStateChangeReturn
    {
        Failure = 0,
        Success = 1,
        Async = 2,
        NoPreroll = 3,
    }

    internal enum GstFormat
    {
        Undefined = 0,
        Default = 1,
        Bytes = 2,
        Time = 3,
    }

    [Flags]
    internal enum GstSeekFlags
    {
        None = 0,
        Flush = 1 << 0,
        KeyUnit = 1 << 1,
        Accurate = 1 << 2,
    }

    [Flags]
    internal enum GstMapFlags
    {
        Read = 1 << 0,
        Write = 1 << 1,
    }

    /// <summary>
    /// <c>GstMapInfo</c> 的布局。尾部有 4 个保留指针，必须完整声明，
    /// 否则 <c>gst_buffer_map</c> 会写出结构体边界破坏栈。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GstMapInfo
    {
        public IntPtr Memory;
        public GstMapFlags Flags;
        public IntPtr Data;
        public nuint Size;
        public nuint MaxSize;
        public IntPtr UserData0;
        public IntPtr UserData1;
        public IntPtr UserData2;
        public IntPtr UserData3;
    }

    /// <summary>GStreamer 的时间单位是纳秒；<c>TimeSpan.Ticks</c> 是 100ns。</summary>
    internal const long NanosPerTick = 100;

    internal static TimeSpan FromGstTime(long nanos) =>
        nanos < 0 ? TimeSpan.Zero : TimeSpan.FromTicks(nanos / NanosPerTick);

    internal static long ToGstTime(TimeSpan time) => time.Ticks * NanosPerTick;

    /// <summary>读取 GError 的消息文本并释放它。</summary>
    internal static string? TakeErrorMessage(IntPtr error)
    {
        if (error == IntPtr.Zero) return null;

        try
        {
            // GError 布局：{ GQuark domain; gint code; gchar *message; }
            // domain + code 各 4 字节共 8 字节，随后 message 指针按指针宽度对齐。
            // 64 位下偏移为 8；32 位下前两字段仍占 8 字节，指针紧随其后，故偏移同为 8。
            const int messageOffset = 8;
            IntPtr messagePtr = Marshal.ReadIntPtr(error, messageOffset);
            return messagePtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(messagePtr) : null;
        }
        finally
        {
            g_error_free(error);
        }
    }
}
