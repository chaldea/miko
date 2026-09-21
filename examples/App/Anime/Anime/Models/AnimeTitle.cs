// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Anime.Models;

public sealed record AnimeTitle(
    int Id,
    string Title,
    string Kind,
    string Genre,
    int Year,
    int Season,
    int Weekday,
    int Episodes,
    double Rating,
    string Description,
    string Poster,
    string UpdateTime)
{
    public string PosterSource => $"res://Assets/{Poster}";
    public string ThumbnailSource => $"res://Assets/thumbnail-{Id}.jpg";
    public string Status => $"全 {Episodes} 集";
}

public sealed record FeaturedAnime(int AnimeId, string ImageSource);
public sealed record UserProfile(string Nickname, string Email, string Avatar, bool SignedIn = true);
public sealed record WatchRecord(int AnimeId, int Episode, DateTimeOffset WatchedAt);
public sealed record AnimeComment(string Author, string Text, string Avatar, string TimeLabel);
public sealed record Reward(string Id, string Title, int Cost, string Description);
public sealed record Redemption(string Title, int Cost, DateTimeOffset CreatedAt);
