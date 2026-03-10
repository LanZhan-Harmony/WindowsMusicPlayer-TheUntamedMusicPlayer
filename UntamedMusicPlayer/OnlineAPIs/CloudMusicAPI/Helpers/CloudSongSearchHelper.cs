using System.Text.Json;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Services;
using ZLogger;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudSongSearchHelper
{
    private static readonly ILogger _logger = LoggingService.CreateLogger<CloudSongSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);
    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task SearchSongsAsync(string keyWords, CloudOnlineSongInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            list.Page = 0;
            list.ListCount = 0;
            list.HasAllLoaded = false;
            list.Clear();
            list.SearchedSongIDs.Clear();
            list.KeyWords = keyWords;

            var (songs, songCount) = await SearchInternalAsync(keyWords, 0);
            list.SongCount = songCount;

            if (songCount == 0)
            {
                list.HasAllLoaded = true;
                return;
            }

            await ProcessSongsAsync(songs, list);
            list.Page = 1;

            // 如果加载后的歌曲数量没达到Limit且还有更多，则继续加载更多
            while (list.Count < CloudOnlineSongInfoList.Limit && !list.HasAllLoaded)
            {
                var (moreSongs, _) = await SearchInternalAsync(
                    list.KeyWords,
                    list.Page * CloudOnlineSongInfoList.Limit
                );
                if (moreSongs.GetArrayLength() > 0)
                {
                    await ProcessSongsAsync(moreSongs, list);
                    list.Page++;
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("搜索失败", ex);
        }
        finally
        {
            _searchSemaphore.Release();
        }
    }

    public static async Task SearchMoreSongsAsync(CloudOnlineSongInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (songs, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlineSongInfoList.Limit
            );
            await ProcessSongsAsync(songs, list);
            list.Page++;
        }
        catch (Exception ex)
        {
            throw new Exception("搜索更多失败", ex);
        }
        finally
        {
            _searchSemaphore.Release();
        }
    }

    private static async Task<(JsonElement Songs, int SongCount)> SearchInternalAsync(
        string keyWords,
        int offset
    )
    {
        var (_, result) = await _api.RequestAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                { "keywords", keyWords },
                { "type", "1" },
                { "limit", $"{CloudOnlineSongInfoList.Limit}" },
                { "offset", $"{offset}" },
            }
        );

        using var document = JsonDocument.Parse(result.ToJsonString());
        var root = document.RootElement;

        if (!root.TryGetProperty("result", out var resultElement))
        {
            throw new Exception("获取搜索结果失败");
        }

        resultElement.TryGetProperty("songCount", out var songCountElement);
        var songCount =
            songCountElement.ValueKind == JsonValueKind.Number ? songCountElement.GetInt32() : 0;

        if (!resultElement.TryGetProperty("songs", out var songsElement))
        {
            if (songCount == 0)
            {
                return (default, 0);
            }
            throw new Exception("获取歌曲列表失败");
        }

        return (songsElement.Clone(), songCount); // 使用Clone()来确保JsonElement在外部使用时仍然有效
    }

    private static async Task ProcessSongsAsync(
        JsonElement songsElement,
        CloudOnlineSongInfoList list
    )
    {
        var actualCount =
            songsElement.ValueKind == JsonValueKind.Array ? songsElement.GetArrayLength() : 0;
        if (actualCount == 0)
        {
            return;
        }

        var songIds = Enumerable
            .Range(0, actualCount)
            .Select(i => songsElement[i].GetProperty("id").GetInt64())
            .ToArray();

        var (_, checkResult) = await _api.RequestAsync(
            CloudMusicApiProviders.SongUrl,
            new Dictionary<string, string> { { "id", string.Join(',', songIds) } }
        );

        var availabilityMap =
            checkResult["data"]
                ?.AsArray()
                .ToDictionary(
                    item => item!["id"]!.GetValue<long>(),
                    item => item!["url"] is not null
                )
            ?? [];

        for (var i = 0; i < actualCount; i++)
        {
            try
            {
                var songElement = songsElement[i];
                var songId = songIds[i];
                var available = availabilityMap.GetValueOrDefault(songId, false);
                list.Add(new BriefCloudOnlineSongInfo(songElement, available));
            }
            catch (Exception ex)
            {
                list.ListCount++;
                _logger.ZLogInformation(ex, $"处理网易云歌曲信息失败");
            }
        }
    }

    public static async Task<List<BriefCloudOnlineSongInfo>> SearchSongsByIDsAsync(long[] IDs)
    {
        if (IDs == null || IDs.Length == 0)
        {
            return [];
        }

        var idString = string.Join(',', IDs);
        var (_, checkResult) = await _api.RequestAsync(
            CloudMusicApiProviders.SongUrl,
            new Dictionary<string, string> { { "id", idString } }
        );
        var (_, detailsResult) = await _api.RequestAsync(
            CloudMusicApiProviders.SongDetail,
            new Dictionary<string, string> { { "ids", idString } }
        );

        var availabilityMap =
            checkResult["data"]
                ?.AsArray()
                .ToDictionary(
                    item => item!["id"]!.GetValue<long>(),
                    item => item!["url"] is not null
                )
            ?? [];

        var detailsMap =
            detailsResult["songs"]
                ?.AsArray()
                .ToDictionary(item => item!["id"]!.GetValue<long>(), item => item)
            ?? [];

        var result = new List<BriefCloudOnlineSongInfo>();
        foreach (var songId in IDs)
        {
            if (!availabilityMap.GetValueOrDefault(songId, false))
            {
                continue;
            }

            if (!detailsMap.TryGetValue(songId, out var trackElement) || trackElement == null)
            {
                continue;
            }

            try
            {
                result.Add(new BriefCloudOnlineSongInfo(trackElement));
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"通过ID获取网易云歌曲信息失败: {songId}");
            }
        }
        return result;
    }
}
