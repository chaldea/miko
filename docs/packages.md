# Packages

Miko ships as a small set of NuGet packages. Reference only the ones you need: the core
engine, a component library, the platform host(s) for your targets, and (optionally) dev
tools and templates.

| Package | Description |
| --- | --- |
| [`Miko`](https://www.nuget.org/packages/Miko) | Core rendering engine: DOM, styling, layout, and painting. |
| [`Miko.Bootstrap`](https://www.nuget.org/packages/Miko.Bootstrap) | Bootstrap-style Razor component library and styles. |
| [`Miko.Ionic`](https://www.nuget.org/packages/Miko.Ionic) | Ionic-style Razor component library (tabs, side menu, icons). |
| [`Miko.DevTools`](https://www.nuget.org/packages/Miko.DevTools) | Runtime debugging tools for the DOM and layout tree. |
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

A typical desktop app references `Miko` (+ optional `Miko.Bootstrap` / `Miko.Ionic` /
`Miko.DevTools`), `Miko.Razor.Compiler`, and `Miko.Windowing`. A cross-platform app
references the platform host packages from each respective head project (see
[Project Structure](/guide/project-structure)).

## Getting the packages

The quickest path is the templates package, which scaffolds a project with the right
references already wired up:

```bash
dotnet new install Miko.Templates
dotnet new miko-razor -o MyApp
```

To add the engine to an existing project manually:

```bash
dotnet add package Miko
dotnet add package Miko.Windowing   # for a desktop window
```

::: warning Versioning
Miko is experimental; packages are published with development versions and APIs may change
between releases.
:::
