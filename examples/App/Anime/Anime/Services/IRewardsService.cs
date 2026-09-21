// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Anime.Models;

namespace Anime.Services;

public interface IRewardsService
{
    int Coins { get; }
    int Claims { get; }
    int DownloadCredits { get; }
    IReadOnlyList<Reward> Rewards { get; }
    IReadOnlyList<Redemption> Redemptions { get; }
    string ClaimCoins();
    string ClaimDownloadCredits();
    string Redeem(string id);
}
