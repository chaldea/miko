using Miko.Components.Player;
using Miko.Hosting;
using Miko.Windowing;
using Miko.Windowing.Video;
using MikoApp.Player;

if (args.Contains("--verify"))
{
    return PlayerVerification.Run(args.SkipWhile(a => a != "--verify").Skip(1).FirstOrDefault());
}
var builder = MikoAppBuilder.CreateDefault();
builder.Services.AddMikoPlayer();
builder.UseSystemVideo();
builder.UseTitle("MikoPlayer").UseSize(960, 760);
builder.UseRootComponent(() => new PlayerDemo().Build());
builder.Build().RunDesktop();
return 0;
