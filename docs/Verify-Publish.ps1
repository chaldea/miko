param(
    [Parameter(Mandatory = $true)][string] $SiteRoot,
    [string] $BasePath = '/'
)
$ErrorActionPreference = 'Stop'
$site = (Resolve-Path -LiteralPath $SiteRoot).Path
$info = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $site 'site-info.json') | ConvertFrom-Json
if ($info.base -ne $BasePath) { throw "Expected base '$BasePath', got '$($info.base)'." }
foreach ($file in @('index.html', '404.html', 'assets/miko.js', 'assets/highlight.js', 'assets/search-index.json', 'demo/index.html', 'demo/_content/Miko.Browser/miko-browser.js', 'demo/_framework/dotnet.js', '.nojekyll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $site $file) -PathType Leaf)) { throw "Missing $file" }
}
$homeHtml = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $site 'index.html')
if (-not $homeHtml.Contains('href="' + $BasePath + 'demo/"')) { throw 'The homepage must link to the demo under the deployment base.' }
$demoHtml = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $site 'demo/index.html')
if (-not $demoHtml.Contains('src="_content/Miko.Browser/miko-browser.js"')) { throw 'The demo must load its bootstrap relative to /demo/.' }
if ($demoHtml -match '(?:href|src)="/') { throw 'The demo must not use root-relative URLs; it is deployed under a base path.' }
$framework = Join-Path $site 'demo/_framework'
foreach ($pattern in @('dotnet.native*.wasm', 'dotnet.runtime*.js')) {
    if (-not (Get-ChildItem -LiteralPath $framework -Filter $pattern -File)) {
        throw "The WebAssembly runtime is incomplete: missing $pattern."
    }
}
if (Test-Path -LiteralPath (Join-Path $site '_framework')) { throw 'Demo runtime files leaked into the documentation root.' }
foreach ($page in (Get-ChildItem -LiteralPath $site -Filter '*.html' -Recurse | Where-Object { -not $_.FullName.StartsWith((Join-Path $site 'demo') + [IO.Path]::DirectorySeparatorChar) })) {
    $html = Get-Content -Raw -Encoding UTF8 -LiteralPath $page.FullName
    foreach ($match in [regex]::Matches($html, '(?:href|src)="(/[^"#?]*)(?:[?#][^"]*)?"')) {
        $url = [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
        if (-not $url.StartsWith($BasePath, [StringComparison]::Ordinal)) { throw "URL escaped deployment base in $($page.Name): $url" }
        $relative = [Uri]::UnescapeDataString($url.Substring($BasePath.Length))
        $target = Join-Path $site $relative
        if (Test-Path -LiteralPath $target -PathType Container) { $target = Join-Path $target 'index.html' }
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw "Broken local URL in $($page.Name): $url" }
    }
}
Write-Output "Verified documentation and WebAssembly deployment at $BasePath ($site)."
