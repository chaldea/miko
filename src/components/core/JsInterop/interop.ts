import { getStyle } from "./style";

export function createShadowDom(root, child: HTMLElement, style: string, type: string) {
    const shadow = root.attachShadow({ mode: 'open' });
    shadow.appendChild(child);
    shadow.adoptedStyleSheets = [getStyle(style, type)];
}
