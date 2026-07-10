# Miko MCP Server

The Miko MCP server is built on the official
[ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)
C# SDK. It exposes the Simulator and DevTools debugging capabilities over the
**standard MCP protocol (Streamable HTTP)** for MCP clients such as Claude Code to
connect to. An AI agent can use it to read the DOM/styles, simulate clicks, switch
device/orientation, capture screenshots, and more, in real time.

> Implementation reference: [Build a Model Context Protocol (MCP) server in C#](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)

## Quick start

In the **simulator startup project** (`MyApp.Simulator`), call `AddMikoMcpServer()`
when creating the app context:

```csharp
using Miko.McpServer;
using Miko.Simulator;

var context = App.CreateContext(builder => builder.AddMikoMcpServer());
context.RunSimulator();
```

`App.CreateContext` accepts an optional `Action<MikoAppBuilder>` so the simulator head
can register MCP on its own, while the shared App assembly **does not reference**
`Miko.McpServer` (keeping each platform head's dependencies isolated).

Start the simulator:

```bash
dotnet run --project MyApp.Simulator/MyApp.Simulator.csproj
```

Besides the simulator window, it also starts the MCP service locally at
`http://localhost:5800`. The console prints a config snippet ready to paste into
`.mcp.json`.

### Configure Claude Code

Add `.mcp.json` at the project root:

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

Start Claude Code in that directory and it will recognize the `miko` MCP service.

## Why use the official SDK

MCP is a protocol built on top of JSON-RPC 2.0. When a client connects it first
performs the `initialize` handshake, then fetches tools via `tools/list`, and finally
invokes them via `tools/call`. A hand-rolled JSON-RPC implementation easily gets the
following details wrong, causing the client to report `invalid_union` /
`unrecognized_keys`:

- Fields must be **camelCase** (`jsonrpc`/`result`/`id`); PascalCase is rejected.
- The `initialize` handshake and protocol version negotiation must be implemented.
- `tools/list` must return tool definitions with a JSON Schema.
- The Streamable HTTP transport requires correct session/SSE semantics.

The official `ModelContextProtocol.AspNetCore` SDK handles all of these. Tools only
need to be annotated with the `[McpServerToolType]` / `[McpServerTool]` attributes; the
SDK discovers them via reflection and generates the schema.

## Options

`AddMikoMcpServer(configure)` lets you customize `MikoMcpOptions`:

| Option | Default | Description |
|--------|---------|-------------|
| `Port` | `5800` | Local port the MCP service listens on. |
| `LogBufferSize` | `1000` | Ring-buffer capacity for `devtools_get_logs` logs. |

```csharp
builder.AddMikoMcpServer(o =>
{
    o.Port = 5900;
    o.LogBufferSize = 500;
});
```

## MCP tools

### Simulator (8)

| Tool | Description | Parameters |
|------|-------------|------------|
| `simulator_get_current_device` | Current device info | — |
| `simulator_list_devices` | Available device list | — |
| `simulator_select_device` | Switch device (also switches platform) | `deviceName` |
| `simulator_get_orientation` | Current orientation | — |
| `simulator_set_orientation` | Set orientation | `orientation`: `Portrait`/`Landscape` |
| `simulator_get_safe_area` | Whether the safe area is enabled | — |
| `simulator_toggle_safe_area` | Enable/disable the safe area | `enabled` |
| `simulator_screenshot` | Screenshot (PNG base64 data URI) | — |

### DevTools (7)

| Tool | Description | Parameters |
|------|-------------|------------|
| `devtools_get_dom_tree` | Full DOM tree as JSON | — |
| `devtools_find_element` | Find an element by id | `id` |
| `devtools_query_elements` | Query elements by selector | `selector` (`tag`/`.class`/`#id`) |
| `devtools_get_computed_style` | Computed style as JSON | `elementId` |
| `devtools_get_box_model` | Box-model dimensions | `elementId` |
| `devtools_click_element` | Simulate a click | `elementId` |
| `devtools_get_logs` | Recent logs | `maxCount` (default 100) |

## Threading model

MCP requests arrive on ASP.NET's background HTTP threads, but Miko's DOM/layout/GPU
surface can only be touched safely on the simulator render thread. Therefore:

- `SimulatorHost.InvokeOnRenderThread(...)` posts the operation to a render-thread
  queue, which is drained at the start of each frame (`OnRender`) and blocks until the
  result is ready.
- `SimulatorService` state changes (switching device/orientation/safe area) and
  screenshots, as well as all DOM reads/writes in `DevToolsService` via `IDomAccessor`
  (adapted to the host by `SimulatorDomAccessor`), are marshaled through this mechanism
  to avoid racing with the render loop.

## Architecture

```
Miko.Simulator
├── ISimulatorService / SimulatorService   Simulator capability interface & implementation
├── ISimulatorHostObserver                 Host-ready callback (hook for McpServer)
└── SimulatorHost.InvokeOnRenderThread      Render-thread marshaling

Miko.DevTools
├── IDevToolsService / DevToolsService      DOM inspection interface & implementation
└── IDomAccessor                            Render-thread-safe engine access abstraction

Miko.McpServer
├── MikoMcpServerExtensions.AddMikoMcpServer Registration entry point + server launcher
├── MikoMcpOptions                           Options
├── SimulatorDomAccessor                     SimulatorHost → IDomAccessor adapter
├── McpLogCapture                            Independent log capture (no DevTools window needed)
└── Tools/
    ├── SimulatorTools                       [McpServerToolType] simulator tools
    └── DevToolsTools                        [McpServerToolType] DevTools tools
```

`Miko.Simulator`/`Miko.DevTools` only define capability interfaces and do not depend on
MCP; `Miko.McpServer` injects the interface implementations into its own Kestrel
container and exposes the tool types as MCP services via the official SDK's reflection.

## Tests

`tests/Miko.McpServer.Tests` uses the official MCP **client** to connect to an
in-process Kestrel host, covering the `initialize` handshake, `tools/list`, and
`tools/call` (read-only / with parameters / state changes / error paths). This is the
regression guard for protocol correctness.

```bash
dotnet test tests/Miko.McpServer.Tests/Miko.McpServer.Tests.csproj
```

## Notes

1. The default port is 5800; if it is in use, change it via `Port` and update the
   agent's MCP config accordingly.
2. The server runs on a background thread, in parallel with the simulator window,
   without blocking it.
