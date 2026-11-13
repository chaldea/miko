import { dotnet } from '../../_framework/dotnet.js';

let callbacks;
let canvas;
let context2d;
let input;
let textarea;
let activeEditor = null;
let composing = false;
let frameHandle = 0;
let activePointerId = null;
let started = false;
let safeAreaProbe;
const wordSegmenter = typeof Intl.Segmenter === 'function' ? new Intl.Segmenter(undefined, { granularity: 'word' }) : null;

function fail(error) {
    console.error('[Miko] Browser host failed:', error);
    const loading = document.getElementById('miko-loading');
    if (loading) {
        loading.textContent = `Miko failed to start: ${error?.message ?? error}`;
        loading.classList.add('miko-error');
    }
}

function requestFrame() {
    if (frameHandle || !callbacks) return;
    frameHandle = requestAnimationFrame(timestamp => {
        frameHandle = 0;
        try {
            const rect = canvas.getBoundingClientRect();
            callbacks.RenderFrame(timestamp, rect.width, rect.height, window.devicePixelRatio || 1);
        } catch (error) {
            fail(error);
        }
    });
}

function present(view, width, height) {
    if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width;
        canvas.height = height;
    }
    // MemoryView points into the WASM heap and is valid only for this synchronous call.
    const copy = new Uint8Array(view.byteLength);
    view.copyTo(copy);
    const bytes = new Uint8ClampedArray(copy.buffer);
    context2d.putImageData(new ImageData(bytes, width, height), 0, 0);
    const loading = document.getElementById('miko-loading');
    if (loading) loading.remove();
}

function point(event) {
    const rect = canvas.getBoundingClientRect();
    return [event.clientX - rect.left, event.clientY - rect.top];
}

function pointerKind(pointerType) {
    return pointerType === 'touch' ? 1 : pointerType === 'pen' ? 2 : 0;
}

function modifiers(event) {
    return (event.ctrlKey ? 1 : 0) | (event.shiftKey ? 2 : 0) |
        (event.altKey ? 4 : 0) | (event.metaKey ? 8 : 0);
}

function onKeyDown(event) {
    if (!callbacks || event.isComposing || event.keyCode === 229) return;
    if (callbacks.KeyDown(event.code, modifiers(event), event.repeat)) event.preventDefault();
    requestFrame();
}

function onKeyUp(event) {
    if (!callbacks || event.isComposing) return;
    callbacks.KeyUp(event.code, modifiers(event));
    requestFrame();
}

function previousWordStart(text, cursor) {
    if (!wordSegmenter) {
        const match = text.slice(0, cursor).match(/[\p{L}\p{N}_]+$/u);
        return match ? cursor - match[0].length : Math.max(0, cursor - 1);
    }
    let boundary = 0;
    for (const part of wordSegmenter.segment(text.slice(0, cursor))) boundary = part.index;
    return boundary;
}

function nextWordEnd(text, cursor) {
    if (!wordSegmenter) {
        const match = text.slice(cursor).match(/^[\p{L}\p{N}_]+/u);
        return match ? cursor + match[0].length : Math.min(text.length, cursor + 1);
    }
    for (const part of wordSegmenter.segment(text.slice(cursor))) return cursor + part.segment.length;
    return text.length;
}

function deletionCounts(inputType) {
    const text = activeEditor.value || '';
    const cursor = activeEditor.selectionStart ?? text.length;
    const backward = inputType.includes('Backward');
    const lineStart = cursor <= 0 ? 0 : text.lastIndexOf('\n', cursor - 1) + 1;
    const newline = text.indexOf('\n', cursor);
    const lineEnd = newline < 0 ? text.length : newline;
    if (inputType.includes('EntireSoftLine')) {
        return { before: cursor - lineStart, after: lineEnd - cursor };
    }
    if (inputType.includes('SoftLine') || inputType.includes('HardLine')) {
        return backward
            ? { before: cursor - lineStart, after: 0 }
            : { before: 0, after: lineEnd - cursor };
    }
    if (inputType.includes('Word')) {
        return backward
            ? { before: cursor - previousWordStart(text, cursor), after: 0 }
            : { before: 0, after: nextWordEnd(text, cursor) - cursor };
    }
    return { before: backward ? 1 : 0, after: backward ? 0 : 1 };
}

function onBeforeInput(event) {
    if (!callbacks || event.target !== activeEditor) return;
    // During IME composition the native editor owns both its value and selection. Deleting or
    // inserting here must update the composition, never Miko's committed text.
    if (composing) return;
    const type = event.inputType;
    if (type.startsWith('delete')) {
        event.preventDefault();
        const { before, after } = deletionCounts(type);
        callbacks.DeleteSurroundingText(before, after);
    } else if (type === 'insertLineBreak' || type === 'insertParagraph') {
        event.preventDefault();
        callbacks.TextInput('\n');
    } else if (type.startsWith('insert')) {
        event.preventDefault();
        if (!composing && type !== 'insertCompositionText') {
            const text = event.data ?? event.dataTransfer?.getData('text/plain');
            if (text) callbacks.TextInput(text);
        }
    }
    requestFrame();
}

function configureEditor(editor, state) {
    if (!composing && editor.value !== state.text) editor.value = state.text;
}

function setInputMethodState(text, cursor, multiline, inputType, left, top, width, height) {
    const editor = multiline ? textarea : input;
    activeEditor = editor;
    const other = editor === input ? textarea : input;
    if (document.activeElement === other) other.blur();

    editor.style.left = `${left}px`;
    editor.style.top = `${top}px`;
    editor.style.width = `${Math.max(1, width)}px`;
    editor.style.height = `${Math.max(1, height)}px`;
    editor.inputMode = inputType === 'number' ? 'decimal' : 'text';
    if (editor === input) input.type = inputType === 'password' ? 'password' : 'text';
    configureEditor(editor, { text, cursor });
    if (document.activeElement !== editor) editor.focus({ preventScroll: true });
    if (!composing) {
        try { editor.setSelectionRange(cursor, cursor); } catch { /* input types without selection */ }
    }
}

function showKeyboard() {
    if (!activeEditor) return;
    activeEditor.focus({ preventScroll: true });
    activeEditor.click();
}

function hideKeyboard() {
    if (document.activeElement === input || document.activeElement === textarea) {
        document.activeElement.blur();
    }
    activeEditor = null;
    composing = false;
    updateKeyboardInset();
}

function updateSafeArea() {
    if (!callbacks || !safeAreaProbe) return;
    const style = getComputedStyle(safeAreaProbe);
    callbacks.SetSafeAreaInsets(
        parseFloat(style.paddingLeft) || 0, parseFloat(style.paddingTop) || 0,
        parseFloat(style.paddingRight) || 0, parseFloat(style.paddingBottom) || 0);
}

function updateKeyboardInset() {
    if (!callbacks || !window.visualViewport) return;
    const viewport = window.visualViewport;
    // visualViewport also shrinks for pinch zoom and desktop window resizing. Only a focused
    // editor on a touch device at 1× scale can indicate a software keyboard.
    const keyboardVisible = activeEditor && document.activeElement === activeEditor &&
        navigator.maxTouchPoints > 0 && Math.abs(viewport.scale - 1) < 0.01;
    const inset = keyboardVisible ? Math.max(0, window.innerHeight - viewport.height - viewport.offsetTop) : 0;
    callbacks.SetKeyboardInset(inset);
    requestFrame();
}

function initialize(canvasId, title) {
    canvas = document.getElementById(canvasId);
    if (!(canvas instanceof HTMLCanvasElement)) {
        throw new Error(`Canvas #${canvasId} was not found. Add <canvas id="${canvasId}"></canvas> to index.html.`);
    }
    context2d = canvas.getContext('2d', { alpha: false, desynchronized: true });
    if (!context2d) throw new Error('The browser did not provide a 2D canvas context.');
    document.title = title;
    canvas.tabIndex = 0;
    canvas.style.touchAction = 'none';
    canvas.style.outline = 'none';

    safeAreaProbe = document.createElement('div');
    safeAreaProbe.setAttribute('aria-hidden', 'true');
    Object.assign(safeAreaProbe.style, {
        position: 'fixed', visibility: 'hidden', pointerEvents: 'none',
        paddingLeft: 'env(safe-area-inset-left, 0px)',
        paddingTop: 'env(safe-area-inset-top, 0px)',
        paddingRight: 'env(safe-area-inset-right, 0px)',
        paddingBottom: 'env(safe-area-inset-bottom, 0px)',
    });
    document.body.appendChild(safeAreaProbe);

    input = document.createElement('input');
    textarea = document.createElement('textarea');
    for (const editor of [input, textarea]) {
        editor.className = 'miko-native-input';
        editor.setAttribute('aria-hidden', 'true');
        editor.autocapitalize = 'off';
        editor.autocomplete = 'off';
        editor.spellcheck = false;
        Object.assign(editor.style, {
            position: 'fixed', opacity: '0', pointerEvents: 'none', border: '0', padding: '0',
            margin: '0', background: 'transparent', color: 'transparent', caretColor: 'transparent',
            fontSize: '16px', resize: 'none', overflow: 'hidden', zIndex: '-1'
        });
        editor.addEventListener('keydown', onKeyDown);
        editor.addEventListener('keyup', onKeyUp);
        editor.addEventListener('beforeinput', onBeforeInput);
        editor.addEventListener('paste', event => {
            if (editor !== activeEditor || composing) return;
            event.preventDefault();
            const text = event.clipboardData?.getData('text/plain');
            if (text) callbacks.TextInput(text);
            requestFrame();
        });
        editor.addEventListener('compositionstart', event => {
            if (editor !== activeEditor) return;
            composing = true;
            callbacks.CompositionStart();
            event.preventDefault();
            requestFrame();
        });
        editor.addEventListener('compositionupdate', event => {
            if (editor !== activeEditor) return;
            callbacks.CompositionUpdate(event.data || '');
            requestFrame();
        });
        editor.addEventListener('compositionend', event => {
            if (editor !== activeEditor) return;
            composing = false;
            callbacks.CompositionEnd(event.data || '');
            requestFrame();
        });
        document.body.appendChild(editor);
    }

    canvas.addEventListener('keydown', onKeyDown);
    canvas.addEventListener('keyup', onKeyUp);
    canvas.addEventListener('contextmenu', event => event.preventDefault());
    canvas.addEventListener('pointerdown', event => {
        if (activePointerId !== null && activePointerId !== event.pointerId) return;
        activePointerId = event.pointerId;
        canvas.setPointerCapture(event.pointerId);
        if (document.activeElement !== activeEditor) canvas.focus({ preventScroll: true });
        const [x, y] = point(event);
        callbacks.PointerDown(x, y, event.button, pointerKind(event.pointerType), event.pointerId);
        event.preventDefault();
        requestFrame();
    });
    canvas.addEventListener('pointermove', event => {
        if (activePointerId !== null && activePointerId !== event.pointerId) return;
        const [x, y] = point(event);
        callbacks.PointerMove(x, y);
        event.preventDefault();
        requestFrame();
    });
    const endPointer = (event, cancel) => {
        if (activePointerId !== event.pointerId) return;
        const [x, y] = point(event);
        if (cancel) callbacks.PointerCancel(x, y, event.pointerId);
        else callbacks.PointerUp(x, y, event.button, pointerKind(event.pointerType), event.pointerId);
        activePointerId = null;
        if (canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
        event.preventDefault();
        requestFrame();
    };
    canvas.addEventListener('pointerup', event => endPointer(event, false));
    canvas.addEventListener('pointercancel', event => endPointer(event, true));
    canvas.addEventListener('lostpointercapture', event => {
        if (activePointerId === event.pointerId) endPointer(event, true);
    });
    canvas.addEventListener('wheel', event => {
        const [x, y] = point(event);
        const unit = event.deltaMode === WheelEvent.DOM_DELTA_LINE ? 16 :
            event.deltaMode === WheelEvent.DOM_DELTA_PAGE ? canvas.clientHeight : 1;
        callbacks.Wheel(x, y, event.deltaX * unit, event.deltaY * unit);
        event.preventDefault();
        requestFrame();
    }, { passive: false });

    const viewportChanged = () => {
        updateSafeArea();
        updateKeyboardInset();
        requestFrame();
    };
    window.addEventListener('resize', viewportChanged);
    window.addEventListener('orientationchange', viewportChanged);
    if (window.ResizeObserver) new ResizeObserver(viewportChanged).observe(canvas);
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', viewportChanged);
        window.visualViewport.addEventListener('scroll', viewportChanged);
    }
    document.addEventListener('visibilitychange', () => {
        callbacks.SetVisible(!document.hidden);
        requestFrame();
    });
    updateSafeArea();
    updateKeyboardInset();
    started = true;
}

let nativeCallbacks;
let openedBrowser = null;
let networkSubscribed = false;
let fallbackDeviceId;

function locationPath() {
    return location.hash.startsWith('#/') ? decodeURI(location.hash.slice(1)) : '/';
}

function openBrowser(url, windowName) {
    openedBrowser = window.open(url, windowName || '_blank');
    if (!openedBrowser) return false;
    try { openedBrowser.opener = null; openedBrowser.focus(); } catch { /* cross-origin windows may reject access */ }
    return true;
}

function closeBrowser() {
    if (!openedBrowser || openedBrowser.closed) return false;
    openedBrowser.close();
    openedBrowser = null;
    return true;
}

async function writeClipboardImage(dataUrl) {
    const response = await fetch(dataUrl);
    const blob = await response.blob();
    await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
}

function blobDataUrl(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(blob);
    });
}

async function readClipboard() {
    if (navigator.clipboard.read) {
        const items = await navigator.clipboard.read();
        for (const item of items) {
            const imageType = item.types.find(type => type.startsWith('image/'));
            if (imageType) {
                return JSON.stringify({ value: await blobDataUrl(await item.getType(imageType)), type: imageType });
            }
            if (item.types.includes('text/plain')) {
                const text = await (await item.getType('text/plain')).text();
                return JSON.stringify({ value: text, type: 'text/plain' });
            }
        }
        return JSON.stringify({ value: '', type: 'text/plain' });
    }
    const value = await navigator.clipboard.readText();
    return JSON.stringify({ value, type: 'text/plain' });
}

function getDeviceId() {
    try {
        const key = 'miko.device-id';
        let id = localStorage.getItem(key);
        if (!id) {
            id = crypto.randomUUID();
            localStorage.setItem(key, id);
        }
        return id;
    } catch {
        // Storage can be denied in private/sandboxed contexts; keep one identifier for this page session.
        return fallbackDeviceId ??= crypto.randomUUID();
    }
}

function deviceOperatingSystem() {
    const text = `${navigator.userAgentData?.platform || ''} ${navigator.userAgent}`.toLowerCase();
    if (/iphone|ipad|ipod|ios/.test(text)) return 'ios';
    if (/android/.test(text)) return 'android';
    if (/windows/.test(text)) return 'windows';
    if (/mac/.test(text)) return 'mac';
    return 'unknown';
}

function getDeviceInfo() {
    return JSON.stringify({
        name: navigator.userAgentData?.platform || navigator.platform || 'Browser',
        model: navigator.userAgentData?.model || navigator.platform || 'Browser',
        operatingSystem: deviceOperatingSystem(),
        osVersion: navigator.userAgent,
        manufacturer: navigator.vendor || 'Unknown',
        memoryUsed: performance.memory?.usedJSHeapSize || 0,
        userAgent: navigator.userAgent,
    });
}

async function getBatteryInfo() {
    if (!navigator.getBattery) return null;
    const battery = await navigator.getBattery();
    return JSON.stringify({ level: battery.level, charging: battery.charging });
}

function networkStatus() {
    const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
    return JSON.stringify({ connected: navigator.onLine, type: connection?.type || 'unknown' });
}

function notifyNetwork() {
    nativeCallbacks.NetworkChanged(networkStatus());
    requestFrame();
}

function subscribeNetwork() {
    if (networkSubscribed) return;
    networkSubscribed = true;
    window.addEventListener('online', notifyNetwork);
    window.addEventListener('offline', notifyNetwork);
    const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
    connection?.addEventListener('change', notifyNetwork);
}

function unsubscribeNetwork() {
    if (!networkSubscribed) return;
    networkSubscribed = false;
    window.removeEventListener('online', notifyNetwork);
    window.removeEventListener('offline', notifyNetwork);
    const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
    connection?.removeEventListener('change', notifyNetwork);
}

function speak(text, language) {
    if (!window.speechSynthesis || !window.SpeechSynthesisUtterance) return false;
    const utterance = new SpeechSynthesisUtterance(text);
    if (language) utterance.lang = language;
    speechSynthesis.speak(utterance);
    return true;
}

function positionOptions(json) {
    const value = JSON.parse(json);
    return {
        enableHighAccuracy: value.enableHighAccuracy,
        timeout: value.timeout,
        maximumAge: value.maximumAge,
    };
}

function positionJson(position) {
    return JSON.stringify({ timestamp: position.timestamp, coords: position.coords });
}

function getCurrentPosition(optionsJson) {
    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            position => resolve(positionJson(position)),
            error => {
                const reason = new Error(error.message);
                reason.code = error.code;
                reject(reason);
            },
            positionOptions(optionsJson));
    });
}

function watchPosition(optionsJson) {
    const id = navigator.geolocation.watchPosition(
        position => { nativeCallbacks.PositionChanged(String(id), positionJson(position), null); requestFrame(); },
        error => { nativeCallbacks.PositionChanged(String(id), null, error.message); requestFrame(); },
        positionOptions(optionsJson));
    return String(id);
}

async function checkGeolocationPermission() {
    if (!navigator.permissions?.query) return 'prompt';
    try {
        const status = await navigator.permissions.query({ name: 'geolocation' });
        return status.state;
    } catch {
        return 'prompt';
    }
}

async function requestGeolocationPermission() {
    let requestError;
    try {
        await getCurrentPosition(JSON.stringify({ enableHighAccuracy: false, timeout: 10000, maximumAge: 0 }));
    } catch (error) {
        requestError = error;
    }
    if (navigator.permissions?.query) return checkGeolocationPermission();
    return requestError?.code === 1 ? 'denied' : requestError ? 'prompt' : 'granted';
}

const runtime = await dotnet.create();
const { setModuleImports, getAssemblyExports, runMain } = runtime;
setModuleImports('miko-browser', {
    initialize,
    requestFrame,
    present,
    setCursor: cursor => { if (canvas) canvas.style.cursor = cursor; },
    setInputMethodState,
    showKeyboard,
    hideKeyboard,
});

const assembly = await getAssemblyExports('Miko.Browser');
callbacks = assembly.Miko.Browser.BrowserCallbacks;
nativeCallbacks = assembly.Miko.Browser.Native.BrowserNativeCallbacks;
setModuleImports('miko-browser-native', {
    getLocationPath: locationPath,
    setLocationPath: path => { location.hash = path; },
    replaceLocationPath: path => history.replaceState(null, '', `${location.pathname}${location.search}#${path}`),
    navigateBack: () => history.back(),
    isAppActive: () => !document.hidden,
    getLaunchUrl: () => location.href,
    getLanguage: () => navigator.language || 'en',
    openBrowser,
    closeBrowser,
    writeClipboardText: text => navigator.clipboard.writeText(text),
    writeClipboardImage,
    readClipboard,
    getDeviceId,
    getDeviceInfo,
    getBatteryInfo,
    getNetworkStatus: networkStatus,
    subscribeNetwork,
    unsubscribeNetwork,
    speak,
    supportsVibration: () => typeof navigator.vibrate === 'function',
    vibrate: pattern => navigator.vibrate(pattern.split(',').map(Number)),
    supportsGeolocation: () => Boolean(navigator.geolocation),
    getCurrentPosition,
    watchPosition,
    clearPositionWatch: id => navigator.geolocation.clearWatch(Number(id)),
    checkGeolocationPermission,
    requestGeolocationPermission,
});
window.addEventListener('hashchange', () => {
    callbacks.Navigate(locationPath());
    requestFrame();
});
try {
    await runMain();
    if (!started) throw new Error('The application Main method did not call RunBrowser().');
} catch (error) {
    fail(error);
}
