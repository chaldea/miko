// Optional QA: node docs/Verify-Browser.mjs <site-root> <base-path> <playwright-module> [browser-executable]
// The static server intentionally has no SPA fallback or ASP.NET middleware.
import { createServer } from 'node:http';
import { readFile, stat, mkdir } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import assert from 'node:assert/strict';
import { verifyHighlighting } from './Verify-Highlighting.mjs';

const [siteArg, base = '/', playwrightModule, executablePath] = process.argv.slice(2);
assert(siteArg && playwrightModule, 'Supply site-root, base-path and an installed playwright-core module.');
const site = resolve(siteArg);
const { chromium } = await import(pathToFileURL(resolve(playwrightModule)).href);
const mime = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.wasm': 'application/wasm', '.json': 'application/json', '.svg': 'image/svg+xml' };
const server = createServer(async (request, response) => {
    try {
        const url = new URL(request.url, 'http://localhost');
        if (!url.pathname.startsWith(base)) { response.writeHead(404).end(); return; }
        let file = resolve(site, decodeURIComponent(url.pathname.slice(base.length)));
        if (file !== site && !file.startsWith(site + sep)) { response.writeHead(403).end(); return; }
        if ((await stat(file)).isDirectory()) {
            if (!url.pathname.endsWith('/')) { response.writeHead(301, { Location: url.pathname + '/' }).end(); return; }
            file = resolve(file, 'index.html');
        }
        response.writeHead(200, { 'Content-Type': mime[extname(file)] ?? 'application/octet-stream' });
        response.end(await readFile(file));
    } catch { response.writeHead(404).end('Not found'); }
});
await new Promise(ready => server.listen(0, '127.0.0.1', ready));
const origin = 'http://127.0.0.1:' + server.address().port;
let browser;
try {
    browser = await chromium.launch({ headless: true, ...(executablePath ? {executablePath} : {}) });
    const page = await browser.newPage({ viewport: {width: 1440, height: 1000} });
    const errors = [];
    const wasm = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('requestfailed', request => errors.push(request.url() + ': ' + request.failure()?.errorText));
    page.on('response', response => {
        if (new URL(response.url()).pathname.endsWith('.wasm')) wasm.push(response);
    });
    await page.goto(origin + base);
    await page.locator('.VPHomeHero').waitFor();
    const screenshots = resolve('artifacts/docs-browser', base === '/' ? 'root' : 'subpath');
    await mkdir(screenshots, {recursive:true});
    await page.screenshot({path:resolve(screenshots,'home.png'),fullPage:true});
    await page.locator('.VPHomeHero a').filter({hasText:'Live Demo'}).click();
    // The playground draws on a canvas; the loading overlay is removed after the first presented frame.
    await page.locator('#miko-app').waitFor({timeout:60000});
    await page.locator('#miko-loading').waitFor({state:'detached', timeout:120000});
    assert.equal(new URL(page.url()).pathname, base + 'demo/');
    assert.equal(await page.locator('#miko-loading.miko-error').count(), 0);
    assert(wasm.length > 0, 'The demo must download actual WebAssembly binaries');
    for (const response of wasm) {
        assert(response.ok(), response.url());
        assert(new URL(response.url()).pathname.startsWith(base + 'demo/_framework/'));
        assert.match(response.headers()['content-type'], /application\/wasm/);
    }
    assert.equal(await page.locator('script[src*="assets/miko.js"]').count(), 0);
    assert.equal(await page.locator('link[href*="assets/miko.css"]').count(), 0);
    assert.equal(await page.evaluate(async () => (await navigator.serviceWorker.getRegistrations()).length), 0);
    await page.screenshot({path:resolve(screenshots,'demo.png'),fullPage:true});
    await page.reload();
    await page.locator('#miko-loading').waitFor({state:'detached', timeout:120000});
    await page.goBack();
    await page.locator('.VPHomeHero').waitFor();
    assert.equal(new URL(page.url()).pathname, base);
    await page.goto(origin + base + 'guide/introduction/');
    await page.evaluate(() => document.fonts.ready);
    assert(await page.evaluate(() => document.fonts.check('16px Inter')), 'The local Inter font must load');
    assert.equal(await page.locator('.VPNavBarSearch .search-label').innerText(), 'Search');
    assert.equal(await page.locator('.social-link[aria-label="GitHub"]').count(), 1);
    assert((await page.locator('.vp-doc').innerText()).includes('🪶'), 'Supplementary Unicode must be preserved');
    assert.equal(await page.locator('.VPDocAsideOutline .level-3').count(), 0);
    const sidebarGroup = page.locator('.VPSidebar details').first();
    await sidebarGroup.locator('summary').click();
    const expanded = await sidebarGroup.evaluate(element => element.open);
    await page.reload();
    assert.equal(await page.locator('.VPSidebar details').first().evaluate(element => element.open), expanded);
    const flyout = page.locator('.VPNavBarMenuGroup').first();
    await flyout.locator('summary').hover();
    assert(await flyout.evaluate(element => element.open), 'Navigation group should open on hover');
    await page.keyboard.press('Escape');
    assert(!(await flyout.evaluate(element => element.open)));
    await page.locator('#search-open').click();
    await page.locator('#search-input').fill('razor');
    await page.waitForFunction(() => document.querySelector('#search-results a'));
    assert.match(await page.locator('#search-status').innerText(), /^Found \d+ results$/);
    await page.keyboard.press('Escape');
    await page.locator('#theme-toggle').click();
    assert.equal(await page.locator('#theme-toggle').getAttribute('aria-label'), 'Switch to light theme');
    await page.locator('#theme-toggle').click();
    await page.screenshot({path:resolve(screenshots,'introduction.png')});
    await verifyHighlighting(page, origin, base, screenshots);
    await page.goto(origin + base + 'components/ionic/button/');
    await page.locator('.vp-doc').waitFor();
    assert(!(await page.locator('.vp-doc').innerText()).includes('<!-- miko:'));
    await page.screenshot({path:resolve(screenshots,'component.png'),fullPage:true});
    const missing = await fetch(origin + base + 'missing-page');
    assert.equal(missing.status, 404);
    const redirect = await fetch(origin + base + 'demo', {redirect:'manual'});
    assert.equal(redirect.status, 301);
    assert.equal(redirect.headers.get('location'), base + 'demo/');
    await page.setViewportSize({width:390,height:844});
    await page.goto(origin + base + 'guide/introduction/');
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'Document must fit mobile viewport');
    await page.locator('#menu-toggle').click();
    assert(await page.locator('#mobile-nav').isVisible());
    await page.keyboard.press('Escape');
    assert(!(await page.locator('#mobile-nav').isVisible()));
    await page.screenshot({path:resolve(screenshots,'introduction-mobile.png')});
    await page.goto(origin + base);
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'Homepage must fit mobile viewport');
    await page.screenshot({path:resolve(screenshots,'home-mobile.png'),fullPage:true});
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({base, wasmResponses:wasm.length, browserErrors:errors, screenshots, result:'passed'}));
} finally {
    await browser?.close();
    await new Promise(done => server.close(done));
}
