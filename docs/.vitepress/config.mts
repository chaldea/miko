import { readdirSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vitepress'

// The Ionic component pages, read from disk so a new page shows up in the sidebar without
// editing this file. Titles come from each page's first `# ` heading.
const ionicDir = fileURLToPath(new URL('../components/ionic/', import.meta.url))
const ionicGuides = ['index', 'theming', 'overlays']
const ionicPage = (slug: string) => {
  const heading = readFileSync(`${ionicDir}${slug}.md`, 'utf8').match(/^#\s+(.+)$/m)
  return { text: heading ? heading[1].trim() : slug, link: `/components/ionic/${slug === 'index' ? '' : slug}` }
}
const ionicComponents = readdirSync(ionicDir)
  .filter((file) => file.endsWith('.md'))
  .map((file) => file.slice(0, -3))
  .filter((slug) => !ionicGuides.includes(slug))
  .map(ionicPage)
  .sort((a, b) => a.text.localeCompare(b.text))

// https://vitepress.dev/reference/site-config
//
// The site is published to GitHub Pages at https://chaldea.github.io/miko/,
// so `base` is the repository sub-path. Source pages are kept as plain Markdown to
// stay easy for both humans and AI agents to read and scrape.
export default defineConfig({
  lang: 'en-US',
  title: 'Miko',
  description:
    'A native, cross-platform UI rendering engine for .NET that uses Razor as its layout DSL and draws every pixel with SkiaSharp — no browser, no WebView.',

  base: '/miko/',
  cleanUrls: true,
  lastUpdated: true,

  // Emit a sitemap so search engines and AI crawlers can discover every page.
  sitemap: {
    hostname: 'https://chaldea.github.io/miko/'
  },

  head: [
    ['meta', { name: 'theme-color', content: '#512BD4' }],
    [
      'meta',
      {
        name: 'keywords',
        content:
          'Miko, .NET, SkiaSharp, Razor, UI rendering engine, cross-platform, native UI, Blazor, C#'
      }
    ]
  ],

  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'Guide', link: '/guide/introduction' },
      { text: 'Engine API', link: '/engine/overview' },
      { text: 'Examples', link: '/examples' },
      { text: 'Components', link: '/components/ionic/' },
      { text: 'Packages', link: '/packages' },
      {
        text: 'Resources',
        items: [
          { text: 'GitHub', link: 'https://github.com/chaldea/miko' },
          { text: 'NuGet', link: 'https://www.nuget.org/packages/Miko' }
        ]
      }
    ],

    sidebar: [
      {
        text: 'Introduction',
        collapsed: false,
        items: [
          { text: 'What is Miko?', link: '/guide/introduction' },
          { text: 'Getting Started', link: '/guide/getting-started' },
          { text: 'Project Structure', link: '/guide/project-structure' }
        ]
      },
      {
        text: 'Building UIs with Razor',
        collapsed: false,
        items: [
          { text: 'Razor Components', link: '/guide/razor-components' },
          { text: 'Elements', link: '/guide/elements' },
          { text: 'Styling', link: '/guide/styling' },
          { text: 'Layout', link: '/guide/layout' },
          { text: 'Fonts & Text', link: '/guide/fonts' },
          { text: 'Localization & RTL', link: '/guide/localization' },
          { text: 'Events', link: '/guide/events' },
          { text: 'Async & Lifecycle', link: '/guide/async' },
          { text: 'Native Capabilities', link: '/guide/native' }
        ]
      },
      {
        text: 'Publishing',
        collapsed: false,
        items: [
          { text: 'Desktop Apps', link: '/guide/publishing-desktop' },
          { text: 'Android Apps', link: '/guide/publishing-android' },
          { text: 'iOS Apps', link: '/guide/publishing-ios' }
        ]
      },
      {
        text: 'Ionic Components',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/components/ionic/' },
          ...ionicGuides.filter((slug) => slug !== 'index').map(ionicPage)
        ]
      },
      {
        text: 'Ionic Component Reference',
        collapsed: true,
        items: ionicComponents
      },
      {
        text: 'Rendering Engine',
        collapsed: false,
        items: [
          { text: 'Pipeline Overview', link: '/engine/overview' },
          { text: 'Using the Engine Directly', link: '/engine/direct-api' }
        ]
      },
      {
        text: 'Reference',
        collapsed: false,
        items: [
          { text: 'Examples', link: '/examples' },
          { text: 'Packages', link: '/packages' },
          { text: 'Component Libraries', link: '/components/' },
          { text: 'MCP Server', link: '/mcp-server' },
          { text: 'Known Limitations', link: '/guide/known-limitations' }
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/chaldea/miko' }
    ],

    search: {
      provider: 'local'
    },

    editLink: {
      pattern: 'https://github.com/chaldea/miko/edit/main/docs/:path',
      text: 'Edit this page on GitHub'
    },

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2025–present Miko contributors'
    }
  }
})
