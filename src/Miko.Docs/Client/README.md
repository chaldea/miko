# Embedded syntax highlighting

The original VitePress 1.6.4 site uses Shiki 2.5.0 with GitHub Light / Dark.
Miko.Docs uses the same tokenizer, Oniguruma WebAssembly engine and themes in
the browser. The generator emits escaped HTML first, so code remains readable
when JavaScript or WebAssembly is disabled. Highlighting preserves the existing
line elements, highlighted lines, line numbers, whitespace and clipboard text.

The committed ../Assets/highlight.js includes the engine, WASM binary, themes
and grammars. There are no CDN requests, dynamic language downloads or Node/npm
requirements for dotnet build, dotnet publish or deployed sites. The bundle
is approximately 1.9 MB before HTTP compression. Only grammars needed by the
current page are initialized. The site theme switches colors through CSS.

## Optional asset regeneration

Only maintainers changing the highlighter need Node.js/npm. From this directory:

    npm ci
    npm run build

Versions and transitive dependencies are locked in package-lock.json. Commit
the readable source, lockfile, regenerated asset and its notices together.
The .NET build never invokes these commands. The optional browser checks in
docs/Verify-Browser.mjs verify real C#/Razor pages, supported languages, light/dark
colors, multiline comments, escaped HTML, line highlighting, clipboard success,
clipboard denial/retry, and operation without external asset requests.

## Languages

Razor (razor, cshtml), C# (csharp, cs, c#), shell (bash, sh, shellscript, shell,
console, zsh), PowerShell (powershell, ps, ps1), XML, JSON/JSONC, HTML, CSS,
JavaScript (javascript, js), TypeScript (typescript, ts), JSX/TSX and YAML
(yaml, yml). Embedded grammar dependencies are included automatically.
Plain text, unlabelled fences and unknown languages remain escaped, copyable text.

## Third-party notices

Shiki, themes, grammars and VS Code TextMate: ../Assets/SHIKI-LICENSE.txt.
VS Code Oniguruma bindings: ../Assets/ONIGURUMA-LICENSE.txt.
Oniguruma: ../Assets/ONIGURUMA-NOTICES.txt.
The VitePress clipboard SVGs are Lucide icons: ../Assets/LUCIDE-LICENSE.txt.
All of these are embedded in the assembly and published alongside the JS asset.
