// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Anime.Models;
using Miko.Components.Player.Danmaku;

namespace Anime.Services;

/// <summary>In-memory mock backend. Pages depend on interfaces; seed data lives in embedded JSON.</summary>
public sealed class MockAnimeStore : IAnimeCatalog, IUserLibrary, ICommunityService, IRewardsService
{
    private readonly HashSet<int> _favorites;
    private readonly List<WatchRecord> _history;
    private readonly Dictionary<int, List<AnimeComment>> _comments = [];
    private readonly Dictionary<int, List<DanmakuItem>> _danmaku = [];
    private readonly List<Redemption> _redemptions = [];
    private readonly List<AnimeComment> _seedComments;
    private DateOnly _rewardDay = DateOnly.FromDateTime(DateTime.Now);
    private int _claims;
    private bool _downloadClaimed;

    public MockAnimeStore()
    {
        using var stream = typeof(App).Assembly.GetManifestResourceStream("Anime.Assets.catalog.json")
            ?? throw new InvalidOperationException("The bundled catalog is missing.");
        var seed = JsonSerializer.Deserialize(stream, CatalogJsonContext.Default.CatalogSeed)
            ?? throw new InvalidOperationException("The bundled catalog is invalid.");
        Titles = seed.Titles.AsReadOnly();
        Featured = seed.Featured;
        Profile = seed.Profile;
        Rewards = seed.Rewards.AsReadOnly();
        _favorites = seed.Favorites.ToHashSet();
        _history = seed.History;
        _seedComments = seed.Comments;
    }

    public IReadOnlyList<AnimeTitle> Titles { get; }
    public FeaturedAnime Featured { get; }
    public UserProfile Profile { get; private set; }
    public IReadOnlyList<AnimeTitle> Favorites => Titles.Where(t => _favorites.Contains(t.Id)).ToArray();
    public IReadOnlyList<WatchRecord> History => _history.OrderByDescending(h => h.WatchedAt).ToArray();
    public int Coins { get; private set; }
    public int Claims
    {
        get
        {
            ResetDailyRewards();
            return _claims;
        }
    }
    public int DownloadCredits { get; private set; }
    public IReadOnlyList<Reward> Rewards { get; }
    public IReadOnlyList<Redemption> Redemptions => _redemptions.AsReadOnly();

    public AnimeTitle? Find(int id) => Titles.FirstOrDefault(t => t.Id == id);

    public IReadOnlyList<AnimeTitle> Search(string kind, string genre, string year, string query, bool byRating)
    {
        var titles = Titles.Where(t => (kind == "精选" || t.Kind == kind)
            && (genre == "全部" || t.Genre.Contains(genre, StringComparison.Ordinal))
            && (year == "全部" || t.Year.ToString() == year)
            && t.Title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));
        return (byRating ? titles.OrderByDescending(t => t.Rating) : titles.OrderByDescending(t => t.Year)).ToArray();
    }

    public IReadOnlyList<AnimeTitle> Trending(int season) => Titles
        .Where(t => t.Season == season).OrderByDescending(t => t.Rating).ToArray();

    public IReadOnlyList<AnimeTitle> Scheduled(int weekday) => Titles.Where(t => t.Weekday == weekday).ToArray();
    public bool IsFavorite(int id) => _favorites.Contains(id);

    public void ToggleFavorite(int id)
    {
        if (Find(id) is null) return;
        if (!_favorites.Remove(id)) _favorites.Add(id);
    }

    public void RecordWatch(int id, int episode)
    {
        var title = Find(id);
        if (title is null || episode < 1 || episode > title.Episodes) return;
        _history.RemoveAll(h => h.AnimeId == id);
        _history.Add(new WatchRecord(id, episode, DateTimeOffset.Now));
    }

    public void RemoveHistory(int id) => _history.RemoveAll(h => h.AnimeId == id);

    public void UpdateProfile(string nickname, string email)
    {
        if (string.IsNullOrWhiteSpace(nickname)) throw new ArgumentException("昵称不能为空。");
        if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out _)) throw new ArgumentException("请输入有效邮箱。");
        Profile = Profile with { Nickname = nickname.Trim(), Email = email.Trim() };
    }

    public void SignOut() => Profile = Profile with { SignedIn = false };
    public void SignIn() => Profile = Profile with { SignedIn = true };

    public IReadOnlyList<AnimeComment> Comments(int id) => CommentList(id).AsReadOnly();

    public bool AddComment(int id, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 300 || Find(id) is null) return false;
        CommentList(id).Insert(0, new AnimeComment(Profile.Nickname, text.Trim(), Profile.Avatar, "刚刚"));
        return true;
    }

    private List<AnimeComment> CommentList(int id)
    {
        if (!_comments.TryGetValue(id, out var comments)) _comments[id] = comments = [.. _seedComments];
        return comments;
    }

    public IReadOnlyList<DanmakuItem> Danmaku(int id) => _danmaku.TryGetValue(id, out var items) ? items.ToArray() : [];

    public bool SendDanmaku(int id, DanmakuItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Text) || item.Text.Length > 60 || Find(id) is null) return false;
        if (!_danmaku.TryGetValue(id, out var items)) _danmaku[id] = items = [];
        items.Add(item with { Text = item.Text.Trim() });
        return true;
    }

    public string ClaimCoins()
    {
        ResetDailyRewards();
        if (_claims >= 3) return "今日任务已完成，明天再来吧。";
        _claims++;
        Coins += 30;
        return "模拟任务完成，获得 30 金币。";
    }

    public string ClaimDownloadCredits()
    {
        ResetDailyRewards();
        if (_downloadClaimed) return "今日下载次数奖励已领取。";
        _downloadClaimed = true;
        DownloadCredits += 5;
        return "已获得 5 次模拟下载额度。";
    }

    public string Redeem(string id)
    {
        var reward = Rewards.FirstOrDefault(r => r.Id == id);
        if (reward is null) return "兑换项目不存在。";
        if (Coins < reward.Cost) return "金币不足，完成任务后再来兑换。";
        Coins -= reward.Cost;
        _redemptions.Insert(0, new Redemption(reward.Title, reward.Cost, DateTimeOffset.Now));
        return $"已兑换：{reward.Title}（本地演示）。";
    }

    private void ResetDailyRewards()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (_rewardDay == today) return;
        _rewardDay = today;
        _claims = 0;
        _downloadClaimed = false;
    }

}
