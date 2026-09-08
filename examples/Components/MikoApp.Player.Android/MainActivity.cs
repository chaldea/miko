using Android.App;
using Android.OS;
using Miko.Android;
using Miko.Android.Video;
using Miko.Common;
using Miko.Components.Player;
using Miko.Components.Player.Danmaku;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;

namespace MikoApp.Player.Android;

[Activity(Label = "MikoPlayer", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    private MikoPlayer? _player;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        string local = Path.Combine(FilesDir!.AbsolutePath, "miko-local.mp4");
        using (var source = Assets!.Open("miko-local.mp4"))
        using (var destination = File.Create(local)) source.CopyTo(destination);
        var builder = MikoAppBuilder.CreateDefault();
        builder.Services.AddMikoPlayer();
        builder.UseAndroidVideo();
        builder.UseRootComponent(() =>
        {
            _player?.Dispose();
            _player = new MikoPlayer { Source = Intent?.GetStringExtra("source") ?? local, AutoPlay = true, Loop = true,
                DanmakuSettings = new DanmakuSettings { FontSize = 16, Area = .5f, MaxVisible = 16 },
                Danmaku = Enumerable.Range(0, 200).Select(i => new DanmakuItem(TimeSpan.FromSeconds(i * .6), "Miko danmaku " + i,
                    i % 5 == 0 ? DanmakuMode.Top : DanmakuMode.Scroll)).ToArray() };
            var root = new DivElement { Style = new Style { Display = Miko.Common.Display.Flex, FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100), Height = Length.Percent(100), BackgroundColor = Color.FromHex("#101214"), Color = Color.White,
                Padding = Length.Px(12), BoxSizing = BoxSizing.BorderBox } };
            root.Children.Add(new H2Element { TextContent = "MikoPlayer", Style = new Style { FontSize = Length.Px(22) } });
            var sources = new DivElement { Style = new Style { Display = Miko.Common.Display.Flex, Height = Length.Px(48), Gap = Length.Px(8) } };
            foreach (var (label, uri) in new[] { ("Local", local), ("HLS", "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8") })
                sources.Children.Add(new ButtonElement { TextContent = label, Style = new Style { Width = Length.Px(80), Height = Length.Px(44), Color = Color.White,
                    BackgroundColor = Color.FromHex("#30373b"), BorderWidth = Length.Px(0) },
                    OnClick = _ => { _player.Source = uri; _player.Build(); } });
            root.Children.Add(sources);
            root.Children.Add(_player.Build());
            return root;
        });
        SetContentView(MikoAndroidApp.CreateView(this, builder.Build));
    }
    protected override void OnDestroy() { _player?.Dispose(); base.OnDestroy(); }
}
