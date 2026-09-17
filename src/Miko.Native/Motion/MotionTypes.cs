namespace Miko.Native.Motion;

/// <summary>三轴加速度（m/s²）。</summary>
/// <param name="X">X 轴分量。</param>
/// <param name="Y">Y 轴分量。</param>
/// <param name="Z">Z 轴分量。</param>
public sealed record Acceleration(double X, double Y, double Z);

/// <summary>三轴旋转速率（度/秒）。设备方向事件复用同一形状。</summary>
/// <param name="Alpha">绕 Z 轴（指南针方向）。</param>
/// <param name="Beta">绕 X 轴（前后倾斜）。</param>
/// <param name="Gamma">绕 Y 轴（左右倾斜）。</param>
public sealed record RotationRate(double Alpha, double Beta, double Gamma);

/// <summary>加速度采样事件。</summary>
/// <param name="Acceleration">去除重力后的加速度。</param>
/// <param name="AccelerationIncludingGravity">含重力的加速度。</param>
/// <param name="RotationRate">旋转速率。</param>
/// <param name="Interval">采样间隔（毫秒）。</param>
public sealed record AccelEvent(
    Acceleration Acceleration,
    Acceleration AccelerationIncludingGravity,
    RotationRate RotationRate,
    double Interval);
