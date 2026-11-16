using Miko.MockServer;

if (args.FirstOrDefault() == "--generate-media")
{
    await MediaGenerator.GenerateAsync(args.ElementAtOrDefault(1) ?? "ffmpeg");
    return;
}
await MockApplication.Build(args).RunAsync();
