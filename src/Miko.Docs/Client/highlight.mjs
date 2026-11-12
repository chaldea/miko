// Browser entry point. Regenerate Assets/highlight.js with npm ci && npm run build here.
import { createHighlighterCore } from 'shiki/core';
import { createOnigurumaEngine } from 'shiki/engine/oniguruma';
import wasm from 'shiki/wasm';
import light from '@shikijs/themes/github-light';
import dark from '@shikijs/themes/github-dark';
import razor from '@shikijs/langs/razor';
import csharp from '@shikijs/langs/csharp';
import shellscript from '@shikijs/langs/shellscript';
import powershell from '@shikijs/langs/powershell';
import xml from '@shikijs/langs/xml';
import json from '@shikijs/langs/json';
import jsonc from '@shikijs/langs/jsonc';
import html from '@shikijs/langs/html';
import css from '@shikijs/langs/css';
import javascript from '@shikijs/langs/javascript';
import typescript from '@shikijs/langs/typescript';
import jsx from '@shikijs/langs/jsx';
import tsx from '@shikijs/langs/tsx';
import yaml from '@shikijs/langs/yaml';

const languages = new Map();
for (const group of [razor, csharp, shellscript, powershell, xml, json, jsonc, html, css, javascript, typescript, jsx, tsx, yaml]) {
  // Each module includes its embedded grammars (e.g. Razor -> HTML/C#/CSS/JS).
  for (const grammar of group) {
    for (const name of [grammar.name, ...(grammar.aliases ?? [])]) {
      languages.set(name, { name: grammar.name, group });
    }
  }
}
for (const [alias, name] of Object.entries({ cshtml: 'razor', ps1: 'powershell', shell: 'shellscript', console: 'shellscript' })) {
  languages.set(alias, languages.get(name));
}

async function highlight() {
  const blocks = [...document.querySelectorAll('.vp-doc div[class*="language-"]')].map(block => {
    const label = [...block.classList].find(name => name.startsWith('language-'))?.slice(9).toLowerCase();
    return { code: block.querySelector('pre > code'), language: languages.get(label) };
  }).filter(({ code, language }) => code && language);
  if (!blocks.length) return;

  const highlighter = await createHighlighterCore({
    themes: [light, dark],
    langs: [...new Set(blocks.flatMap(({ language }) => language.group))],
    // The WASM binary, themes and grammars are all in this local asset. No CDN/fetch.
    engine: createOnigurumaEngine(wasm)
  });
  try {
    for (const { code, language } of blocks) {
      try {
        const lines = [...code.querySelectorAll(':scope > .line')];
        // Tokenize the whole block so multiline strings/comments retain their state.
        const { tokens } = highlighter.codeToTokens(code.textContent, {
          lang: language.name, themes: { light: 'github-light', dark: 'github-dark' }
        });
        // Keep the escaped server HTML if tokenization cannot preserve the exact text.
        if (!lines.length || lines.some((line, i) => tokens[i]?.map(token => token.content).join('') !== line.textContent)) continue;
        for (const [i, line] of lines.entries()) {
          const content = document.createDocumentFragment();
          for (const token of tokens[i]) {
            const span = document.createElement('span');
            span.textContent = token.content;
            for (const [property, value] of Object.entries(token.htmlStyle ?? {})) span.style.setProperty(property, value);
            content.append(span);
          }
          // Keep .line/.highlighted and the newline text nodes for line numbers/copy.
          line.replaceChildren(content);
        }
        code.classList.add('shiki');
        code.dataset.highlighted = 'shiki';
      } catch { /* An unsupported block remains readable and copyable. */ }
    }
  } finally {
    highlighter.dispose();
  }
}

highlight().catch(() => { /* Leave escaped code readable if WebAssembly is unavailable. */ });
