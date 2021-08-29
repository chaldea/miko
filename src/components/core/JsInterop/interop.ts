import { getStyle } from "./style";

export function createShadowDom(root, children, style: string, type: string) {
    const shadow = root.attachShadow({ mode: 'open' });
    for (let item of children) {
        shadow.appendChild(item);
    }
    shadow.adoptedStyleSheets = [getStyle(style, type)];
}
