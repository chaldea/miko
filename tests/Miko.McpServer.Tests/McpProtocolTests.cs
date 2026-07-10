using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Miko.DevTools;
using Miko.McpServer.Tools;
using Miko.Simulator;
using Shouldly;

namespace Miko.McpServer.Tests;

/// <summary>
/// End-to-end MCP protocol tests. Spins up the real MCP tool types behind an
/// in-process Kestrel host and connects with the official MCP client, exercising
/// the initialize handshake + tools/list + tools/call path that a client like
/// Claude Code performs. This is the regression guard for the "invalid_union /
/// unrecognized_keys" protocol error that a hand-rolled JSON-RPC server produced.
/// </summary>
public sealed class McpProtocolTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private int _port;
    private readonly FakeSimulatorService _sim = new();
    private readonly FakeDevToolsService _dev = new();

    public async Task InitializeAsync()
    {
        _port = 5100 + Random.Shared.Next(400);
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{_port}");
        builder.Services.AddSingleton<ISimulatorService>(_sim);
        builder.Services.AddSingleton<IDevToolsService>(_dev);
        builder.Services
            .AddMcpServer(o => o.ServerInfo = new() { Name = "miko", Version = "1.0.0" })
            .WithHttpTransport()
            .WithTools<SimulatorTools>()
            .WithTools<DevToolsTools>();
        _app = builder.Build();
        _app.MapMcp();
        await _app.StartAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<McpClient> ConnectAsync()
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"http://127.0.0.1:{_port}"),
            TransportMode = HttpTransportMode.StreamableHttp,
            Name = "test",
        }, LoggerFactory.Create(_ => { }));
        return await McpClient.CreateAsync(transport, new McpClientOptions
        {
            ClientInfo = new() { Name = "test", Version = "1.0.0" }
        });
    }

    private static string TextOf(CallToolResult r) =>
        r.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";

    [Fact]
    public async Task Initialize_handshake_succeeds()
    {
        var client = await ConnectAsync();
        client.ServerInfo.Name.ShouldBe("miko");
    }

    [Fact]
    public async Task ListTools_returns_all_registered_tools()
    {
        var client = await ConnectAsync();
        var tools = await client.ListToolsAsync();
        var names = tools.Select(t => t.Name).ToList();

        names.ShouldContain("simulator_get_current_device");
        names.ShouldContain("simulator_select_device");
        names.ShouldContain("simulator_screenshot");
        names.ShouldContain("devtools_get_dom_tree");
        names.ShouldContain("devtools_click_element");
        names.ShouldContain("devtools_get_logs");
        tools.Count.ShouldBe(15);
    }

    [Fact]
    public async Task CallTool_readonly_returns_device_json()
    {
        var client = await ConnectAsync();
        var result = await client.CallToolAsync("simulator_get_current_device",
            new Dictionary<string, object?>());
        TextOf(result).ShouldContain("iPhone 15 Pro");
    }

    [Fact]
    public async Task CallTool_with_argument_mutates_state()
    {
        var client = await ConnectAsync();
        var result = await client.CallToolAsync("simulator_select_device",
            new Dictionary<string, object?> { ["deviceName"] = "Pixel 7" });

        TextOf(result).ShouldContain("Pixel 7");
        _sim.Selected.ShouldBe("Pixel 7");
    }

    [Fact]
    public async Task CallTool_unknown_device_reports_not_found()
    {
        var client = await ConnectAsync();
        var result = await client.CallToolAsync("simulator_select_device",
            new Dictionary<string, object?> { ["deviceName"] = "Nokia 3310" });
        TextOf(result).ShouldContain("未找到");
    }

    [Fact]
    public async Task CallTool_devtools_dom_tree_returns_json()
    {
        var client = await ConnectAsync();
        var result = await client.CallToolAsync("devtools_get_dom_tree",
            new Dictionary<string, object?>());
        TextOf(result).ShouldContain("root");
    }

    [Fact]
    public async Task CallTool_click_element_dispatches()
    {
        var client = await ConnectAsync();
        var result = await client.CallToolAsync("devtools_click_element",
            new Dictionary<string, object?> { ["elementId"] = "btn-1" });
        TextOf(result).ShouldContain("btn-1");
        _dev.LastClicked.ShouldBe("btn-1");
    }

    // ---------------- fakes ----------------

    private sealed class FakeSimulatorService : ISimulatorService
    {
        public string Selected = "iPhone 15 Pro";
        public DeviceInfo GetCurrentDevice() => new(Selected, 393, 852, 3f, "Phone", "Ios");
        public IReadOnlyList<DeviceInfo> GetAvailableDevices() => new List<DeviceInfo>
        {
            new("iPhone 15 Pro", 393, 852, 3f, "Phone", "Ios"),
            new("Pixel 7", 412, 915, 2.625f, "Phone", "Android"),
        };
        public bool SelectDevice(string deviceName)
        {
            if (deviceName is not ("iPhone 15 Pro" or "Pixel 7")) return false;
            Selected = deviceName; return true;
        }
        public Orientation GetOrientation() => Orientation.Portrait;
        public bool SetOrientation(Orientation o) => true;
        public bool GetSafeAreaEnabled() => true;
        public bool ToggleSafeArea(bool e) => true;
        public byte[]? CaptureScreenshot() => new byte[] { 1, 2, 3 };
    }

    private sealed class FakeDevToolsService : IDevToolsService
    {
        public string? LastClicked;
        public string GetDomTree() => "{\"tagName\":\"root\",\"children\":[]}";
        public ElementInfo? FindElementById(string id) => new(id, "div", null, null, 0);
        public IReadOnlyList<ElementInfo> QueryElements(string s) => new List<ElementInfo>();
        public string? GetComputedStyle(string id) => "{}";
        public bool ClickElement(string id) { LastClicked = id; return true; }
        public BoxModelInfo? GetBoxModel(string id) => null;
        public IReadOnlyList<LogEntryInfo> GetLogs(int n = 100) => new List<LogEntryInfo>();
    }
}
