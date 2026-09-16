using System.Runtime.CompilerServices;

namespace Miko.Native;

/// <summary>
/// 原生服务实现的可选基类，集中「本平台不支持该能力」的失败语义。
/// <para>
/// 契约（<c>issues/feat-platform.md</c> 平台实现要求第 1 条）：每个实现项目只实现其支持平台的
/// 接口成员，**不支持的能力必须抛出明确的 <see cref="PlatformNotSupportedException"/>**，
/// 而不是静默返回空值或假数据——后者会让调用方以为拿到了真实结果。
/// </para>
/// </summary>
public abstract class NativeServiceBase
{
    /// <summary>
    /// 抛出带成员名的 <see cref="PlatformNotSupportedException"/>。
    /// <paramref name="member"/> 由编译器填入调用方成员名，无需手写。
    /// </summary>
    /// <returns>永不返回；声明返回值只是为了能写成 <c>=&gt; throw Unsupported()</c> 之外的
    /// <c>return Unsupported&lt;T&gt;();</c> 形式。</returns>
    protected Exception Unsupported([CallerMemberName] string member = "")
        => new PlatformNotSupportedException(
            $"{GetType().Name}.{member} is not supported on this platform.");

    /// <summary>返回一个以 <see cref="PlatformNotSupportedException"/> 失败的 <see cref="Task"/>。</summary>
    /// <remarks>
    /// 异步成员用它而不是直接 <c>throw</c>：<c>async</c> 方法里 throw 与非 <c>async</c> 方法里 throw
    /// 的时机不同（后者在 await 之前就抛），统一走失败 Task 让调用方无论哪种写法都能 <c>await</c> 捕获。
    /// </remarks>
    protected Task UnsupportedAsync([CallerMemberName] string member = "")
        => Task.FromException(Unsupported(member));

    /// <summary>返回一个以 <see cref="PlatformNotSupportedException"/> 失败的 <see cref="Task{T}"/>。</summary>
    protected Task<T> UnsupportedAsync<T>([CallerMemberName] string member = "")
        => Task.FromException<T>(Unsupported(member));
}
