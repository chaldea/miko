declare const __zone_symbol__requestAnimationFrame: any;
declare const requestAnimationFrame: any;

/**
 * Patched version of requestAnimationFrame that avoids ngzone
 * Use only when you know ngzone should not run
 */
 export const raf = (h: any) => {
    if (typeof __zone_symbol__requestAnimationFrame === 'function') {
      return __zone_symbol__requestAnimationFrame(h);
    }
    if (typeof requestAnimationFrame === 'function') {
      return requestAnimationFrame(h);
    }
    return setTimeout(h);
  };