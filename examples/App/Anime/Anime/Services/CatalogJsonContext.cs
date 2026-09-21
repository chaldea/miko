// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Anime.Models;

namespace Anime.Services;

internal sealed record CatalogSeed(
    List<AnimeTitle> Titles,
    FeaturedAnime Featured,
    UserProfile Profile,
    List<int> Favorites,
    List<WatchRecord> History,
    List<AnimeComment> Comments,
    List<Reward> Rewards);

// Source generation keeps the embedded mock catalog usable with mobile trimming/AOT.
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CatalogSeed))]
internal partial class CatalogJsonContext : JsonSerializerContext;
