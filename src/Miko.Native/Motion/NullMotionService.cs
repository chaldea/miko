namespace Miko.Native.Motion;

/// <summary>
/// 未注册平台实现时的 <see cref="IMotionService"/> 占位实现。
/// 本接口只有事件、没有方法，因此订阅不抛异常，只是永远收不到采样。
/// </summary>
public sealed class NullMotionService : IMotionService
{
    public event Action<AccelEvent>? OnAccel { add { } remove { } }
    public event Action<RotationRate>? OnOrientation { add { } remove { } }
}
