# Miko.McpServer

MCP (Model Context Protocol) debug server for [Miko](https://github.com/chaldea/miko).

`Miko.McpServer` exposes a running Miko app's **Simulator** and **DevTools**
capabilities over the standard [Model Context Protocol](https://modelcontextprotocol.io)
(Streamable HTTP), so AI agents such as **Claude Code** can inspect the DOM,
read computed styles, simulate clicks, switch devices, capture screenshots, and
more — while the app runs in the device simulator.

It is built on the official
[`ModelContextProtocol.AspNetCore`](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)
SDK, so the protocol (initialize handshake, `tools/list`, `tools/call`) is
implemented to spec.

## Usage

In the **simulator** startup project, enable the server when building the app
context. It only starts when you call `AddMikoMcpServer()`, so production heads
that never call it pay nothing:

```csharp
using Miko.McpServer;
using Miko.Simulator;

// Shared App.CreateContext takes an optional builder hook so only the
// simulator head references Miko.McpServer.
var context = App.CreateContext(builder => builder.AddMikoMcpServer());
context.RunSimulator();
```

Run the simulator; alongside the window it starts an MCP endpoint on
`http://localhost:5800`:

```bash
dotnet run --project MyApp.Simulator/MyApp.Simulator.csproj
```

Point an MCP client at it. For Claude Code, add `.mcp.json` at the repo root:

```json
{
  "mcpServers": {
    "miko": {
      "type": "http",
      "url": "http://localhost:5800"
    }
  }
}
```

## Tools

**Simulator** — `simulator_get_current_device`, `simulator_list_devices`,
`simulator_select_device`, `simulator_get_orientation`,
`simulator_set_orientation`, `simulator_get_safe_area`,
`simulator_toggle_safe_area`, `simulator_screenshot`.

**DevTools** — `devtools_get_dom_tree`, `devtools_find_element`,
`devtools_query_elements`, `devtools_get_computed_style`,
`devtools_get_box_model`, `devtools_click_element`, `devtools_get_logs`.

## Options

`AddMikoMcpServer(o => …)` accepts `MikoMcpOptions`:

| Option          | Default | Description                                   |
| --------------- | ------- | --------------------------------------------- |
| `Port`          | `5800`  | Local HTTP port the MCP endpoint listens on.  |
| `LogBufferSize` | `1000`  | Ring-buffer capacity for `devtools_get_logs`. |

See the [documentation](https://chaldea.github.io/miko/) for the full guide.
