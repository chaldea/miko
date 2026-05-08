namespace Miko.Routing;

public class NavigationManager
{
    private string _currentPath = "/";

    public string CurrentPath => _currentPath;

    public event Action<string>? LocationChanged;

    public void NavigateTo(string path)
    {
        if (_currentPath == path) return;

        _currentPath = path;
        LocationChanged?.Invoke(path);
    }
}
