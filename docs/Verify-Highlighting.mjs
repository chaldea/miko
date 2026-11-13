// Browser behavior checks shared by Verify-Browser.mjs; fixtures never enter the published site.
import assert from 'node:assert/strict';
import { resolve } from 'node:path';

export async function verifyHighlighting(page, origin, base, screenshots) {
    const requests = [];
    const record = request => requests.push(request.url());
    page.on('request', record);
    await page.goto(origin + base + 'guide/getting-started/');
    await page.waitForFunction(() => ['csharp', 'razor', 'bash'].every(language =>
        document.querySelector('.language-' + language + ' code[data-highlighted="shiki"]')));
    for (const language of ['csharp', 'razor', 'bash']) {
        const colors = await page.locator('.language-' + language + ' code').first().evaluate(code =>
            [...new Set([...code.querySelectorAll('.line > span')].map(token => getComputedStyle(token).color))]);
        assert(colors.length > 1, language + ' must have multiple syntax colors');
    }
    // Disable transitions only while taking QA screenshots so theme colors are settled.
    const screenshot = async (block, name) => {
        await block.evaluate(element => element.scrollIntoView({ block: 'center', behavior: 'instant' }));
        await block.screenshot({ path: resolve(screenshots, name), animations: 'disabled',
            style: '.VPNav { visibility: hidden !important; } *, *::before, *::after { transition: none !important; }' });
    };
    await screenshot(page.locator('.language-csharp').first(), 'code-csharp-light.png');
    await screenshot(page.locator('.language-razor').first(), 'code-razor-light.png');

    const newline = String.fromCharCode(10);
    const source = ['/* multiline', 'still a comment */', 'var message = "<script>alert(1)</script>&😀";', '', String.fromCharCode(9) + 'Console.WriteLine(message);'];
    const fixtures = [
        ['csharp', source], ['cs', ['public int Count => 42;']],
        ['razor', ['@page "/demo"', '<Button Class="primary">@message</Button>', '@code {', '  private string message = "Hello";', '}']],
        ['cshtml', ['<div>@DateTime.Now</div>']], ['bash', ['echo "hello" # comment']],
        ['ps1', ['$name = "Miko"; Write-Host $name']], ['xml', ['<Project Sdk="Microsoft.NET.Sdk" />']],
        ['json', ['{"name": "Miko", "count": 42}']], ['jsonc', ['// comment', '{"ok": true}']],
        ['css', ['.app { color: red; }']], ['html', ['<button disabled>OK</button>']],
        ['js', ['const message = "Miko";']], ['ts', ['const count: number = 42;']],
        ['jsx', ['const app = <div>Hello</div>;']], ['tsx', ['const app: JSX.Element = <div>Hello</div>;']],
        ['yml', ['name: Miko', 'enabled: true']],
        ['text', ['<script>do not execute</script> & 😀']], ['unknown-language', ['<div>leave as text</div>']], ['csharp', []]
    ];
    const escape = text => text.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
    const blocks = fixtures.map(([language, lines], index) =>
        '<div id="fixture-' + index + '" class="language-' + language + ' line-numbers-mode" dir="ltr">' +
        '<button class="copy" type="button" title="Copy code" aria-label="Copy code"></button><span class="lang">' + language + '</span>' +
        '<pre tabindex="0"><code>' + lines.map((line, i) => '<span class="line' + (i === 1 ? ' highlighted' : '') + '">' + escape(line) + '</span>' + newline).join('') +
        '</code></pre><div class="line-numbers-wrapper" aria-hidden="true">' + lines.map((_, i) => i + 1).join('<br>') + '</div></div>').join('');
    const url = origin + base + '__highlight-qa/';
    const template = await (await page.request.get(origin + base + 'guide/getting-started/')).text();
    const start = template.indexOf('<div class="vp-doc">') + '<div class="vp-doc">'.length;
    const end = template.indexOf('</main>', start);
    assert(start > 0 && end > start);
    const body = template.slice(0, start) + blocks + '</div>' + template.slice(end);
    await page.route(url, route => route.fulfill({ status: 200, contentType: 'text/html; charset=utf-8', body }));
    await page.goto(url);
    await page.waitForFunction(() => document.querySelectorAll('code[data-highlighted="shiki"]').length === 16);
    for (const [index, [, lines]] of fixtures.entries()) {
        assert.equal(await page.locator('#fixture-' + index + ' code').textContent(), lines.length ? lines.join(newline) + newline : '');
    }
    assert.equal(await page.locator('#fixture-16 code span span, #fixture-17 code span span').count(), 0);
    assert.equal(await page.locator('.vp-doc script').count(), 0, 'Code must remain inert text');
    assert.equal(await page.locator('#fixture-0 .line.highlighted').count(), 1);
    assert.equal(await page.locator('#fixture-0 .line-numbers-wrapper').innerText(), [1, 2, 3, 4, 5].join(newline));
    const comment = page.locator('#fixture-0 .line').nth(1).locator('span').first();
    assert.equal(await comment.evaluate(token => getComputedStyle(token).color), 'rgb(106, 115, 125)', 'Multiline comments use GitHub Light colors');
    const keyword = page.locator('#fixture-0 .line > span').filter({ hasText: /^var$/ }).first();
    assert.equal(await keyword.evaluate(token => getComputedStyle(token).color), 'rgb(215, 58, 73)');
    await page.locator('#theme-toggle').click();
    assert.equal(await keyword.evaluate(token => getComputedStyle(token).color), 'rgb(249, 117, 131)');
    assert.equal(await comment.evaluate(token => getComputedStyle(token).color), 'rgb(106, 115, 125)');
    await screenshot(page.locator('#fixture-0'), 'code-csharp-dark.png');
    await screenshot(page.locator('#fixture-2'), 'code-razor-dark.png');

    await page.context().grantPermissions(['clipboard-read', 'clipboard-write']);
    const button = page.locator('#fixture-0 button.copy');
    const originalIcon = await button.evaluate(element => getComputedStyle(element).backgroundImage);
    await button.click();
    await page.waitForFunction(() => document.querySelector('#fixture-0 button').classList.contains('copied'));
    // Windows normalizes clipboard line endings to CRLF; source characters stay unchanged.
    assert.equal((await page.evaluate(() => navigator.clipboard.readText())).replaceAll(String.fromCharCode(13), ''), source.join(newline) + newline);
    assert.equal(await button.textContent(), '', 'Only the SVG icon should render');
    assert.equal(await button.getAttribute('aria-label'), 'Copied');
    assert.notEqual(await button.evaluate(element => getComputedStyle(element).backgroundImage), originalIcon);
    assert.equal(await button.evaluate(element => getComputedStyle(element, '::before').content), '"Copied"');
    const size = await button.boundingBox();
    assert.equal(size.width, 40); assert.equal(size.height, 40);
    await screenshot(page.locator('#fixture-0'), 'code-copied-dark.png');
    await page.locator('#theme-toggle').click();
    await button.click();
    await page.waitForFunction(() => document.querySelector('#fixture-0 button').classList.contains('copied'));
    await screenshot(page.locator('#fixture-0'), 'code-copied-light.png');
    await page.waitForFunction(() => !document.querySelector('#fixture-0 button').classList.contains('copied'));
    assert.equal(await button.getAttribute('title'), 'Copy code');
    // A rejected clipboard request must not show success, and retry must work.
    await page.evaluate(() => {
        window.originalClipboard = navigator.clipboard.writeText.bind(navigator.clipboard);
        navigator.clipboard.writeText = () => Promise.reject(new Error('Clipboard denied for QA'));
    });
    await button.click();
    assert.equal(await button.getAttribute('aria-label'), 'Copy failed; select the code manually');
    assert(!(await button.evaluate(element => element.classList.contains('copied'))));
    await page.evaluate(() => { navigator.clipboard.writeText = window.originalClipboard; });
    await button.click();
    await page.waitForFunction(() => document.querySelector('#fixture-0 button').classList.contains('copied'));

    const noJs = await page.context().browser().newContext({ javaScriptEnabled: false });
    try {
        const plain = await noJs.newPage();
        await plain.goto(origin + base + 'guide/getting-started/');
        assert((await plain.locator('.language-csharp code').first().textContent()).includes('using'));
    } finally { await noJs.close(); }
    assert(requests.every(request => new URL(request).origin === origin), 'Highlighting must use only local assets');
    page.off('request', record);
    await page.unroute(url);
}
