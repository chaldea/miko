// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Components;
using Miko.Components.Player;
using Miko.Core;

namespace Anime.Components;

/// <summary>
/// Hosts the page-owned MikoPlayer without recreating its native video session when
/// sibling forms change. All controls and rendering are provided by MikoPlayer.
/// The detail page owns disposal. No application DOM is constructed here.
/// </summary>
public sealed class PlaybackSurface : ComponentBase
{
    [Parameter] public MikoPlayer Player { get; set; } = null!;

    public override Element Build() => Player.Build();
}
