using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Utils;

namespace UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;

/// <summary>
/// 网易云音乐API
/// </summary>
public sealed partial class CloudMusicApiService : IDisposable
{
    private readonly HttpClient _client;
    private readonly HttpClientHandler _clientHandler;

    public CloudMusicApiService()
    {
        _clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = true,
        };
        _client = new HttpClient(_clientHandler);
    }

    public Task<(bool IsOk, CloudSearchSongsResponse? Result)> SearchSongsAsync(
        string keywords,
        int limit,
        int offset
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                ["keywords"] = keywords,
                ["type"] = "1",
                ["limit"] = $"{limit}",
                ["offset"] = $"{offset}",
            },
            CloudJsonContext.Default.CloudSearchSongsResponse
        );

    public Task<(bool IsOk, CloudSearchAlbumsResponse? Result)> SearchAlbumsAsync(
        string keywords,
        int limit,
        int offset
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                ["keywords"] = keywords,
                ["type"] = "10",
                ["limit"] = $"{limit}",
                ["offset"] = $"{offset}",
            },
            CloudJsonContext.Default.CloudSearchAlbumsResponse
        );

    public Task<(bool IsOk, CloudSearchArtistsResponse? Result)> SearchArtistsAsync(
        string keywords,
        int limit,
        int offset
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                ["keywords"] = keywords,
                ["type"] = "100",
                ["limit"] = $"{limit}",
                ["offset"] = $"{offset}",
            },
            CloudJsonContext.Default.CloudSearchArtistsResponse
        );

    public Task<(bool IsOk, CloudSearchPlaylistsResponse? Result)> SearchPlaylistsAsync(
        string keywords,
        int limit,
        int offset
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                ["keywords"] = keywords,
                ["type"] = "1000",
                ["limit"] = $"{limit}",
                ["offset"] = $"{offset}",
            },
            CloudJsonContext.Default.CloudSearchPlaylistsResponse
        );

    public Task<(bool IsOk, CloudSearchSuggestResponse? Result)> SearchSuggestionsAsync(
        string keywords
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.SearchSuggest,
            new Dictionary<string, string> { ["keywords"] = keywords },
            CloudJsonContext.Default.CloudSearchSuggestResponse
        );

    public Task<(bool IsOk, CloudSongUrlResponse? Result)> GetSongUrlsAsync(
        IEnumerable<long> songIds
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.SongUrl,
            CreateIdQuery(songIds, "id"),
            CloudJsonContext.Default.CloudSongUrlResponse
        );

    public Task<(bool IsOk, CloudSongDetailResponse? Result)> GetSongDetailsAsync(
        IEnumerable<long> songIds
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.SongDetail,
            CreateIdQuery(songIds, "ids"),
            CloudJsonContext.Default.CloudSongDetailResponse
        );

    public Task<(bool IsOk, CloudAlbumResponse? Result)> GetAlbumAsync(long albumId) =>
        RequestTypedAsync(
            CloudMusicApiProviders.Album,
            new Dictionary<string, string> { ["id"] = $"{albumId}" },
            CloudJsonContext.Default.CloudAlbumResponse
        );

    public Task<(bool IsOk, CloudLyricResponse? Result)> GetLyricAsync(long songId) =>
        RequestTypedAsync(
            CloudMusicApiProviders.Lyric,
            new Dictionary<string, string> { ["id"] = $"{songId}" },
            CloudJsonContext.Default.CloudLyricResponse
        );

    public Task<(bool IsOk, CloudArtistAlbumResponse? Result)> GetArtistAlbumsAsync(
        long artistId,
        int limit,
        int offset
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.ArtistAlbum,
            new Dictionary<string, string>
            {
                ["id"] = $"{artistId}",
                ["limit"] = $"{limit}",
                ["offset"] = $"{offset}",
            },
            CloudJsonContext.Default.CloudArtistAlbumResponse
        );

    public Task<(bool IsOk, CloudArtistDescriptionResponse? Result)> GetArtistDescriptionAsync(
        long artistId
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.ArtistDesc,
            new Dictionary<string, string> { ["id"] = $"{artistId}" },
            CloudJsonContext.Default.CloudArtistDescriptionResponse
        );

    public Task<(bool IsOk, CloudPlaylistDetailResponse? Result)> GetPlaylistDetailAsync(
        long playlistId
    ) =>
        RequestTypedAsync(
            CloudMusicApiProviders.PlaylistDetail,
            new Dictionary<string, string> { ["id"] = $"{playlistId}" },
            CloudJsonContext.Default.CloudPlaylistDetailResponse
        );

    private Task<(bool, JsonObject)> RequestJsonAsync(
        CloudMusicApiProvider provider,
        Dictionary<string, string> queries
    )
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(queries);

        return RequestJsonAsync(
            provider.Method,
            provider.Url(queries),
            provider.Data(queries),
            provider.Options
        );
    }

    private async Task<(bool IsOk, T? Result)> RequestTypedAsync<T>(
        CloudMusicApiProvider provider,
        Dictionary<string, string> queries,
        JsonTypeInfo<T> typeInfo
    )
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        var (isOk, json) = await RequestJsonAsync(provider, queries);
        return (isOk, json.Deserialize(typeInfo));
    }

    private async Task<(bool, JsonObject)> RequestJsonAsync(
        HttpMethod method,
        string url,
        IEnumerable<KeyValuePair<string, string>> data,
        Options options
    )
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);

        var (isOk, json) = await Request.CreateRequest(_client, method, url, data, options);
        if (json["body"] is not JsonObject body)
        {
            return (
                false,
                new JsonObject { { "code", 500 }, { "msg", "响应格式错误：body 不是对象" } }
            );
        }

        json = body;
        if (!isOk && (int?)json["code"] == 301)
        {
            json["msg"] = "需要登录";
        }

        return (isOk, json);
    }

    private static Dictionary<string, string> CreateIdQuery(
        IEnumerable<long> ids,
        string parameterName
    )
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentException.ThrowIfNullOrEmpty(parameterName);

        var idValues = ids.Distinct().ToArray();
        if (idValues.Length == 0)
        {
            throw new ArgumentException("至少需要一个 ID。", nameof(ids));
        }

        return new Dictionary<string, string> { [parameterName] = string.Join(',', idValues) };
    }

    public void Dispose()
    {
        _clientHandler.Dispose();
        _client.Dispose();
    }
}
