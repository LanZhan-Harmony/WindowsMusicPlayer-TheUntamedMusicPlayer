using System.Text.Json;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Services;
using ZLogger;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudArtistDetailSearchHelper
{
    private static readonly ILogger _logger =
        LoggingService.CreateLogger<CloudArtistDetailSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);
    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task<DetailedCloudOnlineArtistInfo> SearchArtistDetailAsync(
        BriefCloudOnlineArtistInfo briefInfo
    )
    {
        await _searchSemaphore.WaitAsync();
        var info = new DetailedCloudOnlineArtistInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            Cover = briefInfo.Cover,
            CoverPath = briefInfo.CoverPath,
        };
        try
        {
            var artistTask = _api.RequestAsync(
                CloudMusicApiProviders.ArtistDesc,
                new Dictionary<string, string> { { "id", $"{briefInfo.ID}" } }
            );

            var (albumsElement, totalAlbumNum, totalSongNum) = await SearchAlbumsInternalAsync(
                briefInfo.ID,
                0
            );

            var (_, artistResult) = await artistTask;
            info.Introduction = artistResult["briefDesc"]?.ToString();
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

            await ProcessArtistDetailAsync(albumsElement, info);
            info.Page = 1;

            // 如果加载后的数量没达到Limit且还有更多，则继续加载更多
            while (info.AlbumList.Count < DetailedCloudOnlineArtistInfo.Limit && !info.HasAllLoaded)
            {
                var (moreAlbums, _, _) = await SearchAlbumsInternalAsync(
                    info.ID,
                    info.Page * DetailedCloudOnlineArtistInfo.Limit
                );
                if (moreAlbums.GetArrayLength() > 0)
                {
                    await ProcessArtistDetailAsync(moreAlbums, info);
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

    public static async Task SearchMoreArtistDetailAsync(DetailedCloudOnlineArtistInfo info)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (albums, _, _) = await SearchAlbumsInternalAsync(
                info.ID,
                info.Page * DetailedCloudOnlineArtistInfo.Limit
            );
            await ProcessArtistDetailAsync(albums, info);
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
        JsonElement Albums,
        int TotalAlbumNum,
        int TotalSongNum
    )> SearchAlbumsInternalAsync(long artistId, int offset)
    {
        var (_, albumResult) = await _api.RequestAsync(
            CloudMusicApiProviders.ArtistAlbum,
            new Dictionary<string, string>
            {
                { "id", $"{artistId}" },
                { "limit", $"{DetailedCloudOnlineArtistInfo.Limit}" },
                { "offset", $"{offset}" },
            }
        );

        using var document = JsonDocument.Parse(albumResult.ToJsonString());
        var root = document.RootElement;

        var artistElement = root.TryGetProperty("artist", out var artist) ? artist : default;
        var albumsElement = root.TryGetProperty("hotAlbums", out var albums) ? albums : default;

        var totalAlbumNum =
            artist.ValueKind == JsonValueKind.Object
            && artist.TryGetProperty("albumSize", out var albumSize)
                ? albumSize.GetInt32()
                : 0;
        var totalSongNum =
            artist.ValueKind == JsonValueKind.Object
            && artist.TryGetProperty("musicSize", out var musicSize)
                ? musicSize.GetInt32()
                : 0;

        return (albumsElement.Clone(), totalAlbumNum, totalSongNum);
    }

    private static async Task ProcessArtistDetailAsync(
        JsonElement albumsElement,
        DetailedCloudOnlineArtistInfo info
    )
    {
        var actualCount =
            albumsElement.ValueKind == JsonValueKind.Array ? albumsElement.GetArrayLength() : 0;
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
                    albumInfos[i] = await CloudOnlineArtistAlbumInfo.CreateAsync(
                        albumsElement[i],
                        _api,
                        true
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
        BriefCloudOnlineArtistInfo info
    )
    {
        var songs = new List<IBriefSongInfoBase>();
        try
        {
            var (albumsElement, _, _) = await SearchAlbumsInternalAsync(info.ID, 0);
            var actualCount =
                albumsElement.ValueKind == JsonValueKind.Array ? albumsElement.GetArrayLength() : 0;
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
                        albumInfos[i] = await CloudOnlineArtistAlbumInfo.CreateAsync(
                            albumsElement[i],
                            _api,
                            false
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
