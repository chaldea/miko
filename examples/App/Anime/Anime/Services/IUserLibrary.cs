// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Anime.Models;

namespace Anime.Services;

public interface IUserLibrary
{
    UserProfile Profile { get; }
    IReadOnlyList<AnimeTitle> Favorites { get; }
    IReadOnlyList<WatchRecord> History { get; }
    bool IsFavorite(int id);
    void ToggleFavorite(int id);
    void RecordWatch(int id, int episode);
    void RemoveHistory(int id);
    void UpdateProfile(string nickname, string email);
    void SignOut();
    void SignIn();
}
