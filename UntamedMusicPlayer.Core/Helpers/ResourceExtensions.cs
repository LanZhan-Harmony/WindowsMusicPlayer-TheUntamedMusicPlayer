namespace UntamedMusicPlayer.Core.Helpers;

public static class ResourceExtensions
{
    private static Func<string, string> _resolver = static resourceKey => resourceKey;

    /// <summary>
    /// Configures the presentation layer's resource resolver without making Core depend on it.
    /// </summary>
    public static void Configure(Func<string, string> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        Volatile.Write(ref _resolver, resolver);
    }

    extension(string resourceKey)
    {
        public string GetLocalized() => Volatile.Read(ref _resolver)(resourceKey);

        public string GetLocalizedWithReplace(string placeholder, string value)
        {
            var template = resourceKey.GetLocalized();
            return template.Replace(placeholder, value);
        }

        public string GetLocalizedWithReplace(IDictionary<string, string> replacements)
        {
            var template = resourceKey.GetLocalized();
            foreach (var (placeholder, value) in replacements)
            {
                template = template.Replace(placeholder, value);
            }
            return template;
        }
    }
}
