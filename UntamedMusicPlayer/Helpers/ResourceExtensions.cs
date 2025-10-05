using Microsoft.Windows.ApplicationModel.Resources;

namespace UntamedMusicPlayer.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceLoader _resourceLoader = new();

    public static string GetLocalized(this string resourceKey) =>
        _resourceLoader.GetString(resourceKey);

    public static string GetLocalizedWithReplace(
        this string resourceKey,
        string placeholder,
        string value
    )
    {
        var template = _resourceLoader.GetString(resourceKey);
        return template.Replace(placeholder, value);
    }

    public static string GetLocalizedWithReplace(
        this string resourceKey,
        IDictionary<string, string> replacements
    )
    {
        var template = _resourceLoader.GetString(resourceKey);
        foreach (var (placeholder, value) in replacements)
        {
            template = template.Replace(placeholder, value);
        }
        return template;
    }
}
