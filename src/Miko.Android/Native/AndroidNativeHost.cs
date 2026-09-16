using Android.App;
using Android.Content;
using Miko.Native;

namespace Miko.Android.Native;

/// <summary>
/// Android Native 服务看到的宿主。<see cref="MikoSurfaceView"/> 构造时把它
/// <c>Attach</c> 到 <see cref="INativeHostContext"/>。
/// <para>
/// 之所以不直接把 <c>Activity</c> Attach 上去：相机、相册、系统设置等能力要
/// <c>StartActivityForResult</c> 并等待回调，而回调只会送到 <c>Activity.OnActivityResult</c>。
/// 宿主必须把它转发进来，<see cref="AndroidActivityResultRelay"/> 负责把回调接回到
/// 对应的 <c>Task</c>。
/// </para>
/// </summary>
public sealed class AndroidNativeHost
{
    public AndroidNativeHost(Context context, Activity? activity)
    {
        Context = context;
        Activity = activity;
        ActivityResults = new AndroidActivityResultRelay();
    }

    /// <summary>Android 上下文（取系统服务用）。</summary>
    public Context Context { get; }

    /// <summary>承载视图的 Activity；能力需要界面交互（相机/权限）时必须存在。</summary>
    public Activity? Activity { get; }

    /// <summary>startActivityForResult 的结果中继。</summary>
    public AndroidActivityResultRelay ActivityResults { get; }

    /// <summary>取得 Activity，缺失时抛出说明性异常（视图未挂在 Activity 上）。</summary>
    public Activity RequireActivity() => Activity ?? throw new InvalidOperationException(
        "This capability needs an Activity, but the Miko view was created with a non-Activity Context.");
}

/// <summary>
/// <c>startActivityForResult</c> 的结果中继。
/// <para>
/// Android 的 Activity 结果是**进程级回调**而非返回值：发起方拿不到结果，
/// 只有 <c>Activity.OnActivityResult</c> 会收到。宿主 Activity 必须重写该方法并调用
/// <see cref="Deliver"/>，否则相机、相册等能力会永远等待。
/// </para>
/// </summary>
public sealed class AndroidActivityResultRelay
{
    // requestCode 是 16 位（Android 限制 startActivityForResult 的 requestCode 不得使用高位），
    // 从一个不易与应用自身请求码冲突的基数开始分配。
    private const int RequestCodeBase = 0x4D00; // 'M'
    private const int RequestCodeMax = 0xFFFF;

    private readonly Lock _gate = new();
    private readonly Dictionary<int, TaskCompletionSource<ActivityResult>> _pending = new();
    private int _nextRequestCode = RequestCodeBase;

    /// <summary>登记一次等待，返回要传给 <c>startActivityForResult</c> 的 requestCode。</summary>
    public (int RequestCode, Task<ActivityResult> Result) Register()
    {
        var tcs = new TaskCompletionSource<ActivityResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_gate)
        {
            // requestCode 会在 0xFFFF 处回绕。直接用索引器赋值会**静默顶掉**一个仍在等待的
            // TCS，那个调用方就永远等不到结果了——所以要跳过仍被占用的码。
            var attempts = RequestCodeMax - RequestCodeBase + 1;

            for (var i = 0; i < attempts; i++)
            {
                var requestCode = _nextRequestCode++;
                if (_nextRequestCode > RequestCodeMax) _nextRequestCode = RequestCodeBase;

                if (!_pending.TryAdd(requestCode, tcs)) continue;

                return (requestCode, tcs.Task);
            }

            throw new InvalidOperationException(
                $"All {attempts} activity request codes are in flight; " +
                "the host Activity is probably not forwarding OnActivityResult.");
        }
    }

    /// <summary>
    /// 由宿主 Activity 的 <c>OnActivityResult</c> 调用，把结果交还给等待方。
    /// 返回值表示该 requestCode 是否由 Miko 发起。
    /// </summary>
    public bool Deliver(int requestCode, Result resultCode, Intent? data)
    {
        TaskCompletionSource<ActivityResult>? tcs;

        lock (_gate)
        {
            if (!_pending.Remove(requestCode, out tcs)) return false;
        }

        tcs.TrySetResult(new ActivityResult(resultCode, data));
        return true;
    }

    /// <summary>取消一次等待（例如调用方超时或视图销毁）。</summary>
    public void Cancel(int requestCode)
    {
        TaskCompletionSource<ActivityResult>? tcs;

        lock (_gate)
        {
            if (!_pending.Remove(requestCode, out tcs)) return;
        }

        tcs.TrySetCanceled();
    }

    /// <summary>
    /// 取消全部等待中的请求。
    /// <para>
    /// 屏幕旋转会销毁并重建 Activity，新的 <see cref="AndroidNativeHost"/> 随之被 Attach。
    /// 此时挂在旧 relay 上的请求再也等不到结果（结果会送到新 Activity），必须主动作废，
    /// 否则调用方永远挂起。
    /// </para>
    /// </summary>
    public void CancelAll()
    {
        List<TaskCompletionSource<ActivityResult>> pending;

        lock (_gate)
        {
            pending = _pending.Values.ToList();
            _pending.Clear();
        }

        foreach (var tcs in pending)
            tcs.TrySetException(new OperationCanceledException(
                "The hosting Activity was recreated (for example on rotation) before the result arrived."));
    }
}

/// <summary>一次 <c>startActivityForResult</c> 的结果。</summary>
/// <param name="ResultCode">结果码；<see cref="Result.Ok"/> 表示用户完成了操作。</param>
/// <param name="Data">结果 Intent。</param>
public sealed record ActivityResult(Result ResultCode, Intent? Data)
{
    /// <summary>用户是否完成了操作（而非取消）。</summary>
    public bool IsOk => ResultCode == Result.Ok;
}
