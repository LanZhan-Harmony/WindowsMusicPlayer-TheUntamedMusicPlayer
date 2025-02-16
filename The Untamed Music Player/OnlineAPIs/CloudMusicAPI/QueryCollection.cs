namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
internal sealed partial class QueryCollection : List<KeyValuePair<string, string>>
{
    public void Add(string key, string value) => Add(new KeyValuePair<string, string>(key, value));
}
