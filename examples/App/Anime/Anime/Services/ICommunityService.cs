// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Anime.Models;
using Miko.Components.Player.Danmaku;

namespace Anime.Services;

public interface ICommunityService
{
    IReadOnlyList<AnimeComment> Comments(int id);
    bool AddComment(int id, string text);
    IReadOnlyList<DanmakuItem> Danmaku(int id);
    bool SendDanmaku(int id, DanmakuItem item);
}
