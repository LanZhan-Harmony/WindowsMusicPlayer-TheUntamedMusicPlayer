namespace UntamedMusicPlayer.OnlineAPI.KuwoMusicAPI;

internal sealed partial class QueryCollection : List<KeyValuePair<string, string>>
{
    public QueryCollection() { }

    public QueryCollection(int capacity)
        : base(capacity) { }

    public void Add(string key, string value) => Add(new(key, value));
}
