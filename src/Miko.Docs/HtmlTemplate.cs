// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Text;
using static Miko.Docs.MarkdownRenderer;

namespace Miko.Docs;

internal static class HtmlTemplate
{
    public static string Render(SitePage page, IReadOnlyList<SitePage> pages, SiteConfig config)
    {
        var theme = config.ThemeConfig;
        string L(string english, string chinese) => Localize(config, english, chinese);
        var sidebar = page.Content.Layout == "doc" && page.Content.Sidebar && theme.Sidebar.Count > 0;
        var outlineHeadings = page.Content.Headings.Where(h => h.Level >= 2 && h.Level <= theme.OutlineLevel).ToList();
        var outline = page.Content.Layout == "doc" && page.Content.Outline && outlineHeadings.Count > 0;
        var html = new StringBuilder($$"""
            <!doctype html>
            <html lang="{{E(config.Lang)}}">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="description" content="{{E(page.Content.Description)}}">
              <meta name="miko-base" content="{{E(config.Base)}}">
              <title>{{E(page.Content.Title == config.Title ? config.Title : page.Content.Title + " | " + config.Title)}}</title>
              <script>try{document.documentElement.classList.toggle('dark',(localStorage.getItem('miko-theme')|| (matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light'))==='dark')}catch{}</script>
              <link rel="stylesheet" href="{{E(config.Base)}}assets/vars.css">
              <link rel="preload" href="{{E(config.Base)}}assets/inter-roman-latin.woff2" as="font" type="font/woff2" crossorigin>
              <link rel="stylesheet" href="{{E(config.Base)}}assets/fonts.css">
              <link rel="stylesheet" href="{{E(config.Base)}}assets/base.css">
              <link rel="stylesheet" href="{{E(config.Base)}}assets/vp-doc.css">
              <link rel="stylesheet" href="{{E(config.Base)}}assets/custom-block.css">
              <link rel="stylesheet" href="{{E(config.Base)}}assets/miko.css">
              <script src="{{E(config.Base)}}assets/miko.js" defer></script>
              <script src="{{E(config.Base)}}assets/highlight.js" defer></script>
            </head>
            <body>
            <div id="app"><div class="Layout">
            <a class="VPSkipLink" href="#main-content">{{L("Skip to content", "跳转到内容")}}</a>
            <header class="VPNav"><div class="VPNavBar{{(sidebar ? " has-sidebar" : "")}}"><div class="container"><div class="nav-title">
            <a class="VPNavBarTitle" href="{{E(config.Base)}}">
            """);
        if (!string.IsNullOrEmpty(theme.Logo)) html.Append($"<img class=\"logo\" alt=\"\" src=\"{E(Urls.Resolve(theme.Logo, config))}\">");
        html.Append(E(config.Title)).Append("</a></div><div class=\"nav-content\"><div class=\"nav-content-body\"><div class=\"nav-search\">");
        if (theme.Search) html.Append($"<button type=\"button\" class=\"VPNavBarSearch\" id=\"search-open\" aria-label=\"{L("Search", "搜索文档")}\" aria-haspopup=\"dialog\"><span class=\"search-icon\" aria-hidden=\"true\"></span><span class=\"search-label\">{L("Search", "搜索文档")}</span><kbd>Ctrl K</kbd></button>");
        html.Append($"</div><nav class=\"VPNavBarMenu\" aria-label=\"{L("Main navigation", "主导航")}\">").Append(Navigation(theme.Nav, page, config, false)).Append("</nav>");
        if (theme.Appearance) html.Append($"<div class=\"nav-appearance\"><button type=\"button\" id=\"theme-toggle\" class=\"VPSwitchAppearance\" aria-label=\"{L("Switch to dark theme", "切换深色模式")}\" aria-pressed=\"false\"><span class=\"switch-check\"><span class=\"sun-icon\" aria-hidden=\"true\"></span><span class=\"moon-icon\" aria-hidden=\"true\"></span></span></button></div>");
        if (theme.SocialLinks.Count > 0)
        {
            html.Append("<div class=\"nav-social\">");
            foreach (var social in theme.SocialLinks)
                html.Append($"<a class=\"social-link\" href=\"{E(Urls.Safe(social.Link))}\" aria-label=\"{E(social.Icon == "github" ? "GitHub" : social.Icon)}\" target=\"_blank\" rel=\"noopener noreferrer\"><span class=\"{(social.Icon == "github" ? "github-icon" : "external-icon")}\" aria-hidden=\"true\"></span></a>");
            html.Append("</div>");
        }
        html.Append($"<button type=\"button\" id=\"menu-toggle\" aria-controls=\"mobile-nav\" aria-expanded=\"false\" aria-label=\"{L("Open navigation menu", "打开导航菜单")}\"><span class=\"menu-icon\" aria-hidden=\"true\"></span></button></div></div></div></div></header>");
        html.Append($"<nav id=\"mobile-nav\" class=\"mobile-nav\" aria-label=\"{L("Mobile navigation", "移动导航")}\" hidden>")
            .Append(Navigation(theme.Nav, page, config, false)).Append(Navigation(theme.Sidebar, page, config, true)).Append("</nav>");
        if (sidebar) html.Append($"<aside class=\"VPSidebar\"><nav class=\"nav\" aria-label=\"{L("Documentation navigation", "文档导航")}\">")
            .Append(Navigation(theme.Sidebar, page, config, true)).Append("</nav></aside>");
        html.Append($"<div class=\"VPContent{(sidebar ? " has-sidebar" : "")}{(page.Content.Layout == "home" ? " is-home" : "")}\" id=\"main-content\" tabindex=\"-1\">");
        if (page.Content.Layout == "home") html.Append(Home(page, config));
        else
        {
            html.Append($"<div class=\"VPDoc{(sidebar ? " has-sidebar" : "")}{(outline ? " has-aside" : "")}\"><div class=\"container\">");
            if (outline)
            {
                html.Append($"<aside class=\"aside\"><div class=\"aside-container\"><nav class=\"VPDocAsideOutline\" aria-label=\"{E(theme.OutlineTitle)}\"><div class=\"outline-title\">{E(theme.OutlineTitle)}</div><ul>");
                foreach (var heading in outlineHeadings)
                    html.Append($"<li class=\"level-{heading.Level}\"><a class=\"outline-link\" href=\"#{E(heading.Id)}\">{E(heading.Text)}</a></li>");
                html.Append("</ul></nav></div></aside>");
            }
            html.Append("<div class=\"content\"><div class=\"content-container\"><main class=\"main\"><div class=\"vp-doc\">")
                .Append(page.Content.Html).Append("</div></main>").Append(DocFooter(page, pages, config)).Append("</div></div></div></div>");
        }
        html.Append($"</div><footer class=\"VPFooter{(sidebar ? " has-sidebar" : "")}\"><p>{E(theme.Footer)}</p></footer>");
        if (theme.Search) html.Append($$"""
            <dialog id="search-dialog" class="VPLocalSearchBox" aria-labelledby="search-title">
              <form method="dialog"><label id="search-title" for="search-input">{{L("Search documentation", "搜索文档")}}</label><button class="search-close" aria-label="{{L("Close search", "关闭搜索")}}">Esc</button></form>
              <input id="search-input" type="search" placeholder="{{L("Search docs…", "输入关键词…")}}" autocomplete="off">
              <p id="search-status" role="status" aria-live="polite">{{L("Type to search the documentation", "输入关键词搜索文档")}}</p><ul id="search-results"></ul>
            </dialog>
            """);
        return html.Append("</div></div></body></html>\n").ToString();
    }

    private static string Localize(SiteConfig config, string english, string chinese) =>
        config.Lang.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? chinese : english;

    private static string Navigation(IEnumerable<NavItem> items, SitePage page, SiteConfig config, bool sidebar, int depth = 0)
    {
        var html = new StringBuilder();
        foreach (var item in items)
        {
            var link = item.Link is null ? null : Urls.Resolve(item.Link, config);
            var exact = link == page.Url;
            var active = exact || (!sidebar && link is not null && IsSameSection(link, page.Url, config.Base));
            var label = link is null ? $"<span class=\"text\">{E(item.Text)}</span>" : $"<a class=\"{(sidebar ? "VPLink link" : "VPNavBarMenuLink")}{(active ? " active" : "")}\" href=\"{E(link)}\"{(exact ? " aria-current=\"page\"" : "")}>{E(item.Text)}</a>";
            if (item.Items.Count > 0)
            {
                var hasActive = SiteConfig.Flatten(item.Items).Any(child => child.Link is not null && Urls.Resolve(child.Link, config) == page.Url);
                html.Append($"<details class=\"{(sidebar ? $"VPSidebarItem level-{depth}" : "VPNavBarMenuGroup")}\" data-nav-key=\"{E(item.Text)}\"{(sidebar && (!item.Collapsed || hasActive) ? " open" : "")}><summary><span class=\"text\">{E(item.Text)}</span><span class=\"caret-icon\" aria-hidden=\"true\"></span></summary>");
                if (link is not null) html.Append(label);
                html.Append("<div class=\"items\">").Append(Navigation(item.Items, page, config, sidebar, depth + 1)).Append("</div></details>");
            }
            else html.Append($"<div class=\"{(sidebar ? $"VPSidebarItem level-{depth}" : "nav-item")}\">{label}</div>");
        }
        return html.ToString();
    }

    private static bool IsSameSection(string link, string page, string basePath)
    {
        if (!link.StartsWith(basePath, StringComparison.Ordinal) || link == basePath) return false;
        var relative = link[basePath.Length..].TrimEnd('/');
        var slash = relative.LastIndexOf('/');
        return slash >= 0 && page.StartsWith(basePath + relative[..(slash + 1)], StringComparison.Ordinal);
    }

    private static string Home(SitePage page, SiteConfig config)
    {
        var home = config.Home;
        var html = new StringBuilder($"<main class=\"VPHome\"><section class=\"VPHero VPHomeHero\"><div class=\"container\"><div class=\"main\"><h1 class=\"name\">{E(home.Name ?? config.Title)}</h1><p class=\"text\">{E(home.Text ?? page.Content.Title)}</p><p class=\"tagline\">{E(home.Tagline ?? config.Description)}</p><div class=\"actions\">");
        foreach (var action in home.Actions)
            html.Append($"<a class=\"VPButton medium {(action.Theme == "alt" ? "alt" : "brand")}\" href=\"{E(Urls.Resolve(action.Link, config))}\">{E(action.Text)}</a>");
        html.Append("</div></div><div class=\"hero-mark\" aria-hidden=\"true\"><span>M.</span></div></div></section><section class=\"VPFeatures VPHomeFeatures\" aria-label=\"功能\"><div class=\"items\">");
        foreach (var feature in home.Features)
            html.Append($"<article class=\"VPFeature\"><div class=\"icon\">{E(feature.Icon)}</div><h2 class=\"title\">{E(feature.Title)}</h2><p class=\"details\">{E(feature.Details)}</p></article>");
        return html.Append("</div></section><div class=\"vp-doc home-content\">").Append(page.Content.Html).Append("</div></main>").ToString();
    }

    private static string DocFooter(SitePage page, IReadOnlyList<SitePage> pages, SiteConfig config)
    {
        var html = new StringBuilder("<footer class=\"VPDocFooter\">");
        if (!string.IsNullOrEmpty(config.ThemeConfig.EditLink))
        {
            var path = string.Join('/', page.Source.Split('/').Select(Uri.EscapeDataString));
            var edit = config.ThemeConfig.EditLink.Replace(":path", path, StringComparison.Ordinal);
            html.Append($"<a class=\"edit-link\" href=\"{E(Urls.Safe(edit))}\">{Localize(config, "Edit this page on GitHub", "编辑此页")} ↗</a>");
        }
        var links = SiteConfig.Flatten(config.ThemeConfig.Sidebar).Where(item => item.Link is not null)
            .Select(item => (item.Text, Url: Urls.Resolve(item.Link!, config))).DistinctBy(item => item.Url)
            .Where(item => pages.Any(p => p.Url == item.Url)).ToList();
        var index = links.FindIndex(item => item.Url == page.Url);
        html.Append($"<nav class=\"prev-next\" aria-label=\"{Localize(config, "Page navigation", "相邻页面")}\">");
        if (index > 0) html.Append($"<a class=\"pager-link prev\" href=\"{E(links[index - 1].Url)}\"><span class=\"desc\">{Localize(config, "Previous page", "上一页")}</span><span class=\"title\">{E(links[index - 1].Text)}</span></a>");
        if (index >= 0 && index + 1 < links.Count) html.Append($"<a class=\"pager-link next\" href=\"{E(links[index + 1].Url)}\"><span class=\"desc\">{Localize(config, "Next page", "下一页")}</span><span class=\"title\">{E(links[index + 1].Text)}</span></a>");
        return html.Append("</nav></footer>").ToString();
    }
}
