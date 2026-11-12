// Optional maintainer tooling only; dotnet build/publish embed the committed assets.
import { build } from 'esbuild';
import { readFile, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const require = createRequire(import.meta.url);
await build({
  entryPoints: [resolve(here, 'highlight.mjs')],
  outfile: resolve(here, '../Assets/highlight.js'),
  bundle: true, minify: true, format: 'iife', platform: 'browser', target: 'es2020',
  // Escape grammar whitespace in strings so Git line-ending conversion cannot alter it.
  supported: { 'template-literal': false },
  banner: { js: '/*! Shiki 2.5.0 | GitHub Light/Dark | Includes Oniguruma WASM. See SHIKI-LICENSE.txt and ONIGURUMA-LICENSE.txt. Source: Client/highlight.mjs. */' },
  legalComments: 'inline'
});
const notices = [];
for (const [name, license] of [
  ['shiki', 'LICENSE'], ['@shikijs/core', 'LICENSE'], ['@shikijs/engine-oniguruma', 'LICENSE'],
  ['@shikijs/langs', 'LICENSE'], ['@shikijs/themes', 'LICENSE'], ['@shikijs/vscode-textmate', 'LICENSE.md']
]) {
  // Resolve from the installed entry point because these packages restrict exports.
  const root = resolve(dirname(require.resolve(name)), '..');
  const { version } = JSON.parse(await readFile(resolve(root, 'package.json'), 'utf8'));
  notices.push(name + ' ' + version + '\n' + await readFile(resolve(root, license), 'utf8'));
}
await writeFile(resolve(here, '../Assets/SHIKI-LICENSE.txt'), notices.join('\n\n'), 'utf8');
