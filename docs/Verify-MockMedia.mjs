// Start Miko.MockServer and the WASM playground first.
// node docs/Verify-MockMedia.mjs <playground-url> <mock-http-url> <playwright-module> <browser-executable>
import assert from 'node:assert/strict';
import { mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const [playground, mock, playwrightModule, executablePath] = process.argv.slice(2);
assert(playground && mock && playwrightModule && executablePath, 'Supply playground, mock, Playwright and browser paths.');
const { chromium } = await import(pathToFileURL(resolve(playwrightModule)).href);
const browser = await chromium.launch({ headless: true, executablePath });
const output = resolve('artifacts/issue168-browser');
await mkdir(output, { recursive: true });
const errors = [];
const websocketUrl = mock.replace(/^http/, 'ws').replace(/\/$/, '') + '/vnc';

async function open(route, forceMse = false) {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
        // The SDK's debug host probes this optional development-only file.
        if (!response.ok() && !response.url().includes('hot-reload-settings.json'))
            errors.push(`${response.status()} ${response.url()}`);
    });
    await page.addInitScript(forceMse => {
        window.qaVideos = [];
        const create = document.createElement.bind(document);
        document.createElement = (...args) => {
            const element = create(...args);
            if (args[0] === 'video') window.qaVideos.push(element);
            return element;
        };
        if (forceMse) HTMLMediaElement.prototype.canPlayType = () => '';
    }, forceMse);
    await page.goto(playground.replace(/\/$/, '') + '/#/' + route);
    await page.locator('#miko-loading').waitFor({ state: 'detached', timeout: 120000 });
    return page;
}

async function edit(page, x, y, text) {
    await page.mouse.click(x, y);
    // Canvas inputs currently expose a caret, so replace through their key/input path.
    const length = await page.locator('input').inputValue().then(value => value.length);
    await page.keyboard.press('End');
    for (let i = 0; i < length; i++) await page.keyboard.press('Backspace');
    await page.keyboard.insertText(text);
}

async function pixels(page, x, y, width, height) {
    return page.evaluate(([x, y, width, height]) => Array.from(document.querySelector('#miko-app')
        .getContext('2d').getImageData(x, y, width, height).data), [x, y, width, height]);
}

try {
    const vnc = await open('vnc');
    let received = 0, closed = false;
    vnc.on('websocket', socket => {
        if (socket.url() !== websocketUrl) return;
        socket.on('framereceived', frame => { received += frame.payload.length; });
        socket.on('close', () => { closed = true; });
    });
    await edit(vnc, 780, 363, websocketUrl);
    await vnc.mouse.click(780, 596);
    await assertEventually(() => received > 640 * 360 * 4);
    // The framebuffer is fitted into the 346 x 606 canvas preview, letterboxed vertically.
    await vnc.waitForTimeout(300);
    const before = await pixels(vnc, 625, 424, 80, 48);
    const count = received;
    await vnc.mouse.click(675, 438);
    await assertEventually(() => received > count + 640 * 360 * 4);
    await vnc.waitForTimeout(300);
    assert.notDeepEqual(await pixels(vnc, 625, 424, 80, 48), before, 'Click must update the actual rendered desktop');
    await vnc.screenshot({ path: resolve(output, 'vnc-clicked.png') });
    await vnc.evaluate(() => { location.hash = '#/webcam'; });
    await assertEventually(() => closed);
    await vnc.close();

    for (const forceMse of [false, true]) {
        const page = await open('webcam', forceMse);
        await edit(page, 780, 294, mock.replace(/\/$/, '') + '/media/webcam.m3u8');
        await page.mouse.click(780, 341);
        try {
            await page.waitForFunction(() => window.qaVideos.some(video => video.currentTime > 1), null, { timeout: 15000 });
        } catch (error) {
            await page.screenshot({ path: resolve(output, 'webcam-failure.png') });
            console.log(await page.evaluate(() => qaVideos.map(v => ({ src: v.src, error: v.error?.message, paused: v.paused }))));
            console.log(errors);
            throw error;
        }
        assert.deepEqual(await page.evaluate(() => [qaVideos[0].videoWidth, qaVideos[0].videoHeight]), [1280, 360]);
        if (forceMse) assert.match(await page.evaluate(() => qaVideos[0].src), /^blob:/);
        // Pan while the source frame is paused: changed canvas pixels must come from PTZ cropping.
        await page.evaluate(() => qaVideos[0].pause());
        await page.waitForTimeout(200);
        const initial = await pixels(page, 620, 230, 320, 100);
        assert(new Set(initial).size > 30, 'Decoded panorama must reach the Miko canvas');
        await page.mouse.move(850, 556);
        await page.mouse.down();
        await page.waitForTimeout(700);
        await page.mouse.up();
        assert.notDeepEqual(await pixels(page, 620, 230, 320, 100), initial, 'PTZ must repaint a paused frame');
        await page.screenshot({ path: resolve(output, forceMse ? 'webcam-mse-ptz.png' : 'webcam-native-ptz.png') });
        // Exercise the loop boundary without a 12-second wait on every verification run.
        await page.evaluate(async () => { const v = qaVideos[0]; v.currentTime = v.duration - .4; await v.play(); });
        await page.waitForFunction(() => qaVideos[0].currentTime < 2 && !qaVideos[0].paused, null, { timeout: 10000 });
        await page.mouse.click(930, 386); // Fullscreen
        await page.waitForTimeout(250);
        await page.screenshot({ path: resolve(output, forceMse ? 'webcam-mse-fullscreen.png' : 'webcam-native-fullscreen.png') });
        await page.evaluate(() => { location.hash = '#/vnc'; });
        await page.waitForFunction(() => qaVideos.every(video => video.paused && !video.getAttribute('src')));
        await page.close();
    }
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({ result: 'passed', vncBytes: received, paths: ['native HLS', 'hls.js / MSE'], screenshots: output }));
} finally { await browser.close(); }

async function assertEventually(condition) {
    const until = Date.now() + 10000;
    while (!condition() && Date.now() < until) await new Promise(done => setTimeout(done, 50));
    assert(condition(), 'Timed out waiting for WebSocket traffic / cleanup');
}
