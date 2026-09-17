namespace Miko.Native.Motion;

/// <summary>
/// 加速度与方向传感器。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/motion#api">Motion API</see>。
/// 支持平台：Android、iOS。
/// <para>
/// iOS 上首次监听前需要在用户操作（点击等）中请求系统传感器权限，否则不会有采样回调。
/// </para>
/// </summary>
public interface IMotionService
{
    /// <summary>加速度、含重力加速度、旋转速率和采样间隔。</summary>
    event Action<AccelEvent>? OnAccel;

    /// <summary>设备方向变化（指南针等）。</summary>
    event Action<RotationRate>? OnOrientation;
}
