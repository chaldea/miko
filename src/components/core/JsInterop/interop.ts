import { styles } from "./style";

const styleCache: { [key: string]: any } = {};

export function getDom(element: any) {
    if (!element) {
        element = document.body;
    } else if (typeof element === 'string') {
        if (element === 'document') {
            return document;
        }
        element = document.querySelector(element!)
    }
    return element;
}

export function addEventListener(element: HTMLElement, type: string, listener: () => void) {
    element.addEventListener(type, listener);
}

export function getStyle(styleUrl: string): CSSStyleSheet {
    if (!styleCache[styleUrl]) {
        styleCache[styleUrl] = new CSSStyleSheet();
        styleCache[styleUrl].replace(styles[styleUrl]);
    }
    return styleCache[styleUrl];
}

export function createShadowDom(root: any, children: any, styleUrl: string) {
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
