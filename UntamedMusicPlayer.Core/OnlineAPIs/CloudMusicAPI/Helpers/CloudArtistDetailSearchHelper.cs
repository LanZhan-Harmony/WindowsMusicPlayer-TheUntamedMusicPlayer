using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudArtistDetailSearchHelper
{
    private static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<CloudArtistDetailSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    public static async Task<DetailedCloudOnlineArtistInfo> SearchArtistDetailAsync(
        BriefCloudOnlineArtistInfo briefInfo,
        CloudMusicApiService api
    )
    {
        await _searchSemaphore.WaitAsync();
        var info = new DetailedCloudOnlineArtistInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            CoverPath = briefInfo.CoverPath,
        };
        try
        {
            var artistTask = api.GetArtistDescriptionAsync(briefInfo.ID);

            var (albumsElement, totalAlbumNum, totalSongNum) = await SearchAlbumsInternalAsync(
                briefInfo.ID,
                0,
                api
            );

            var (_, artistResult) = await artistTask;
            info.Introduction = artistResult?.BriefDescription;
            info.TotalAlbumNum = totalAlbumNum;
            info.TotalSongNum = totalSongNum;
            info.CountStr = IDetailedOnlineArtistInfo.GetCountStr(
                info.TotalAlbumNum,
                info.TotalSongNum
            );

            if (totalAlbumNum == 0)
            {
                info.HasAllLoaded = true;
                return info;
            }

            await ProcessArtistDetailAsync(albumsElement, info, api);
            info.Page = 1;

            // 如果加载后的数量没达到Limit且还有更多，则继续加载更多
            while (info.AlbumList.Count < DetailedCloudOnlineArtistInfo.Limit && !info.HasAllLoaded)
            {
                var (moreAlbums, _, _) = await SearchAlbumsInternalAsync(
                    info.ID,
                    info.Page * DetailedCloudOnlineArtistInfo.Limit,
                    api
                );
                if (moreAlbums.Count > 0)
                {
                    await ProcessArtistDetailAsync(moreAlbums, info, api);
                    info.Page++;
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"搜索艺术家详情失败: {briefInfo.Name}");
        }
        finally
        {
            _searchSemaphore.Release();
        }
        return info;
    }

    public static async Task SearchMoreArtistDetailAsync(
        DetailedCloudOnlineArtistInfo info,
        CloudMusicApiService api
    )
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (albums, _, _) = await SearchAlbumsInternalAsync(
                info.ID,
                info.Page * DetailedCloudOnlineArtistInfo.Limit,
                api
            );
            await ProcessArtistDetailAsync(albums, info, api);
            info.Page++;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"搜索更多艺术家详情失败: {info.Name}, Page: {info.Page}");
        }
        finally
        {
            _searchSemaphore.Release();
        }
    }

    private static async Task<(
        List<CloudArtistAlbumDto> Albums,
        int TotalAlbumNum,
        int TotalSongNum
    )> SearchAlbumsInternalAsync(long artistId, int offset, CloudMusicApiService api)
    {
        var (_, albumResult) = await api.GetArtistAlbumsAsync(
            artistId,
            DetailedCloudOnlineArtistInfo.Limit,
            offset
        );

        return (
            albumResult?.Albums ?? [],
            albumResult?.Artist?.AlbumSize ?? 0,
            albumResult?.Artist?.MusicSize ?? 0
        );
    }

    private static async Task ProcessArtistDetailAsync(
        List<CloudArtistAlbumDto> albumsElement,
        DetailedCloudOnlineArtistInfo info,
        CloudMusicApiService api
    )
    {
        var actualCount = albumsElement.Count;
        if (actualCount == 0)
        {
            return;
        }

        var albumInfos = new CloudOnlineArtistAlbumInfo[actualCount];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await Parallel.ForEachAsync(
            Enumerable.Range(0, actualCount),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2,
                CancellationToken = cts.Token,
            },
            async (i, cancellationToken) =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    albumInfos[i] = await CloudMusicModelFactory.CreateArtistAlbumAsync(
                        albumsElement[i],
                        api,
                        isDetailed: true
                    );
                }
                catch (Exception ex)
                {
                    lock (info.AlbumList)
                    {
                        info.CurrentAlbumNum++;
                    }
                    _logger.ZLogInformation(ex, $"处理网易云艺术家详细信息失败: {info.Name}");
                }
            }
        );

        foreach (var albumInfo in albumInfos)
        {
            info.Add(albumInfo);
        }
    }

    public static async Task<List<IBriefSongInfoBase>> GetSongsByArtistAsync(
        BriefCloudOnlineArtistInfo info,
        CloudMusicApiService api
    )
    {
        var songs = new List<IBriefSongInfoBase>();
        try
        {
            var (albumsElement, _, _) = await SearchAlbumsInternalAsync(info.ID, 0, api);
            var actualCount = albumsElement.Count;
            if (actualCount == 0)
            {
                return songs;
            }

            var albumInfos = new CloudOnlineArtistAlbumInfo[actualCount];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await Parallel.ForEachAsync(
                Enumerable.Range(0, actualCount),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount / 2,
                    CancellationToken = cts.Token,
                },
                async (i, cancellationToken) =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        albumInfos[i] = await CloudMusicModelFactory.CreateArtistAlbumAsync(
                            albumsElement[i],
                            api,
                            isDetailed: false
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogInformation(ex, $"获取网易云艺术家专辑歌曲失败: {info.Name}");
                    }
                }
            );

            foreach (var album in albumInfos)
            {
                if (album is { IsAvailable: true })
                {
                    songs.AddRange(album.SongList);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"获取网易云艺术家歌曲失败: {info.Name}");
        }
        return songs;
    }
}
