// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Anime.Models;

namespace Anime.Services;

public interface IAnimeCatalog
{
    IReadOnlyList<AnimeTitle> Titles { get; }
    FeaturedAnime Featured { get; }
    AnimeTitle? Find(int id);
    IReadOnlyList<AnimeTitle> Search(string kind, string genre, string year, string query, bool byRating);
    IReadOnlyList<AnimeTitle> Trending(int season);
    IReadOnlyList<AnimeTitle> Scheduled(int weekday);
}
