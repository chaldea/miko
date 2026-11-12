using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Miko.Docs.Demo;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
await builder.Build().RunAsync();
