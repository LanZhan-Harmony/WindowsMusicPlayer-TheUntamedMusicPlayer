using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudSongSearchHelper
{
    private static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<CloudSongSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    public static async Task SearchSongsAsync(
        string keyWords,
        CloudOnlineSongInfoList list,
        CloudMusicApiService api
    )
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

            var (songs, songCount) = await SearchInternalAsync(keyWords, 0, api);
            list.SongCount = songCount;

            if (songCount == 0)
            {
                list.HasAllLoaded = true;
                return;
            }

            await ProcessSongsAsync(songs, list, api);
            list.Page = 1;

            // 如果加载后的歌曲数量没达到Limit且还有更多，则继续加载更多
            while (list.Count < CloudOnlineSongInfoList.Limit && !list.HasAllLoaded)
            {
                var (moreSongs, _) = await SearchInternalAsync(
                    list.KeyWords,
                    list.Page * CloudOnlineSongInfoList.Limit,
                    api
                );
                if (moreSongs.Count > 0)
                {
                    await ProcessSongsAsync(moreSongs, list, api);
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

    public static async Task SearchMoreSongsAsync(
        CloudOnlineSongInfoList list,
        CloudMusicApiService api
    )
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (songs, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlineSongInfoList.Limit,
                api
            );
            await ProcessSongsAsync(songs, list, api);
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

    private static async Task<(List<CloudSearchSongDto> Songs, int SongCount)> SearchInternalAsync(
        string keyWords,
        int offset,
        CloudMusicApiService api
    )
    {
        var (_, response) = await api.SearchSongsAsync(
            keyWords,
            CloudOnlineSongInfoList.Limit,
            offset
        );

        if (response?.Result is not { } resultElement)
        {
            throw new Exception("获取搜索结果失败");
        }

        var songCount = resultElement.SongCount;
        if (resultElement.Songs is null)
        {
            if (songCount == 0)
            {
                return ([], 0);
            }
            throw new Exception("获取歌曲列表失败");
        }

        return (resultElement.Songs, songCount);
    }

    private static async Task ProcessSongsAsync(
        List<CloudSearchSongDto> songs,
        CloudOnlineSongInfoList list,
        CloudMusicApiService api
    )
    {
        var actualCount = songs.Count;
        if (actualCount == 0)
        {
            return;
        }

        var songIds = songs.Select(song => song.Id).ToArray();

        var (_, checkResult) = await api.GetSongUrlsAsync(songIds);

        var availabilityMap =
            checkResult?.Data?.ToDictionary(item => item.Id, item => item.Url is not null) ?? [];

        for (var i = 0; i < actualCount; i++)
        {
            try
            {
                var songElement = songs[i];
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

    public static async Task<List<BriefCloudOnlineSongInfo>> SearchSongsByIDsAsync(
        long[] IDs,
        CloudMusicApiService api
    )
    {
        if (IDs is null || IDs.Length == 0)
        {
            return [];
        }

        var (_, checkResult) = await api.GetSongUrlsAsync(IDs);
        var (_, detailsResult) = await api.GetSongDetailsAsync(IDs);

        var availabilityMap =
            checkResult?.Data?.ToDictionary(item => item.Id, item => item.Url is not null) ?? [];

        var detailsMap = detailsResult?.Songs?.ToDictionary(item => item.Id) ?? [];

        var result = new List<BriefCloudOnlineSongInfo>();
        foreach (var songId in IDs)
        {
            if (!availabilityMap.GetValueOrDefault(songId, false))
            {
                continue;
            }

            if (!detailsMap.TryGetValue(songId, out var trackElement))
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
