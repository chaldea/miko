// Browser decoders produce RGBA frames for Miko's canvas, including its PTZ crop and fullscreen.
const sessions = new Map();
let nextId = 1;
let hlsModule;

export const videoInterop = {
    create(url, hls, callback) {
        const id = nextId++;
        const video = document.createElement('video');
        video.crossOrigin = 'anonymous';
        video.playsInline = true;
        video.preload = 'auto';
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d', { willReadFrequently: true });
        const session = { video, canvas, context, callback, listeners: [], disposed: false, ready: false, play: false };
        sessions.set(id, session);
        const emit = (kind, seconds = video.currentTime, message = '') => {
            if (!session.disposed) callback(JSON.stringify({ kind, seconds: Number.isFinite(seconds) ? seconds : 0, width: video.videoWidth, height: video.videoHeight, message }));
        };
        session.emit = emit;
        const on = (name, handler) => {
            video.addEventListener(name, handler);
            session.listeners.push([name, handler]);
        };
        on('loadedmetadata', () => emit('loaded', video.duration));
        on('resize', () => emit('loaded', video.duration));
        on('loadeddata', () => emit('frame'));
        on('seeked', () => emit('frame'));
        on('playing', () => emit('playing'));
        on('pause', () => emit('pause'));
        on('waiting', () => emit('waiting'));
        on('ended', () => emit('ended'));
        on('error', () => emit('error', 0, video.error?.message || 'Video could not be loaded.'));
        const frame = () => {
            if (session.disposed) return;
            emit('frame');
            session.frame = video.requestVideoFrameCallback(frame);
        };
        if (video.requestVideoFrameCallback) session.frame = video.requestVideoFrameCallback(frame);
        else on('timeupdate', () => emit('frame'));
        // Defer loading so the engine can attach its event handler and apply playback options.
        queueMicrotask(async () => {
            try {
                if (session.disposed) return;
                if (hls && !video.canPlayType('application/vnd.apple.mpegurl')) {
                    hlsModule ??= import('./vendor/hls.min.js');
                    await hlsModule;
                    if (session.disposed) return;
                    const Hls = globalThis.Hls;
                    if (!Hls?.isSupported()) throw new Error('This browser does not support HLS playback.');
                    session.hls = new Hls({ enableWorker: false });
                    session.hls.on(Hls.Events.ERROR, (_, data) => {
                        if (data.fatal) emit('error', 0, `HLS playback failed: ${data.details}`);
                    });
                    session.hls.loadSource(url);
                    session.hls.attachMedia(video);
                } else video.src = url;
                session.ready = true;
                if (session.play) video.play().catch(error => emit('error', 0, error.message));
            } catch (error) { emit('error', 0, error.message); }
        });
        return id;
    },
    control(id, action, value) {
        const s = sessions.get(id);
        if (!s) return;
        switch (action) {
            case 'play': s.play = true; if (s.ready) s.video.play().catch(e => s.emit('error', 0, e.message)); break;
            case 'pause': s.play = false; s.video.pause(); break;
            case 'seek': s.video.currentTime = value; break;
            case 'muted': s.video.muted = Boolean(value); break;
            case 'volume': s.video.volume = value; break;
            case 'loop': s.video.loop = Boolean(value); break;
            case 'rate': s.video.playbackRate = value; break;
        }
    },
    position(id) { return sessions.get(id)?.video.currentTime || 0; },
    copyFrame(id, pixels) {
        const s = sessions.get(id);
        if (!s || s.frameFailed || s.video.readyState < 2) return false;
        const width = s.video.videoWidth, height = s.video.videoHeight;
        if (pixels.byteLength !== width * height * 4) return false;
        try {
            if (s.canvas.width !== width || s.canvas.height !== height) {
                s.canvas.width = width; s.canvas.height = height;
            }
            s.context.drawImage(s.video, 0, 0);
            const rgba = s.context.getImageData(0, 0, width, height).data;
            pixels.set(new Uint8Array(rgba.buffer, rgba.byteOffset, rgba.byteLength));
            return true;
        } catch (error) { s.frameFailed = true; s.emit('error', 0, error.message); return false; }
    },
    dispose(id) {
        const s = sessions.get(id);
        if (!s) return;
        s.disposed = true;
        if (s.frame !== undefined) s.video.cancelVideoFrameCallback(s.frame);
        for (const [name, handler] of s.listeners) s.video.removeEventListener(name, handler);
        s.hls?.destroy();
        s.video.pause();
        s.video.removeAttribute('src');
        s.video.load();
        s.callback = null;
        sessions.delete(id);
    },
};
