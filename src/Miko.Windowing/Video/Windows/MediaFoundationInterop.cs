using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Miko.Windowing.Video.Windows;

/// <summary>
/// Media Foundation 的最小 P/Invoke 与 COM 声明集 —— 只覆盖 <c>IMFSourceReader</c>
/// 播放一路视频轨所需的部分，不引入任何第三方互操作包。
///
/// <para>
/// 用经典 <see cref="ComImportAttribute"/> + vtable 顺序声明：MF 的接口方法顺序即 ABI 契约，
/// 声明中方法的**顺序与个数必须与头文件完全一致**，否则会调错槽位。
/// 未用到的方法以 <c>Reserved*</c> 占位保持槽位对齐。
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
internal static class MediaFoundationInterop
{
    private const string Mfplat = "mfplat.dll";
    private const string Mfreadwrite = "mfreadwrite.dll";

    // ---- 初始化 ------------------------------------------------------------

    /// <summary>MF 版本号常量（MF_VERSION = MF_SDK_VERSION &lt;&lt; 16 | MF_API_VERSION）。</summary>
    internal const uint MF_VERSION = 0x00020070;

    internal const uint MFSTARTUP_NOSOCKET = 0x1;

    [DllImport(Mfplat, ExactSpelling = true)]
    internal static extern int MFStartup(uint version, uint flags);

    [DllImport(Mfplat, ExactSpelling = true)]
    internal static extern int MFShutdown();

    [DllImport(Mfplat, ExactSpelling = true)]
    internal static extern int MFCreateAttributes(out IMFAttributes attributes, uint initialSize);

    [DllImport(Mfplat, ExactSpelling = true)]
    internal static extern int MFCreateMediaType(out IMFMediaType mediaType);

    [DllImport(Mfreadwrite, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int MFCreateSourceReaderFromURL(
        [MarshalAs(UnmanagedType.LPWStr)] string url,
        IMFAttributes? attributes,
        out IMFSourceReader reader);

    // ---- GUID 常量 ---------------------------------------------------------
    // 主类型与子类型。NV12 是硬解器的原生输出；RGB32 用于 CPU 回退。

    internal static readonly Guid MFMediaType_Video = new("73646976-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MFVideoFormat_NV12 = new("3231564E-0000-0010-8000-00AA00389B71");
    internal static readonly Guid MFVideoFormat_RGB32 = new("00000016-0000-0010-8000-00AA00389B71");

    internal static readonly Guid MF_MT_MAJOR_TYPE = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
    internal static readonly Guid MF_MT_SUBTYPE = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
    internal static readonly Guid MF_MT_FRAME_SIZE = new("1652c33d-d6b2-4012-b834-72030849a37d");
    internal static readonly Guid MF_MT_DEFAULT_STRIDE = new("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");

    /// <summary>启用硬件解码（读取器内部创建 D3D11 设备管理器并走 D3D11VA）。</summary>
    internal static readonly Guid MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING =
        new("0f81da2c-b537-4672-a8b2-a681b17307a3");

    internal static readonly Guid MF_SOURCE_READER_D3D_MANAGER =
        new("ec822da2-e1e9-4b29-a0d8-563c719f5269");

    internal static readonly Guid MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS =
        new("a634a91c-822b-41b9-a494-4de4643612b0");

    /// <summary>时长（100ns 单位），存于媒体源的表示描述符。</summary>
    internal static readonly Guid MF_PD_DURATION = new("6c990d33-bb8e-477a-8598-0d5d96fcd88a");

    // ---- 常量 --------------------------------------------------------------

    internal const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
    internal const uint MF_SOURCE_READER_MEDIASOURCE = 0xFFFFFFFF;

    internal const uint MF_SOURCE_READERF_ENDOFSTREAM = 0x00000002;
    internal const uint MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED = 0x00000010;

    internal const int S_OK = 0;
    internal const int MF_E_INVALIDSTREAMNUMBER = unchecked((int)0xC00D36B3);

    /// <summary>把 HRESULT 转为异常。MF 的失败码没有可读消息，这里附上上下文说明。</summary>
    internal static void ThrowIfFailed(int hr, string what)
    {
        if (hr < 0)
            throw new InvalidOperationException(
                $"{what} failed (HRESULT 0x{hr:X8}).", Marshal.GetExceptionForHR(hr));
    }

    // ---- COM 接口 ----------------------------------------------------------

    [ComImport, Guid("2cd2d921-c447-44a7-a13c-4adabfc247e3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFAttributes
    {
        [PreserveSig] int GetItem(in Guid key, IntPtr value);
        [PreserveSig] int GetItemType(in Guid key, out int type);
        [PreserveSig] int CompareItem(in Guid key, IntPtr value, out bool result);
        [PreserveSig] int Compare(IMFAttributes attributes, int matchType, out bool result);
        [PreserveSig] int GetUINT32(in Guid key, out uint value);
        [PreserveSig] int GetUINT64(in Guid key, out ulong value);
        [PreserveSig] int GetDouble(in Guid key, out double value);
        [PreserveSig] int GetGUID(in Guid key, out Guid value);
        [PreserveSig] int GetStringLength(in Guid key, out uint length);
        [PreserveSig] int GetString(in Guid key, [Out] char[] value, uint size, ref uint length);
        [PreserveSig] int GetAllocatedString(in Guid key, out IntPtr value, out uint length);
        [PreserveSig] int GetBlobSize(in Guid key, out uint size);
        [PreserveSig] int GetBlob(in Guid key, [Out] byte[] buffer, uint bufferSize, ref uint blobSize);
        [PreserveSig] int GetAllocatedBlob(in Guid key, out IntPtr buffer, out uint size);
        [PreserveSig] int GetUnknown(in Guid key, in Guid riid, out IntPtr ppv);
        [PreserveSig] int SetItem(in Guid key, IntPtr value);
        [PreserveSig] int DeleteItem(in Guid key);
        [PreserveSig] int DeleteAllItems();
        [PreserveSig] int SetUINT32(in Guid key, uint value);
        [PreserveSig] int SetUINT64(in Guid key, ulong value);
        [PreserveSig] int SetDouble(in Guid key, double value);
        [PreserveSig] int SetGUID(in Guid key, in Guid value);
        [PreserveSig] int SetString(in Guid key, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [PreserveSig] int SetBlob(in Guid key, byte[] buffer, uint size);
        [PreserveSig] int SetUnknown(in Guid key, [MarshalAs(UnmanagedType.IUnknown)] object? unknown);
        [PreserveSig] int LockStore();
        [PreserveSig] int UnlockStore();
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetItemByIndex(uint index, out Guid key, IntPtr value);
        [PreserveSig] int CopyAllItems(IMFAttributes dest);
    }

    /// <summary>
    /// <c>IMFMediaType</c> 继承 <c>IMFAttributes</c>，因此必须重复父接口的全部方法声明
    /// 以保持 vtable 槽位对齐（C# 的接口继承不会为 COM 展开 vtable）。
    /// </summary>
    [ComImport, Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaType
    {
        // --- IMFAttributes 部分（槽位 3..32）---
        [PreserveSig] int GetItem(in Guid key, IntPtr value);
        [PreserveSig] int GetItemType(in Guid key, out int type);
        [PreserveSig] int CompareItem(in Guid key, IntPtr value, out bool result);
        [PreserveSig] int Compare(IMFAttributes attributes, int matchType, out bool result);
        [PreserveSig] int GetUINT32(in Guid key, out uint value);
        [PreserveSig] int GetUINT64(in Guid key, out ulong value);
        [PreserveSig] int GetDouble(in Guid key, out double value);
        [PreserveSig] int GetGUID(in Guid key, out Guid value);
        [PreserveSig] int GetStringLength(in Guid key, out uint length);
        [PreserveSig] int GetString(in Guid key, [Out] char[] value, uint size, ref uint length);
        [PreserveSig] int GetAllocatedString(in Guid key, out IntPtr value, out uint length);
        [PreserveSig] int GetBlobSize(in Guid key, out uint size);
        [PreserveSig] int GetBlob(in Guid key, [Out] byte[] buffer, uint bufferSize, ref uint blobSize);
        [PreserveSig] int GetAllocatedBlob(in Guid key, out IntPtr buffer, out uint size);
        [PreserveSig] int GetUnknown(in Guid key, in Guid riid, out IntPtr ppv);
        [PreserveSig] int SetItem(in Guid key, IntPtr value);
        [PreserveSig] int DeleteItem(in Guid key);
        [PreserveSig] int DeleteAllItems();
        [PreserveSig] int SetUINT32(in Guid key, uint value);
        [PreserveSig] int SetUINT64(in Guid key, ulong value);
        [PreserveSig] int SetDouble(in Guid key, double value);
        [PreserveSig] int SetGUID(in Guid key, in Guid value);
        [PreserveSig] int SetString(in Guid key, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [PreserveSig] int SetBlob(in Guid key, byte[] buffer, uint size);
        [PreserveSig] int SetUnknown(in Guid key, [MarshalAs(UnmanagedType.IUnknown)] object? unknown);
        [PreserveSig] int LockStore();
        [PreserveSig] int UnlockStore();
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetItemByIndex(uint index, out Guid key, IntPtr value);
        [PreserveSig] int CopyAllItems(IMFAttributes dest);

        // --- IMFMediaType 自有部分 ---
        [PreserveSig] int GetMajorType(out Guid majorType);
        [PreserveSig] int IsCompressedFormat(out bool compressed);
        [PreserveSig] int IsEqual(IMFMediaType mediaType, out uint flags);
        [PreserveSig] int GetRepresentation(Guid representation, out IntPtr representationData);
        [PreserveSig] int FreeRepresentation(Guid representation, IntPtr representationData);
    }

    [ComImport, Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSourceReader
    {
        [PreserveSig] int GetStreamSelection(uint streamIndex, out bool selected);
        [PreserveSig] int SetStreamSelection(uint streamIndex, bool selected);
        [PreserveSig] int GetNativeMediaType(uint streamIndex, uint mediaTypeIndex, out IMFMediaType mediaType);
        [PreserveSig] int GetCurrentMediaType(uint streamIndex, out IMFMediaType mediaType);
        [PreserveSig] int SetCurrentMediaType(uint streamIndex, IntPtr reserved, IMFMediaType mediaType);
        [PreserveSig] int SetCurrentPosition(in Guid guidTimeFormat, in PropVariant position);
        [PreserveSig] int ReadSample(
            uint streamIndex, uint controlFlags,
            out uint actualStreamIndex, out uint streamFlags, out long timestamp,
            out IMFSample? sample);
        [PreserveSig] int Flush(uint streamIndex);
        [PreserveSig] int GetServiceForStream(uint streamIndex, in Guid guidService, in Guid riid, out IntPtr service);
        [PreserveSig] int GetPresentationAttribute(uint streamIndex, in Guid guidAttribute, out PropVariant attribute);
    }

    [ComImport, Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFSample
    {
        // --- IMFAttributes 部分 ---
        [PreserveSig] int GetItem(in Guid key, IntPtr value);
        [PreserveSig] int GetItemType(in Guid key, out int type);
        [PreserveSig] int CompareItem(in Guid key, IntPtr value, out bool result);
        [PreserveSig] int Compare(IMFAttributes attributes, int matchType, out bool result);
        [PreserveSig] int GetUINT32(in Guid key, out uint value);
        [PreserveSig] int GetUINT64(in Guid key, out ulong value);
        [PreserveSig] int GetDouble(in Guid key, out double value);
        [PreserveSig] int GetGUID(in Guid key, out Guid value);
        [PreserveSig] int GetStringLength(in Guid key, out uint length);
        [PreserveSig] int GetString(in Guid key, [Out] char[] value, uint size, ref uint length);
        [PreserveSig] int GetAllocatedString(in Guid key, out IntPtr value, out uint length);
        [PreserveSig] int GetBlobSize(in Guid key, out uint size);
        [PreserveSig] int GetBlob(in Guid key, [Out] byte[] buffer, uint bufferSize, ref uint blobSize);
        [PreserveSig] int GetAllocatedBlob(in Guid key, out IntPtr buffer, out uint size);
        [PreserveSig] int GetUnknown(in Guid key, in Guid riid, out IntPtr ppv);
        [PreserveSig] int SetItem(in Guid key, IntPtr value);
        [PreserveSig] int DeleteItem(in Guid key);
        [PreserveSig] int DeleteAllItems();
        [PreserveSig] int SetUINT32(in Guid key, uint value);
        [PreserveSig] int SetUINT64(in Guid key, ulong value);
        [PreserveSig] int SetDouble(in Guid key, double value);
        [PreserveSig] int SetGUID(in Guid key, in Guid value);
        [PreserveSig] int SetString(in Guid key, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [PreserveSig] int SetBlob(in Guid key, byte[] buffer, uint size);
        [PreserveSig] int SetUnknown(in Guid key, [MarshalAs(UnmanagedType.IUnknown)] object? unknown);
        [PreserveSig] int LockStore();
        [PreserveSig] int UnlockStore();
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetItemByIndex(uint index, out Guid key, IntPtr value);
        [PreserveSig] int CopyAllItems(IMFAttributes dest);

        // --- IMFSample 自有部分 ---
        [PreserveSig] int GetSampleFlags(out uint flags);
        [PreserveSig] int SetSampleFlags(uint flags);
        [PreserveSig] int GetSampleTime(out long sampleTime);
        [PreserveSig] int SetSampleTime(long sampleTime);
        [PreserveSig] int GetSampleDuration(out long duration);
        [PreserveSig] int SetSampleDuration(long duration);
        [PreserveSig] int GetBufferCount(out uint bufferCount);
        [PreserveSig] int GetBufferByIndex(uint index, out IMFMediaBuffer buffer);
        [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBuffer buffer);
        [PreserveSig] int AddBuffer(IMFMediaBuffer buffer);
        [PreserveSig] int RemoveBufferByIndex(uint index);
        [PreserveSig] int RemoveAllBuffers();
        [PreserveSig] int GetTotalLength(out uint totalLength);
        [PreserveSig] int CopyToBuffer(IMFMediaBuffer buffer);
    }

    [ComImport, Guid("045fa593-8799-42b8-bc8d-8968c6453507"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaBuffer
    {
        [PreserveSig] int Lock(out IntPtr buffer, out uint maxLength, out uint currentLength);
        [PreserveSig] int Unlock();
        [PreserveSig] int GetCurrentLength(out uint currentLength);
        [PreserveSig] int SetCurrentLength(uint currentLength);
        [PreserveSig] int GetMaxLength(out uint maxLength);
    }

    /// <summary>
    /// <c>IMF2DBuffer</c>：带行填充的二维缓冲。硬解输出的 NV12 常有 stride ≠ width，
    /// 用它拿到真实 stride 才能正确寻址（否则画面斜切）。
    /// </summary>
    [ComImport, Guid("7dc9d5f9-9ed9-44ec-9bbf-0600bb589fbb"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMF2DBuffer
    {
        [PreserveSig] int Lock2D(out IntPtr scanline0, out int pitch);
        [PreserveSig] int Unlock2D();
        [PreserveSig] int GetScanline0AndPitch(out IntPtr scanline0, out int pitch);
        [PreserveSig] int IsContiguousFormat(out bool contiguous);
        [PreserveSig] int GetContiguousLength(out uint length);
        [PreserveSig] int ContiguousCopyTo(IntPtr destBuffer, uint destLength);
        [PreserveSig] int ContiguousCopyFrom(IntPtr srcBuffer, uint srcLength);
    }

    /// <summary>
    /// PROPVARIANT 的最小布局。只用到 64 位整数（时长）与 seek 位置，
    /// 因此按「类型标记 + 8 字节值」布局，并经 <see cref="Clear"/> 释放。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PropVariant
    {
        public ushort Type;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public long Value;
        public long Value2;

        internal const ushort VT_EMPTY = 0;
        internal const ushort VT_I8 = 20;
        internal const ushort VT_UI8 = 21;

        /// <summary>构造 100ns 单位的时间值，用于 <c>SetCurrentPosition</c>。</summary>
        internal static PropVariant FromLong(long value) => new() { Type = VT_I8, Value = value };

        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern int PropVariantClear(ref PropVariant pvar);

        internal void Clear()
        {
            // 仅整数类型时无堆分配，但仍按规范调用以防类型判断失误导致泄漏。
            var copy = this;
            PropVariantClear(ref copy);
        }
    }

    /// <summary>MF 的时间单位：100 纳秒。</summary>
    internal const long TicksPerMfUnit = 1;   // TimeSpan.Ticks 也是 100ns，两者 1:1
}
