import * as interop from "./src/components/core/JsInterop/index";

declare global {
  interface Window {
    Miko: any;
  }
}

window.Miko = {
  interop,
};
