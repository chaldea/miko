using Miko.DevTools;
using Miko.Hosting;
using Miko.Windowing.Common;
using Shouldly;

namespace Miko.McpServer.Tests;

/// <summary>
/// ISSUE-148：DevTools 窗口不再自起线程跑窗口循环（macOS 上会卡死），而是挂到桌面宿主的
/// <see cref="SilkEventLoop"/> 上。这里不创建真实窗口——事件循环尚未创建的 DevTools 窗口
/// 被销毁时不触碰任何原生资源——只锁定桥接层的打开/关闭状态机。
/// </summary>
public class DevToolsWindowHostingTests
{
    private static DevToolsBridge InitializedBridge()
    {
        var bridge = new DevToolsBridge(new DevToolsOptions());
        bridge.Initialize(new MikoEngineBuilder().Build());
        return bridge;
    }

    private static object? CurrentWindow(DevToolsBridge bridge) =>
        typeof(DevToolsBridge)
            .GetField("_devToolsWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(bridge);

    [Fact]
    public void Open_Should_Do_Nothing_Without_A_Desktop_Event_Loop()
    {
        // 移动端宿主或单元测试里没有桌面事件循环：F12 不应开出窗口，也不应把状态卡在「已打开」。
        var bridge = InitializedBridge();

        bridge.OpenDevTools();

        bridge.IsOpen.ShouldBeFalse();
        CurrentWindow(bridge).ShouldBeNull();
    }

    [Fact]
    public void Toggle_Should_Attach_And_Close_Through_The_Event_Loop()
    {
        var bridge = InitializedBridge();
        var owner = new object();
        SilkEventLoop.TryEnter(owner).ShouldBeTrue();
        try
        {
            bridge.ToggleDevTools();
            bridge.IsOpen.ShouldBeTrue();
            CurrentWindow(bridge).ShouldNotBeNull();

            bridge.ToggleDevTools();
            bridge.IsOpen.ShouldBeFalse();
            CurrentWindow(bridge).ShouldBeNull();
        }
        finally
        {
            SilkEventLoop.Exit(owner);
        }

        bridge.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void Stale_Window_Teardown_Should_Not_Close_A_Newer_Window()
    {
        // 快速按两次 F12：旧窗口的收尾（事件循环线程上的 Destroy）晚于新窗口的打开。
        var bridge = InitializedBridge();
        var owner = new object();
        SilkEventLoop.TryEnter(owner).ShouldBeTrue();
        try
        {
            bridge.OpenDevTools();
            var first = (ISilkEventLoopWindow)CurrentWindow(bridge)!;
            bridge.CloseDevTools();
            bridge.OpenDevTools();
            var second = CurrentWindow(bridge);

            first.Destroy();

            bridge.IsOpen.ShouldBeTrue();
            CurrentWindow(bridge).ShouldBeSameAs(second);
        }
        finally
        {
            SilkEventLoop.Exit(owner);
        }

        // 宿主退出时销毁了当前窗口，桥接层随之复位。
        bridge.IsOpen.ShouldBeFalse();
        CurrentWindow(bridge).ShouldBeNull();
    }

    [Fact]
    public void Window_Closed_By_The_User_Should_Reset_The_Bridge()
    {
        var bridge = InitializedBridge();
        var owner = new object();
        SilkEventLoop.TryEnter(owner).ShouldBeTrue();
        try
        {
            bridge.OpenDevTools();
            var window = (ISilkEventLoopWindow)CurrentWindow(bridge)!;

            // 用户点关闭按钮：事件循环发现窗口在关闭，随即销毁它。
            window.Destroy();

            bridge.IsOpen.ShouldBeFalse();
            bridge.SelectedElement.ShouldBeNull();
            CurrentWindow(bridge).ShouldBeNull();

            // 之后 F12 能再次打开。
            bridge.ToggleDevTools();
            bridge.IsOpen.ShouldBeTrue();
        }
        finally
        {
            SilkEventLoop.Exit(owner);
        }
    }
}
