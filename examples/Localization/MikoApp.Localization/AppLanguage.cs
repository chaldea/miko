using System.Globalization;

namespace MikoApp.Localization;

/// <summary>A language the app offers, labelled in its own script.</summary>
public sealed record LanguageOption(string CultureName, string NativeName);

/// <summary>
/// The app's current language. Switching it changes what <c>IStringLocalizer</c> returns and how
/// dates and numbers are formatted, and raises <see cref="Changed"/> so the UI re-renders.
/// </summary>
public sealed class AppLanguage
{
    public static IReadOnlyList<LanguageOption> Options { get; } =
    [
        new("en-US", "English"),
        new("zh-CN", "简体中文"),
        new("ar-EG", "العربية"),
        new("he-IL", "עברית"),
        new("hi-IN", "हिन्दी"),
    ];

    public AppLanguage(string? initialCulture = null)
    {
        Apply(Resolve(initialCulture));
    }

    public CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;

    /// <summary>Arabic and Hebrew lay the UI out right to left.</summary>
    public bool IsRightToLeft => Culture.TextInfo.IsRightToLeft;

    public event Action? Changed;

    public void Set(string cultureName)
    {
        if (cultureName == Culture.Name) return;
        Apply(CultureInfo.GetCultureInfo(cultureName));
        Changed?.Invoke();
    }

    /// <summary>
    /// Sets the process-wide default cultures. Assigning <see cref="CultureInfo.CurrentUICulture"/>
    /// inside a click handler would not last: it flows with the async context and is restored when
    /// the handler returns, while the defaults are what the render thread falls back to.
    /// </summary>
    private void Apply(CultureInfo culture)
    {
        Culture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    /// <summary>The requested culture, else the OS language when the app offers it, else English.</summary>
    private static CultureInfo Resolve(string? requested)
    {
        var wanted = requested ?? CultureInfo.CurrentUICulture.Name;
        var match = Options.FirstOrDefault(o => o.CultureName.Equals(wanted, StringComparison.OrdinalIgnoreCase))
            ?? Options.FirstOrDefault(o => o.CultureName.StartsWith(wanted.Split('-')[0] + "-", StringComparison.OrdinalIgnoreCase))
            ?? Options[0];
        return CultureInfo.GetCultureInfo(match.CultureName);
    }
}
