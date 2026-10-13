using Miko.Windowing.Common;
using Shouldly;

namespace Miko.Native.Tests.Desktop;

/// <summary>
/// ISSUE-148：DevTools 等后开的窗口挂在宿主的事件循环上，由主线程创建、泵事件、销毁
/// （macOS 只允许主线程创建窗口）。真实窗口无法在单测里打开，这里用假窗口锁定
/// <see cref="SilkEventLoop"/> 的生命周期契约：创建与泵事件都在循环线程、每个窗口恰好销毁一次。
/// </summary>
public class SilkEventLoopTests
{
    private sealed class FakeWindow : ISilkEventLoopWindow
    {
        public int Created;
        public int Pumped;
        public int Destroyed;
        public int? CreateThread;
        public bool KeepOpen = true;
        public Exception? CreateError;
        public Exception? DestroyError;

        public void Create()
        {
            Created++;
            CreateThread = Environment.CurrentManagedThreadId;
            if (CreateError != null) throw CreateError;
        }

        public bool PumpEvents()
        {
            Pumped++;
            return KeepOpen;
        }

        public void Destroy()
        {
            Destroyed++;
            if (DestroyError != null) throw DestroyError;
        }
    }

    /// <summary>以 <paramref name="owner"/> 充当事件循环运行 <paramref name="body"/>，结束时必定退出。</summary>
    private static void WithLoop(Action<object> body)
    {
        var owner = new object();
        SilkEventLoop.TryEnter(owner).ShouldBeTrue();
        try
        {
            body(owner);
        }
        finally
        {
            SilkEventLoop.Exit(owner);
        }
    }

    [Fact]
    public void TryAttach_Should_Refuse_When_No_Event_Loop_Is_Running()
    {
        var window = new FakeWindow();

        SilkEventLoop.TryAttach(window).ShouldBeFalse();
        window.Created.ShouldBe(0);
        window.Destroyed.ShouldBe(0);
    }

    [Fact]
    public void Attached_Window_Should_Be_Created_And_Pumped_On_The_Loop_Thread()
    {
        WithLoop(_ =>
        {
            var window = new FakeWindow();
            int attachThread = -1;

            // 打开请求来自别的线程（DevTools 由主窗口渲染线程上的 F12 打开）。
            var t = new Thread(() =>
            {
                attachThread = Environment.CurrentManagedThreadId;
                SilkEventLoop.TryAttach(window).ShouldBeTrue();
            });
            t.Start();
            t.Join();
            window.Created.ShouldBe(0);

            SilkEventLoop.PumpAttachedWindows();
            SilkEventLoop.PumpAttachedWindows();

            window.Created.ShouldBe(1);
            window.CreateThread.ShouldBe(Environment.CurrentManagedThreadId);
            window.CreateThread.ShouldNotBe(attachThread);
            window.Pumped.ShouldBe(2);
            window.Destroyed.ShouldBe(0);
        });
    }

    [Fact]
    public void Closed_Window_Should_Be_Destroyed_Once_And_No_Longer_Pumped()
    {
        var window = new FakeWindow();
        WithLoop(_ =>
        {
            SilkEventLoop.TryAttach(window).ShouldBeTrue();
            SilkEventLoop.PumpAttachedWindows();

            window.KeepOpen = false;
            SilkEventLoop.PumpAttachedWindows();
            SilkEventLoop.PumpAttachedWindows();

            window.Pumped.ShouldBe(2);
            window.Destroyed.ShouldBe(1);
        });

        // 循环结束不会再销毁一次。
        window.Destroyed.ShouldBe(1);
    }

    [Fact]
    public void Exit_Should_Destroy_Open_And_Not_Yet_Created_Windows()
    {
        var open = new FakeWindow();
        var pending = new FakeWindow();

        WithLoop(_ =>
        {
            SilkEventLoop.TryAttach(open).ShouldBeTrue();
            SilkEventLoop.PumpAttachedWindows();
            // 循环在创建它之前就结束了。
            SilkEventLoop.TryAttach(pending).ShouldBeTrue();
        });

        open.Destroyed.ShouldBe(1);
        pending.Created.ShouldBe(0);
        pending.Destroyed.ShouldBe(1);
        SilkEventLoop.TryAttach(new FakeWindow()).ShouldBeFalse();
    }

    [Fact]
    public void Window_Whose_Create_Throws_Should_Still_Be_Destroyed_When_The_Loop_Ends()
    {
        var window = new FakeWindow { CreateError = new InvalidOperationException("no metal") };

        WithLoop(_ =>
        {
            SilkEventLoop.TryAttach(window).ShouldBeTrue();
            Should.Throw<InvalidOperationException>(SilkEventLoop.PumpAttachedWindows);
        });

        window.Destroyed.ShouldBe(1);
    }

    [Fact]
    public void Exit_Should_Destroy_Every_Window_Even_When_One_Throws()
    {
        var failing = new FakeWindow { DestroyError = new InvalidOperationException("boom") };
        var other = new FakeWindow();
        var owner = new object();

        SilkEventLoop.TryEnter(owner).ShouldBeTrue();
        SilkEventLoop.TryAttach(failing).ShouldBeTrue();
        SilkEventLoop.TryAttach(other).ShouldBeTrue();
        SilkEventLoop.PumpAttachedWindows();

        Should.Throw<InvalidOperationException>(() => SilkEventLoop.Exit(owner)).Message.ShouldBe("boom");
        failing.Destroyed.ShouldBe(1);
        other.Destroyed.ShouldBe(1);
        SilkEventLoop.TryAttach(new FakeWindow()).ShouldBeFalse();
    }

    [Fact]
    public void Only_The_First_Loop_Should_Own_The_Event_Loop()
    {
        WithLoop(owner =>
        {
            var second = new object();
            SilkEventLoop.TryEnter(second).ShouldBeFalse();

            // 非所有者退出不影响正在运行的事件循环。
            SilkEventLoop.Exit(second);
            var window = new FakeWindow();
            SilkEventLoop.TryAttach(window).ShouldBeTrue();
        });
    }
}
