import { styles } from "./style";

const styleCache: { [key: string]: any } = {};

export function getStyle(styleUrl: string): CSSStyleSheet {
    if (!styleCache[styleUrl]) {
        styleCache[styleUrl] = new CSSStyleSheet();
        styleCache[styleUrl].replace(styles[styleUrl]);
    }
    return styleCache[styleUrl];
}

export function createShadowDom(root, children, styleUrl) {
    const shadow = root.attachShadow({ mode: 'open' });
    for (let item of children) {
        shadow.appendChild(item);
    }
    shadow.adoptedStyleSheets = [getStyle(styleUrl)];
}

export function createStyle(styleUrl: string) {
    var styleEl = document.createElement('style');
    document.head.appendChild(styleEl);
    styleEl.innerHTML = styles[styleUrl];
}
