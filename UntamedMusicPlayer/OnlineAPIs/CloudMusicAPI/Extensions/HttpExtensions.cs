namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Extensions;

internal static class HttpExtensions
{
    extension(IEnumerable<KeyValuePair<string, string>> queries)
    {
        public string ToQueryString()
        {
            ArgumentNullException.ThrowIfNull(queries);

            return string.Join(
                "&",
                queries.Select(t =>
                    Uri.EscapeDataString(t.Key) + "=" + Uri.EscapeDataString(t.Value)
                )
            );
        }
    }
}
