using Miko.Hosting;
using Miko.Windowing;
using MikoApp.Editor.Desktop;

var builder = MikoAppBuilder.CreateDefault().UseTitle("Miko Editor").UseSize(840, 560);
using var app = builder.UseRootComponent(() => new EditorDemo().Build()).Build();
app.RunDesktop();
