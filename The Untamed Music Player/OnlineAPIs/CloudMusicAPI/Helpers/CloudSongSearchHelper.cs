using System.Diagnostics;
using System.Text.Json;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;

public class CloudSongSearchHelper
{
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task SearchSongsAsync(string keyWords, CloudOnlineSongInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        list.Page = 0;
        list.ListCount = 0;
        list.HasAllLoaded = false;
        list.Clear();
        list.SearchedSongIDs.Clear();
        list.KeyWords = keyWords;

        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", keyWords },
                    { "type", "1" },
                    { "limit", $"{CloudOnlineSongInfoList.Limit}" },
                    { "offset", "0" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取songCount
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("songCount", out var songCountElement)
            )
            {
                list.SongCount = songCountElement.GetInt32();

                if (list.SongCount == 0)
                {
                    list.HasAllLoaded = true;
                    return;
                }

                // 获取songs数组
                if (resultElement.TryGetProperty("songs", out var songsElement))
                {
                    await ProcessSongsAsync(songsElement, list);
                    list.Page = 1;
                }
                else
                {
                    throw new Exception("获取歌曲列表失败");
                }
            }
            else
            {
                throw new Exception("获取歌曲数量失败");
            }
        }
        catch
        {
            throw new Exception("搜索失败");
        }
        finally
        {
            _searchSemaphore.Release();
            GC.Collect();
        }
    }

    public static async Task SearchMoreSongsAsync(CloudOnlineSongInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", list.KeyWords },
                    { "type", "1" },
                    { "limit", $"{CloudOnlineSongInfoList.Limit}" },
                    { "offset", $"{list.Page * CloudOnlineSongInfoList.Limit}" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取songs数组
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("songs", out var songsElement)
            )
            {
                await ProcessSongsAsync(songsElement, list);
                list.Page++;
            }
            else
            {
                throw new Exception("获取歌曲列表失败");
            }
        }
        catch
        {
            throw new Exception("搜索更多失败");
        }
        finally
        {
            _searchSemaphore.Release();
            GC.Collect();
        }
    }

    private static async Task ProcessSongsAsync(
        JsonElement songsElement,
        CloudOnlineSongInfoList list
    )
    {
        var actualCount = songsElement.GetArrayLength();
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
        var data = checkResult["data"]!;

        // 建立ID到可用性的映射，解决顺序问题
        var availabilityMap = data.AsArray()
            .ToDictionary(item => item!["id"]!.GetValue<long>(), item => item!["url"] is not null);

        var infos = Enumerable
            .Range(0, actualCount)
            .Select(i =>
            {
                try
                {
                    var songId = songIds[i];
                    var available = availabilityMap.GetValueOrDefault(songId, false);
                    return new BriefCloudOnlineSongInfo(songsElement[i], available);
                }
                catch (Exception ex)
                {
                    list.ListCount++;
                    Debug.WriteLine(ex.StackTrace);
                    return null;
                }
            })
            .Where(info => info is not null);

        foreach (var info in infos)
        {
            list.Add(info);
        }
    }

    public static async Task<List<BriefCloudOnlineSongInfo>> SearchSongsByIDsAsync(long[] IDs)
    {
        var list = new List<BriefCloudOnlineSongInfo>();
        var (_, checkResult) = await _api.RequestAsync(
            CloudMusicApiProviders.SongUrl,
            new Dictionary<string, string> { { "id", string.Join(',', IDs) } }
        );
        var (_, detailsResult) = await _api.RequestAsync(
            CloudMusicApiProviders.SongDetail,
            new Dictionary<string, string> { { "ids", string.Join(',', IDs) } }
        );
        var data = checkResult["data"]!;
        var availabilityMap = data.AsArray()
            .ToDictionary(item => item!["id"]!.GetValue<long>(), item => item!["url"] is not null);
        var detailsMap = detailsResult["songs"]!
            .AsArray()
            .ToDictionary(item => item!["id"]!.GetValue<long>(), item => item);

        for (var i = 0; i < IDs.Length; i++)
        {
            var songId = IDs[i];
            var available = availabilityMap.GetValueOrDefault(songId, false);
            if (!available)
            {
                continue;
            }

            var trackElement = detailsMap.GetValueOrDefault(songId)!;

            try
            {
                var songInfo = new BriefCloudOnlineSongInfo(trackElement);
                list.Add(songInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }
        return list;
    }
}
