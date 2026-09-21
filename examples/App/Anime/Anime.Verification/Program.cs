// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Anime.Services;
using Miko.Components.Player.Danmaku;
using Miko.Core.DomElements;
using Miko.Platform.Video;
using SkiaSharp;
using static Anime.Verification.AppHarness;

using var app = new Anime.Verification.AppHarness(args.FirstOrDefault(a => a != "--ios"), args.Contains("--ios"));
var catalog = app.Service<IAnimeCatalog>();
var library = app.Service<IUserLibrary>();
var community = app.Service<ICommunityService>();
var rewards = app.Service<IRewardsService>();

app.Capture("home");
app.ClickText("ion-tab-button", "福利");
Require(app.Route == "/vip", "The rewards tab did not navigate.");
app.Capture("vip");
app.ClickText("ion-tab-button", "我的");
Require(app.Route == "/user", "The user tab did not navigate.");
app.Capture("user");
app.ClickText("ion-tab-button", "首页");
Require(app.Route == "/home", "The home tab did not navigate.");

app.ClickText("ion-segment-button", "日漫");
app.Capture("home-list");
var seriesCount = app.Find("poster-card").Count;
app.ClickText("filter-button", "2026");
Require(app.Find("poster-card").Count < seriesCount, "Year filter did not change the catalog.");
app.Enter("home-search", "小书痴");
Require(app.Find("poster-card").Count == 1, "Search did not filter the catalog.");
app.ClickClass("poster-card");
Require(app.Route == "/detail" && app.Service<AnimeNavigator>().SelectedId == 13,
    "Poster did not open the selected title.");
app.Navigate("/home");
app.ClickText("ion-segment-button", "剧场版");
Require(app.Find("poster-card").Count == catalog.Titles.Count(t => t.Kind == "剧场版"), "Movie category mismatch.");
app.Capture("home-movie");
app.Navigate("/trending");
app.Capture("trending");
app.ClickText("ion-segment-button", "一月新番");
Require(app.Find("ranking-card").Count == catalog.Trending(1).Count, "Season filter failed.");
app.Navigate("/timetable");
app.Capture("timetable");
app.ClickText("ion-segment-button", "一");
Require(app.Text.Contains(catalog.Scheduled(1)[0].Title, StringComparison.Ordinal), "Weekday filter failed.");

app.Navigate("/profile");
app.Capture("profile");
app.Enter("profile-nickname", "追番测试员");
app.Enter("profile-email", "invalid-email");
app.ClickClass("save-profile");
Require(library.Profile.Nickname != "追番测试员" && app.Text.Contains("有效邮箱"), "Invalid profile accepted.");
app.Enter("profile-email", "viewer@example.com");
app.ClickClass("save-profile");
Require(library.Profile.Nickname == "追番测试员" && library.Profile.Email == "viewer@example.com", "Profile was not saved.");
app.ClickText("ion-button", "退出登录");
Require(!library.Profile.SignedIn, "Sign out failed.");
app.ClickText("ion-button", "登录演示账号");
Require(library.Profile.SignedIn, "Sign in failed.");
app.Navigate("/user");
Require(app.Text.Contains("追番测试员"), "Saved profile was not reflected on the user page.");
app.Navigate("/favorites");
app.Capture("favorites");
var favoriteCount = library.Favorites.Count;
app.ClickText("ion-button", "编辑");
app.ClickText("ion-button", "移除");
Require(library.Favorites.Count == favoriteCount - 1 && app.Route == "/favorites", "Favorite removal failed.");
app.Navigate("/history");
app.Capture("history");
var historyCount = library.History.Count;
app.ClickText("ion-button", "编辑");
app.ClickText("ion-button", "删除");
Require(library.History.Count == historyCount - 1 && app.Route == "/history", "History removal failed.");

app.Navigate("/vip");
app.ClickText("reward-card", "7天免广告");
Require(rewards.Redemptions.Count == 0 && app.Text.Contains("金币不足"), "Insufficient balance was not handled.");
for (var claim = 0; claim < 4; claim++)
{
    app.ClickClass("claim-coins");
}
Require(rewards.Coins == 90 && rewards.Claims == 3, "Daily reward limit failed.");
app.ClickText("reward-card", "7天免广告");
Require(rewards.Coins == 0 && rewards.Redemptions.Count == 1, "Reward redemption failed.");
app.ClickText("ion-button", "兑换记录");
Require(app.Find("ion-item").Any(e => AllText(e).Contains("90 金币")), "Redemption record is missing.");

app.Service<AnimeNavigator>().Open(13);
app.Capture("detail");
var video = app.Root.FindByTagName("video").OfType<VideoElement>().Single();
var session = video.Session;
Require(session is not null && session.Duration > TimeSpan.Zero && session.State != VideoSessionState.Error,
    "The bundled video did not load through the native decoder.");
app.ClickText("episode-button", "第 2 集");
Require(library.History.First().Episode == 2, "Episode selection did not update history.");
var wasFavorite = library.IsFavorite(13);
app.ClickClass("favorite-toggle");
Require(library.IsFavorite(13) != wasFavorite, "Detail favorite toggle failed.");
app.ClickText("ion-segment-button", "评论");
app.Capture("detail-comments");
var comments = community.Comments(13).Count;
app.ClickClass("send-comment");
Require(community.Comments(13).Count == comments && app.Text.Contains("请输入1至300字"), "Blank comment was accepted.");
app.Enter("comment-input", "这是一条通过输入框发布的评论。");
app.ClickClass("send-comment");
Require(community.Comments(13).Count == comments + 1 && app.Text.Contains("通过输入框发布"), "Comment submission failed.");
Require(ReferenceEquals(video.Session, session), "Posting a comment recreated the video session.");

app.ClickClass("open-danmaku");
app.Capture("detail-danmaku");
app.ClickClass("send-danmaku");
app.Capture("danmaku-validation");
Require(community.Danmaku(13).Count == 0 && app.Text.Contains("请输入1至60字"), "Blank danmaku was accepted.");
app.Enter("danmaku-input", "一起追番吧");
app.ClickText("ion-segment-button", "顶部弹幕");
app.Click(app.Find("danmaku-color")[1]);
app.ClickClass("send-danmaku");
var danmaku = community.Danmaku(13).Single();
Require(danmaku.Mode == DanmakuMode.Top && danmaku.Color == SKColor.Parse("#ff4545") && danmaku.Time > TimeSpan.Zero,
    "Danmaku lost its selected mode, color or playback timestamp.");
Require(!app.Find("danmaku-modal").Any(e => !e.HasClass("overlay-hidden")), "Danmaku panel did not close.");
app.ClickText("ion-segment-button", "简介");
app.ClickClass("enter-fullscreen");
app.Resize(844, 390);
app.Capture("detail-fullscreen");
Require(app.Bounds(app.Find("miko-player").Single()).Height == 390, "Player did not fill the landscape viewport.");
app.ClickClass("fullscreen-danmaku");
app.Capture("detail-fullscreen-danmaku");
// Real pointer clicks must hit the modal above the fullscreen video.
app.Enter("danmaku-input", "横屏弹幕");
app.ClickClass("send-danmaku");
Require(community.Danmaku(13).Count == 2, "Fullscreen danmaku panel was hidden or not interactive.");
Require(ReferenceEquals(video.Session, session), "Fullscreen or danmaku recreated the video session.");
app.ClickClass("exit-fullscreen");
app.Resize(390, 844);
app.Navigate("/home");
Require(video.Session is null, "Navigating away leaked the video session.");
app.Service<AnimeNavigator>().Open(13);
app.Pump();
Require(!ReferenceEquals(video, app.Root.FindByTagName("video").Single()), "Reopening detail reused a disposed player.");
Require(app.Text.Contains("当前第 2 集"), "Selected episode was not restored.");
app.Service<AnimeNavigator>().Open(14);
app.Pump();
Require(app.Text.Contains("白兔糖") && !app.Text.Contains("当前第 2 集"), "Opening a recommendation on the detail route did not update the title.");

app.Navigate("/home");
app.Resize(320, 740);
app.Capture("home-narrow");
app.Navigate("/vip");
app.Capture("vip-narrow");
app.Navigate("/user");
app.Capture("user-narrow");
Console.WriteLine($"PASS: navigation, catalog, profile, library, rewards, video and danmaku; {app.Captures} screenshots in {app.Output}");
