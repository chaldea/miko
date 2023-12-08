using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Miko
{
    public class MikoJsInterop
    {
        private const string ModuleName = "Miko.interop";
        private readonly IJSRuntime _jsRuntime;

        public MikoJsInterop(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async ValueTask CreateShadowDomAsync(ElementReference root, IList<ElementReference> children, string styleUrl)
        {
            await _jsRuntime.InvokeVoidAsync($"{ModuleName}.createShadowDom", root, children, styleUrl);
        }

        public async ValueTask CreateStyle(string styleUrl)
        {
            await _jsRuntime.InvokeVoidAsync($"{ModuleName}.createStyle", styleUrl);
        }

        public async ValueTask<ElementReference> GetDom(string selector)
        {
            return await _jsRuntime.InvokeAsync<ElementReference>($"{ModuleName}.getDom", selector);
        }

        public async ValueTask AddEventListener(ElementReference element, string type, Action<object> listener)
        {
            await _jsRuntime.InvokeVoidAsync($"{ModuleName}.addEventListener", element, type, listener);
        }
    }
}