# Miko.Docs

Miko.Docs is a .NET static documentation generator with a VitePress-style interface. Write Markdown, configure your site in JSON, and build, preview, and publish it with the .NET CLI.

The generator produces HTML, CSS, JavaScript, and a local search index. Building and serving documentation requires no Node.js, npm, Vue runtime, or third-party NuGet packages beyond Miko.Docs itself.

## Contents

- [Features](#features)
- [Requirements](#requirements)
- [Quick start](#quick-start)
- [Create a documentation project](#create-a-documentation-project)
- [Configuration reference](#configuration-reference)
- [Writing documentation](#writing-documentation)
- [Development server](#development-server)
- [Build and deployment](#build-and-deployment)
- [Command-line reference](#command-line-reference)
- [MSBuild integration](#msbuild-integration)
- [Programmatic API](#programmatic-api)
- [Development and verification](#development-and-verification)
- [Troubleshooting](#troubleshooting)
- [Compatibility and limitations](#compatibility-and-limitations)
- [License](#license)

## Features

- A C# Markdown renderer with headings, tables, nested lists, task lists, code fences, and custom containers.
- A responsive documentation layout with nested navigation, a page outline, and previous/next links.
- Configurable home pages with a hero section, action buttons, and feature cards.
- Light and dark appearance, code copying, and browser-side full-text search.
- Automatic site generation during `dotnet build` and `dotnet publish`.
- Local preview with file watching, browser refresh, and build-error reporting.
- Deployment under a root path or a subdirectory, with directory-style or `.html` URLs.

## Requirements

| Task | Requirements |
| --- | --- |
| Build documentation or run from source | .NET 11 SDK |
| Run a framework-dependent published preview application | .NET 11 and ASP.NET Core 11 runtimes |
| Host generated static files | An HTTP server or static hosting service; no .NET runtime required |
| Run optional browser verification | Node.js, an existing Playwright Core installation, and a compatible browser |

The repository's `global.json` selects the .NET 11 SDK family. The preview server uses the ASP.NET Core shared framework. Node.js is only used by optional contributor verification scripts.

## Quick start

From the repository root:

```sh
dotnet build
dotnet run --project docs/Miko.Docs.Web
```

Open `http://localhost:5173`. The sample includes a home page, introductory guides, Markdown examples, configuration documentation, and deployment instructions.

For development with C# change tracking:

```sh
dotnet watch --project docs/Miko.Docs.Web
```

To generate a deployable site:

```sh
dotnet publish docs/Miko.Docs.Web -c Release -o artifacts/site
```

The generated site is in `artifacts/site/wwwroot/`.

## Create a documentation project

### 1. Build the package

From the Miko repository root:

```sh
dotnet pack src/Miko.Docs -c Release -o artifacts/packages
```

This creates `artifacts/packages/Miko.Docs.1.0.0-dev.nupkg`. The installation steps below use this local package directory.

### 2. Create a console project

From the directory where you want to create your documentation project:

```sh
dotnet new console -n MyDocs -f net11.0
cd MyDocs
dotnet add package Miko.Docs --version 1.0.0-dev --source "/absolute/path/to/miko-v5/artifacts/packages"
```

Replace the source path with your local package directory. On Windows, an absolute path such as `G:/projects/claude/miko-v5/artifacts/packages` also works. Keep the package source available for future restores, or configure a persistent NuGet feed.

### 3. Configure the application entry point

Replace `Program.cs` with:

```csharp
using Miko.Docs;

return await DocsApplication.RunAsync(args);
```

### 4. Add content

Create `docs/index.md`:

```markdown
# My Documentation

Welcome to the project documentation.
```

Optionally create `miko.docs.json` alongside the project file:

```json
{
  "title": "My Documentation",
  "description": "Guides and reference for my project.",
  "lang": "en",
  "themeConfig": {
    "outlineTitle": "On this page",
    "outlineLevel": 2,
    "socialLinks": [{ "icon": "github", "link": "https://github.com/example/project" }],
    "footer": "Copyright 2026 My Project"
  }
}
```

A typical project looks like this:

```text
MyDocs/
  MyDocs.csproj
  Program.cs
  miko.docs.json
  docs/
    index.md
    guide/
      getting-started.md
    public/
      logo.svg
```

### 5. Build, preview, and publish

Run these commands from the documentation project directory:

```sh
dotnet build
dotnet run
dotnet publish -c Release -o publish
```

The package imports its MSBuild targets automatically. A package consumer does not need a manual `<Import>` or a separate generation command. The repository sample uses a project reference and an explicit targets import so it can run directly from source.

## Configuration reference

Configuration is loaded from `miko.docs.json` in the documentation project directory. The file is optional: omitted properties use their defaults. JSON comments and trailing commas are supported. Property names are case-insensitive; examples use camel case.

The following example includes every configurable property. It assumes that `docs/guide/getting-started.md`, `docs/guide/markdown.md`, `docs/public/logo.svg`, and `docs/public/apple-touch-icon.png` exist.

```json
{
  "title": "My Documentation",
  "description": "Guides and reference for my project.",
  "lang": "en",
  "base": "/",
  "srcDir": "docs",
  "cleanUrls": true,
  "favicon": "/logo.svg",
  "appleTouchIcon": "/apple-touch-icon.png",
  "themeConfig": {
    "logo": "/logo.svg",
    "nav": [
      { "text": "Guide", "link": "/guide/getting-started" }
    ],
    "sidebar": [
      {
        "text": "Guide",
        "collapsed": false,
        "items": [
          { "text": "Getting started", "link": "/guide/getting-started" },
          { "text": "Markdown", "link": "/guide/markdown" }
        ]
      }
    ],
    "outlineTitle": "On this page",
    "footer": "Copyright 2026 My Project",
    "editLink": "https://github.com/example/docs/edit/main/docs/:path",
    "search": true,
    "appearance": true
  },
  "home": {
    "name": "My Project",
    "text": "Documentation for .NET developers",
    "tagline": "Everything you need to get started.",
    "actions": [
      { "text": "Get started", "link": "/guide/getting-started", "theme": "brand" },
      { "text": "Markdown reference", "link": "/guide/markdown", "theme": "alt" }
    ],
    "features": [
      {
        "icon": "M",
        "title": "Markdown content",
        "details": "Write documentation alongside your application code."
      }
    ]
  }
}
```

Replace the illustrative edit-link URL with your repository URL, or omit `editLink`.

### Site options

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `title` | string | `"Miko Docs"` | Site name in the navigation bar and browser title. |
| `description` | string | `""` | Default page description and fallback home-page tagline. |
| `lang` | string | `"zh-CN"` | HTML language and built-in interface language: Chinese for `zh`, English otherwise. |
| `base` | string | `"/"` | Deployment prefix, such as `/` or `/manual/`. |
| `srcDir` | string | `"docs"` | Documentation directory relative to the project directory. |
| `cleanUrls` | boolean | `true` | Generate directory-style URLs; use `false` for `.html` pages. |
| `favicon` | string or null | `null` | Browser icon URL (SVG, PNG or ICO). Local paths receive the configured `base` prefix. |
| `appleTouchIcon` | string or null | `null` | Apple home-screen icon URL, usually a 180 px opaque PNG. Local paths receive the configured `base` prefix. |
| `themeConfig` | object | Default theme settings | Navigation, sidebar, outline label, footer, search, and appearance settings. |
| `home` | object | Default home settings | Hero content, actions, and feature cards for `layout: home` pages. |

`base` must start and end with `/`. Each path segment may contain letters, digits, underscores, or hyphens. It is a path prefix, not a hostname or full URL.

`srcDir` must identify a directory inside the project. Absolute paths and `..` segments are rejected. Supply configuration objects and arrays with the types shown above; use empty arrays rather than `null`.

Built-in controls use Chinese when `lang` starts with `zh`, and English otherwise. Navigation text, the outline title, the footer, and home content remain configurable. There is no locale-bundle configuration.

### Theme options

These properties belong to `themeConfig`.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `logo` | string or null | `null` | Image URL displayed beside the site title. Local paths receive the configured `base` prefix. |
| `nav` | array of navigation items | `[]` | Header navigation. Items with children render as expandable groups. |
| `sidebar` | array of navigation items | `[]` | Documentation navigation, including nested groups. Also determines previous/next page order. |
| `outlineTitle` | string | `"\u672c\u9875\u76ee\u5f55"` | Outline heading; the default Chinese label means "On this page". |
| `outlineLevel` | integer | `2` | Maximum heading level in the outline: `2` or `3`. |
| `socialLinks` | array | `[]` | Header links with `icon` and `link`; `github` uses the GitHub icon. |
| `footer` | string | `"Built with Miko.Docs"` | Footer text, rendered as escaped plain text. |
| `editLink` | string or null | `null` | Edit URL template. Each `:path` is replaced with the page's source path relative to `srcDir`, with URL-encoded path segments. |
| `search` | boolean | `true` | Enable the search dialog and populate the local search index. |
| `appearance` | boolean | `true` | Show the light/dark appearance switch. |

The outline contains second-level headings by default; set `outlineLevel` to `3` to include third-level headings. It is shown in the wide-screen layout. Search runs in the browser, matches page titles and text, supports multiple whitespace-separated terms, and returns up to 20 results. Open it with Ctrl+K or Command+K.

The appearance setting controls the switch's visibility. Setting it to `false` does not force light mode: the theme still uses a saved preference or the browser's preferred color scheme.

### Navigation items

Both `themeConfig.nav` and `themeConfig.sidebar` use the same item structure.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `text` | string | `""` | Display label. |
| `link` | string or null | `null` | Destination URL. A group may also have its own destination. |
| `items` | array of navigation items | `[]` | Nested links or groups. |
| `collapsed` | boolean | `false` | Initially collapse a sidebar group. Groups containing the active page open automatically. |

`collapsed` applies to sidebar groups. Header groups use their own expandable presentation.

Use source-relative site paths such as `/guide/getting-started` or `/guide/getting-started.md`. Do not add the deployment prefix manually. Previous/next links follow the sidebar's nested item order, omit destinations without a generated page, and remove duplicate destinations.

### Home-page options

To enable the home layout, start `docs/index.md` with:

```yaml
---
layout: home
title: Welcome
---
```

Hero and feature data belong in the `home` object of `miko.docs.json`, not in nested YAML front matter. Markdown after the front matter is displayed below the home sections.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `name` | string or null | `null` | Hero name. Falls back to the site `title`. |
| `text` | string or null | `null` | Main hero message. Falls back to the page title. |
| `tagline` | string or null | `null` | Supporting text. Falls back to the site `description`. |
| `actions` | array of actions | `[]` | Hero action buttons. |
| `features` | array of features | `[]` | Feature cards below the hero. |

Each action supports:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `text` | string | `""` | Button label. |
| `link` | string | `"/"` | Button destination; must not be empty. |
| `theme` | string | `"brand"` | Use `"brand"` for the primary style or `"alt"` for the secondary style. Other values use the primary style. |

Each feature supports:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `icon` | string | `"\u2726"` | A text symbol or emoji. It is rendered as text, not as an image URL or HTML. |
| `title` | string | `""` | Feature heading. |
| `details` | string | `""` | Feature description, rendered as plain text. |

## Writing documentation

### Page metadata

Front matter must begin at the start of the file and end with a matching `---` line. Only the following scalar fields are supported:

```yaml
---
title: Getting started
description: Install the package and create your first page.
layout: doc
sidebar: true
outline: true
---

# Getting started
```

| Field | Default | Description |
| --- | --- | --- |
| `title` | First H1 heading, then the source filename without its extension | Page title used by the HTML template and search index. |
| `description` | Site `description` | Page-specific description metadata. |
| `layout` | `doc` | `doc`, `home`, or `page`. |
| `sidebar` | `true` | Set to lowercase `false` to hide the document sidebar for this page. |
| `outline` | `true` | Set to lowercase `false` to omit the page outline. |

`doc` renders the documentation layout. `home` renders the configured hero and features. `page` renders the document content without the document sidebar or outline. Global navigation remains available.

Single-quoted strings, JSON-style double-quoted strings, and unquoted scalar values are supported. Unknown front matter fields cause a build error. This is a limited front matter parser, not a general YAML parser; nested objects, arrays, and multiline YAML values are not supported.

### Supported Markdown

| Area | Supported syntax |
| --- | --- |
| Headings | ATX (`#`) and Setext headings, Unicode anchors, unique anchors for repeated headings, explicit `{#custom-id}` anchors |
| Inline formatting | Bold, italic, bold italic, strikethrough, inline code, escaped punctuation, HTML entities |
| Blocks | Paragraphs, hard line breaks, horizontal rules, block quotes |
| Lists | Ordered, unordered, nested, and task lists |
| Tables | Pipe tables with left, center, and right alignment |
| Links and images | Inline and reference links, images, explicit URL autolinks, query strings, and fragments |
| Code | Backtick or tilde fences, indented code, local Shiki syntax highlighting, language labels, copying, highlighted lines, and optional line numbers |
| Containers | `info`, `note`, `tip`, `important`, `warning`, `danger`, `caution`, and `details` |

Code-block example:

````markdown
```csharp {2-3}:line-numbers
using Miko.Docs;
var builder = new SiteBuilder();
builder.Build(".", "bin/site");
```
````

`{2-3}` highlights a range of lines; `{1,3-5}` combines individual lines and ranges. `:line-numbers` enables line numbers, and `:no-line-numbers` disables them. Code is initially rendered as escaped text, then highlighted in the browser with the embedded Shiki 2.5.0 engine and the original VitePress GitHub Light / Dark themes. The copy button uses the original SVG clipboard/checkmark icons and the browser Clipboard API, which requires a secure context such as HTTPS or localhost.

The local asset includes Oniguruma WebAssembly, both themes and grammars for Razor, C#, Bash/shell, PowerShell, XML, JSON/JSONC, HTML, CSS, JavaScript/TypeScript, JSX/TSX and YAML, including common aliases. No CDN or runtime language download is required. Unknown languages and plain text remain readable and copyable; line highlighting, line numbers and whitespace are preserved. JavaScript and WebAssembly are required for token colors, but code remains readable when they are disabled. Normal .NET builds and publishing use the committed embedded resource and do not require Node/npm. See Client/README.md for optional maintainer regeneration instructions.

Custom-container example:

```markdown
::: tip Start here
Read the [getting started guide](./guide/getting-started.md).
:::

::: details Additional information
Container bodies support **Markdown** and nested containers.
:::
```

An omitted container title defaults to the uppercase container name. Every custom container must have a closing `:::` delimiter.

### Links and generated paths

With the default `base: "/"`:

| Source file | Output with `cleanUrls: true` | Output with `cleanUrls: false` |
| --- | --- | --- |
| `docs/index.md` | `index.html` | `index.html` |
| `docs/guide/start.md` | `guide/start/index.html` | `guide/start.html` |
| `docs/guide/index.md` | `guide/index.html` | `guide/index.html` |
| `docs/404.md` | `404.html` | `404.html` |

A link from `docs/guide/start.md` to `../reference/configuration.md#options` becomes `/reference/configuration/#options` when clean URLs are enabled. With `base: "/manual/"`, it becomes `/manual/reference/configuration/#options`.

Relative Markdown links are resolved from the source page's directory. Root-relative links are resolved from the site root. Query strings and fragments are retained. External `http`, `https`, `mailto`, and `tel` URLs are accepted; protocol-relative URLs and unsupported schemes are rejected. Use matching filename case for portable deployments.

### Static assets and error pages

- Files in `docs/public/` are copied to the output root. For example, `docs/public/logo.svg` is referenced as `/logo.svg`.
- Non-Markdown files elsewhere under `srcDir` keep their relative directory structure.
- Names beginning with `.` and directories named `bin`, `obj`, or `node_modules` are excluded.
- Symbolic links and reparse points in build paths or documentation sources are not supported.
- A root-level `404.md` supplies a custom error page. Otherwise, the generator creates `404.html`.

The generator owns its theme files under `assets/`, `assets/search-index.json`, `site-info.json`, and `.miko-manifest.json`. Do not place conflicting files in `public/` or use the `__miko/` prefix. Custom CSS and JavaScript files are copied like other assets but are not automatically injected into pages.

## Development server

From a documentation project:

```sh
dotnet run
```

The server listens on `127.0.0.1:5173` by default. When sources are available, it builds the site, watches source content and `miko.docs.json`, and refreshes connected browsers after successful changes. Changes are debounced, and the browser checks for updates every 750 milliseconds. Build errors are reported in the terminal and an in-page overlay. Input validation failures leave the previously generated site available.

For application-code changes as well as content changes:

```sh
dotnet watch
```

In the default Hot Reload mode, Miko.Docs handles documentation rebuilding while `dotnet watch` handles C# updates. To restart the application on watched changes instead:

```sh
dotnet watch --no-hot-reload
```

Changing `base` requires a server restart. Press Ctrl+R in `dotnet watch`, or stop and restart `dotnet run`.

Additional examples:

```sh
# Use another port.
dotnet run -- --port 8080

# Listen on all IPv4 interfaces.
dotnet run -- --host 0.0.0.0 --port 8080

# Build once and serve without the internal file watcher or live reload.
dotnet run -- --no-watch

# Show the generator's command-line help.
dotnet run -- help
```

`--no-watch` affects the Miko.Docs watcher; it does not disable an outer `dotnet watch` process. The preview server is intended for development. Use a static hosting service for production deployment.

## Build and deployment

### Generate files during a build

```sh
dotnet build
```

For a standard Debug build of a `net11.0` project, output is generated in `bin/Debug/net11.0/wwwroot/`. The actual default follows MSBuild's target directory, so configuration and runtime-specific output paths are respected.

To run the generator explicitly:

```sh
dotnet run -- build --output bin/site
```

This is an application command, separate from MSBuild's automatic generation. Without `--output`, this command writes to `<project>/bin/docs/`. `dotnet run` also performs its normal project build unless `--no-build` is specified.

### Publish

```sh
dotnet publish -c Release -o publish
```

Deploy the contents of `publish/wwwroot/`. The published directory also includes the .NET preview application; those application files are unnecessary for static hosting.

The publish target regenerates documentation even with `--no-build`, provided the application has already been built for the selected configuration:

```sh
dotnet build -c Release
dotnet publish -c Release --no-build -o publish
```

For a source-free preview of a project named `MyDocs`, run from the published directory:

```sh
cd publish
dotnet MyDocs.dll --port 8080
```

When the configured source directory is absent, the server serves the existing `wwwroot` beside the application and reads its deployment prefix from `site-info.json`.

### Deploy under a subdirectory

Set the prefix before generating the site:

```json
{
  "base": "/manual/"
}
```

Publish again, then map the contents of `wwwroot/` to the host's `/manual/` path. `base` changes generated URLs; it does not create an extra `manual` directory in the build output.

Clean URLs use paths such as `/manual/guide/start/`, backed by `guide/start/index.html`. Configure the host to serve directory indexes. With `cleanUrls: false`, links use `.html` filenames. Configure the host's custom 404 handling separately if it should use the generated `404.html`.

### Output ownership

The output directory must be separate from the Markdown source tree. It cannot be inside the source directory or contain the source/project directory. Output collisions, such as `guide.md` and `guide/index.md` both generating `guide/index.html`, cause a build error.

The generator records files in `.miko-manifest.json`. Later builds remove stale generated files without deleting unrelated files merely because they are absent from the source tree. The manifest is excluded from publish output. When replacing an already deployed site, use a clean deployment or a synchronization method that removes obsolete files; MSBuild publishing does not clean an existing remote deployment.

## Command-line reference

Pass application arguments after the `--` separator in `dotnet run`. Place the command before its options.

| Command | Behavior |
| --- | --- |
| `serve` | Build available sources and start the preview server. This is the default when no command is supplied. |
| `build` | Generate the site and exit. Does not start a server. |
| `help` | Print usage information and exit. |

| Option | Default | Description |
| --- | --- | --- |
| --base &lt;path&gt; | Configured site base | Override the base while building or serving sources, without editing JSON. |
| `--project <directory>` | Current working directory | Documentation project directory containing `miko.docs.json` and `srcDir`. |
| `--output <directory>` | Depends on command | Output directory. Relative paths are resolved against the selected project directory. |
| `--host <address>` | `127.0.0.1` | Preview bind address: `localhost`, an IPv4 address, or an IPv6 address. Arbitrary hostnames are not supported. |
| `--port <number>` | `5173` | Preview port, from `1` through `65535`. |
| `--no-watch` | Watching enabled | Disable the internal file watcher and browser refresh. |
| `--help`, `-h` | Not set | Display help. |

For `build`, the default output is `<project>/bin/docs/`. For `serve`, it is `wwwroot/` beside the running application. Host, port, and watcher settings affect serving only.

The two `--project` options below belong to different command parsers:

```sh
dotnet run --project docs/Miko.Docs.Web -- --project "/path/to/documentation-project" --output "/path/to/generated-site"
```

Before `--`, `--project` selects the .NET project to run. After `--`, it selects the content and configuration directory for Miko.Docs.

The CLI returns `0` on successful completion and `1` for handled configuration, input, and build errors.

## MSBuild integration

The NuGet package includes `buildTransitive/Miko.Docs.targets` and a bundled generator executable. Package consumers import these targets automatically.

| Property | Default | Description |
| --- | --- | --- |
| `MikoDocsEnabled` | `true` | Enable automatic generation, publish integration, and documentation Watch items. |
| `MikoDocsOutputDirectory` | `$(TargetDir)wwwroot` | Build output directory. Relative values are resolved against `$(MSBuildProjectDirectory)`. |
| `MikoDocsBasePath` | Configured site base | Override the deployment prefix during build/publish without editing JSON. |
| `MikoDocsToolPath` | Package's `tools/net11.0/any/Miko.Docs.dll` | Override the generator DLL, primarily for source development. |
| `MikoDocsDotNetHost` | `dotnet` | Executable used to run the generator. |
| `RunWorkingDirectory` | `$(MSBuildProjectDirectory)` when unset | Working directory used by `dotnet run`, allowing source discovery when invoked from another directory. |

Set properties in the consuming project:

```xml
<PropertyGroup>
  <MikoDocsOutputDirectory>$(MSBuildProjectDirectory)/bin/site</MikoDocsOutputDirectory>
</PropertyGroup>
```

Or override them for an individual invocation:

```sh
dotnet build -p:MikoDocsEnabled=false
dotnet build -p:MikoDocsOutputDirectory=bin/site
```

`MikoDocsGenerate` runs after `Build`, except for design-time and cross-targeting outer builds. `MikoDocsPublish` runs before `ComputeFilesToPublish`, depends on generation, and adds the resulting files under `wwwroot/` in the publish output.

Watch items include `.md`, `.json`, `.css`, `.js`, `.svg`, and `.png` files in the project, excluding `bin`, `obj`, and an explicitly configured output directory. The server's own watcher observes source-tree changes independently.

MSBuild properties do not change application CLI defaults. If you configure a custom build output directory and want preview to use it, pass the corresponding `--output` argument. Setting `MikoDocsEnabled=false` disables automatic MSBuild actions, but does not disable generation through `dotnet run` or the C# API.

## Programmatic API

### Build a complete site

```csharp
using Miko.Docs;

var projectDirectory = Path.GetFullPath("MyDocs");
var outputDirectory = Path.Combine(projectDirectory, "bin", "site");

BuildResult result = new SiteBuilder().Build(projectDirectory, outputDirectory);
Console.WriteLine($"Generated {result.PageCount} pages in {result.OutputDirectory}");
```

`BuildResult` exposes `OutputDirectory`, `PageCount`, and the loaded `Config`. `PageCount` counts source Markdown pages; it does not count an automatically generated fallback 404 page.

### Render a Markdown fragment

```csharp
using Miko.Docs;

var config = new SiteConfig
{
    Title = "My Documentation",
    Base = "/manual/",
    CleanUrls = true
};
config.Validate();

MarkdownPage page = new MarkdownRenderer().Render(
    "# Getting started\n\nRead the [reference](../reference.md).",
    config,
    sourcePath: "guide/start.md");

Console.WriteLine(page.Html);
```

`MarkdownPage` exposes `Html`, `Title`, `Description`, `Layout`, `Sidebar`, `Outline`, `Headings`, and `PlainText`. Each heading contains its `Level`, `Id`, and `Text`. The renderer produces a content fragment; use `SiteBuilder` for full HTML pages and theme assets.

`SiteConfig.Load(projectDirectory)` loads and validates the JSON configuration. `DocsApplication.RunAsync(args)` provides the command-line build and preview entry point. The `SiteBuilder` API resolves relative directory arguments against the process working directory; the CLI resolves a relative `--output` against its selected project directory.

## Development and verification

The generator lives in src/Miko.Docs, the site in docs/Miko.Docs.Web, and generator
regressions live in tests/Miko.Docs.Tests (documentation example and template checks are in tests/Miko.Docs.Demo.Tests).
See docs/README.md for combined static documentation and WebAssembly demo hosting.

~~~sh
dotnet test tests/Miko.Docs.Tests
dotnet publish docs/Miko.Docs.Web -c Release -o artifacts/docs
pwsh -File docs/Verify-Publish.ps1 -SiteRoot artifacts/docs/wwwroot
~~~

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| Documentation directory not found | Confirm `srcDir` exists beneath the selected project directory. Use the application `--project` option when running a DLL from another location. |
| No Markdown pages found | Add at least one `.md` page outside `public/` and excluded directories. Add `index.md` to provide a root landing page. |
| Output collision | Check for duplicate routes, such as `guide.md` alongside `guide/index.md`, or public files that overlap generated assets. |
| Front matter build error | Use only the documented scalar fields and close the block with `---`. Put home-page data in JSON. |
| Links or assets fail after subdirectory deployment | Set `base` before building, deploy to the same prefix, and avoid manually adding that prefix to source links. |
| Preview reports that `base` changed | Restart the server; press Ctrl+R when using `dotnet watch`. |
| Preview says it is serving static output | The selected project directory has no configured source tree. Confirm the application working directory or pass `--project`. |
| Custom build directory is not used by preview | Pass `--output` to the application as well as setting `MikoDocsOutputDirectory` for MSBuild. |

## Compatibility and limitations

Miko.Docs adopts the VitePress default theme's styling and structural class names, but it is not a drop-in VitePress implementation.

The current implementation does not support Vue components, executable raw HTML in Markdown, JavaScript/TypeScript configuration files, VitePress plugins, full YAML, math rendering, footnotes, or code groups. It does not claim full CommonMark compatibility. HTML in Markdown and text-valued configuration fields is escaped.

The home hero includes a built-in decorative mark; there is no hero-image option. Custom theme asset injection and a general localization system are not currently exposed as configuration.

## License

Miko.Docs is released under the MIT License. See `LICENSE` for the full text.

The theme includes styles from VitePress and Tailwind CSS preflight rules. Attribution and license text are recorded in `THIRD-PARTY-NOTICES.md`, included in the NuGet package, and copied into generated sites as `assets/THIRD-PARTY-NOTICES.txt`.
