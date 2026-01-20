using Microsoft.Windows.ApplicationModel.Resources;

namespace UntamedMusicPlayer.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceLoader _resourceLoader = new();

    extension(string resourceKey)
    {
        public string GetLocalized() => _resourceLoader.GetString(resourceKey);

        public string GetLocalizedWithReplace(string placeholder, string value)
        {
            var template = _resourceLoader.GetString(resourceKey);
            return template.Replace(placeholder, value);
        }

        public string GetLocalizedWithReplace(IDictionary<string, string> replacements)
        {
            var template = _resourceLoader.GetString(resourceKey);
            foreach (var (placeholder, value) in replacements)
            {
                template = template.Replace(placeholder, value);
            }
            return template;
        }
    }
}
