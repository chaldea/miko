using Miko.Common;
using Miko.Highlight;

namespace Miko.Components.Editor;

public interface IEditorLanguageService
{
    /// <summary>Return ordered, non-overlapping tokens relative to the requested line. The snapshot can be used for multiline syntax.</summary>
    IReadOnlyList<CodeToken>? TokenizeLine(EditorDocument document, int line);
}

public interface IEditorFormatter
{
    string Format(EditorDocument document);
}

/// <summary>Language IDs are case-insensitive. Register providers before passing the registry to one or more editors.</summary>
public sealed class EditorServices
{
    private readonly Dictionary<string, IEditorLanguageService> _languages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IEditorFormatter> _formatters = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterLanguage(string language, IEditorLanguageService service)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        _languages[language] = service ?? throw new ArgumentNullException(nameof(service));
    }

    public void RegisterFormatter(string language, IEditorFormatter formatter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        _formatters[language] = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    internal IEditorLanguageService? Language(string id) => _languages.GetValueOrDefault(id);
    internal IEditorFormatter? Formatter(string id) => _formatters.GetValueOrDefault(id);
}

public sealed record EditorTheme
{
    public Color Background { get; init; } = Color.FromHex("#fafaf8");
    public Color Foreground { get; init; } = Color.FromHex("#222923");
    public Color Gutter { get; init; } = Color.FromHex("#637169");
    public Color Selection { get; init; } = Color.FromHex("#bfdcf6");
    public Color Added { get; init; } = Color.FromHex("#dcf2de");
    public Color Removed { get; init; } = Color.FromHex("#fae0db");
    public SyntaxTheme Syntax { get; init; } = SyntaxTheme.Default;

    public static EditorTheme Light { get; } = new();
    public static EditorTheme Dark { get; } = new()
    {
        Background = Color.FromHex("#1c2425"), Foreground = Color.FromHex("#e0e7e4"),
        Gutter = Color.FromHex("#96a7a2"), Selection = Color.FromHex("#34566b"),
        Added = Color.FromHex("#244432"), Removed = Color.FromHex("#593634"),
        Syntax = new()
        {
            Keyword = Color.FromHex("#84bfff"), String = Color.FromHex("#f7b99b"),
            Comment = Color.FromHex("#99b389"), Number = Color.FromHex("#a5d5bf"),
            Type = Color.FromHex("#8bded2"), Function = Color.FromHex("#ebd799")
        }
    };
}
