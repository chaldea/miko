import * as interop from "./src/components/core/JsInterop/interop";

declare global {
  interface Window {
    Miko: any;
  }
}

window.Miko = {
  interop,
};
