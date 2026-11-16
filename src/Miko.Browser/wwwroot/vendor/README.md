# hls.js

`hls.min.js` is the unmodified hls.js **1.7.4** distribution from
https://www.npmjs.com/package/hls.js/v/1.7.4 (`dist/hls.min.js`).
The Apache-2.0 license is included in `hls.LICENSE`.

Miko loads this local file lazily only when the browser cannot play HLS natively.
No CDN or worker download is needed at runtime. To update, replace the distribution
and license from a pinned npm release and run `docs/Verify-MockMedia.mjs` with both
native HLS and the forced MediaSource path.
