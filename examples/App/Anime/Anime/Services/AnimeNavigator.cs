// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Routing;

namespace Anime.Services;

/// <summary>The template router uses exact routes; keep the selected title in navigation state.</summary>
public sealed class AnimeNavigator(NavigationManager navigation, IAnimeCatalog catalog)
{
    public int? SelectedId { get; private set; }
    public event Action? SelectionChanged;

    public void Open(int id)
    {
        if (catalog.Find(id) is null) return;
        SelectedId = id;
        if (navigation.CurrentPath == "/detail")
        {
            SelectionChanged?.Invoke();
        }
        else
        {
            navigation.NavigateTo("/detail");
        }
    }
}
