namespace Miko.Platform.Video;

/// <summary>
/// 单个 <c>VideoElement</c> 对应的播放会话。生命周期与该元素绑定：元素首次进入渲染树时由引擎创建，
/// 元素从树中移除时由引擎 <see cref="System.IDisposable.Dispose"/>。
/// <para>
/// 控制方法（Play/Pause/Seek…）从主线程调用；解码与帧产出在后端自有线程进行，
/// 通过 <see cref="Event"/> 回调通知。回调可能在后端线程触发，订阅方需自行保证线程安全
/// （引擎侧通过 <c>MikoEngine.PostInvalidate</c> 把失效请求转交主循环）。
/// </para>
/// </summary>
public interface IVideoSession : IDisposable
{
    // ---- 控制（主线程调用）---------------------------------------------------
    void Play();
    void Pause();
    void Seek(TimeSpan position);

    /// <param name="volume">0..1，超出范围由实现钳制。</param>
    void SetVolume(float volume);
    void SetMuted(bool muted);

    /// <param name="rate">播放速率，1.0 为正常速度。</param>
    void SetPlaybackRate(float rate);
    void SetLoop(bool loop);

    // ---- 状态（属性返回当前快照）-------------------------------------------
    VideoSessionState State { get; }
    TimeSpan Duration { get; }
    TimeSpan Position { get; }

    /// <summary>媒体内禀像素宽，用于 replaced 元素的内禀尺寸/纵横比。加载完成前为 0。</summary>
    int VideoWidth { get; }

    /// <summary>媒体内禀像素高。加载完成前为 0。</summary>
    int VideoHeight { get; }

    /// <summary>
    /// 帧源：渲染引擎在合成阶段调用以取当前帧。该调用必须轻量且不阻塞解码线程
    /// （见 <see cref="IVideoFrameSource"/>）。
    /// </summary>
    IVideoFrameSource FrameSource { get; }

    /// <summary>
    /// 会话事件（Loaded / FrameAvailable / Ended / Error）。可能在后端线程触发。
    /// </summary>
    event Action<VideoSessionEvent>? Event;
}
