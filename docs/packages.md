# Packages

Miko ships as a small set of NuGet packages. Reference only the ones you need: the core
engine, a component library, the platform host(s) for your targets, and (optionally) dev
tools and templates.

| Package | Description |
| --- | --- |
| [`Miko`](https://www.nuget.org/packages/Miko) | Core rendering engine: DOM, styling, layout, and painting. |
| [`Miko.Ionic`](https://www.nuget.org/packages/Miko.Ionic) | Ionic-style Razor component library (tabs, side menu, icons). |
| [`Miko.Native`](https://www.nuget.org/packages/Miko.Native) | **Preview** — Native capability interfaces (camera, clipboard, filesystem, notifications, …). APIs may change. |
| [`Miko.DevTools`](https://www.nuget.org/packages/Miko.DevTools) | Runtime debugging tools: DOM and layout tree, console, and resource inspector. Development only. |
| [`Miko.Simulator`](https://www.nuget.org/packages/Miko.Simulator) | Device simulator host for previewing mobile apps on desktop. Development only. |
| [`Miko.McpServer`](https://www.nuget.org/packages/Miko.McpServer) | MCP debug server that exposes a running app over localhost. Development only. |
| [`Miko.Components`](https://www.nuget.org/packages/Miko.Components) | **Preview** — Extended components (native video player, danmaku). APIs may change. |
| [`Miko.Testing`](https://www.nuget.org/packages/Miko.Testing) | **Preview** — Testing utilities. APIs may change. |
| [`Miko.Razor.Compiler`](https://www.nuget.org/packages/Miko.Razor.Compiler) | Source generator that compiles `.razor` components and routes. |
| [`Miko.Templates`](https://www.nuget.org/packages/Miko.Templates) | `dotnet new` templates for scaffolding Miko apps. |

## Platform host packages

The core `Miko` library is platform-agnostic. Each platform you target adds a thin host
package that owns the window / surface / native input:

| Package | Platform |
| --- | --- |
| `Miko.Windowing` | Desktop — Windows, Linux, macOS (Silk.NET). |
| `Miko.Android` | Android. |
| `Miko.iOS` | iOS. |

A typical desktop app references `Miko` (+ optional `Miko.Ionic` /
`Miko.DevTools`), `Miko.Razor.Compiler`, and `Miko.Windowing`. A cross-platform app
references the platform host packages from each respective head project (see
[Project Structure](./guide/project-structure.md)).

## Getting the packages

The quickest path is the templates package, which scaffolds a project with the right
references already wired up:

```bash
dotnet new install Miko.Templates::1.0.0-preview.1
dotnet new miko-razor -o MyApp
```

To add the engine to an existing project manually:

```bash
dotnet add package Miko --prerelease
dotnet add package Miko.Windowing --prerelease   # for a desktop window
```

::: warning 1.0.0-preview.1
This is a preview release. Packages must be installed with explicit version numbers (`::1.0.0-preview.1`) or the `--prerelease` flag. APIs may change before the stable 1.0 release.
:::
