using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;

/// <summary>
/// Composes Cloud Music responses into application models.
/// Models themselves do not know how to call the provider or load artwork.
/// </summary>
public static class CloudMusicModelFactory
{
    private static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<CloudMusicApiService>();
    private static readonly string _unknownAlbum = "SongInfo_UnknownAlbum".GetLocalized();
    private static readonly string _unknownArtist = "SongInfo_UnknownArtist".GetLocalized();

    public static Task<BriefCloudOnlineAlbumInfo> CreateBriefAlbumAsync(CloudSearchAlbumDto dto) =>
        Task.FromResult(BriefCloudOnlineAlbumInfo.FromDto(dto));

    public static Task<BriefCloudOnlineArtistInfo> CreateBriefArtistAsync(
        CloudSearchArtistDto dto
    ) => Task.FromResult(BriefCloudOnlineArtistInfo.FromDto(dto));

    public static Task<BriefCloudOnlinePlaylistInfo> CreateBriefPlaylistAsync(
        CloudSearchPlaylistDto dto
    ) => Task.FromResult(BriefCloudOnlinePlaylistInfo.FromDto(dto));

    public static async Task<IBriefOnlineAlbumInfo> CreateAlbumFromSongAsync(
        IBriefOnlineSongInfo info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);

        var songInfo = Require<BriefCloudOnlineSongInfo>(info);
        var (_, result) = await api.GetSongDetailsAsync([songInfo.ID]);
        var albumInfo = BriefCloudOnlineAlbumInfo.FromSong(
            songInfo,
            result?.Songs?.FirstOrDefault()?.Album
        );
        return albumInfo;
    }

    public static IBriefOnlineAlbumInfo CreateAlbumFromArtistAlbum(IOnlineArtistAlbumInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return BriefCloudOnlineAlbumInfo.FromArtistAlbum(info);
    }

    public static async Task<IDetailedOnlineAlbumInfo> CreateDetailedAlbumAsync(
        IBriefOnlineAlbumInfo info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);

        var briefInfo = Require<BriefCloudOnlineAlbumInfo>(info);
        var detailedInfo = DetailedCloudOnlineAlbumInfo.FromBrief(briefInfo);
        try
        {
            var (_, result) = await api.GetAlbumAsync(briefInfo.ID);
            var album = result?.Album;
            if (album is not null)
            {
                detailedInfo.Introduction = album.Description;
                detailedInfo.Year = GetYear(album.PublishTime);
                detailedInfo.SetArtist(GetAlbumArtistsStr(album.Artists));
                if (string.IsNullOrWhiteSpace(detailedInfo.CoverPath))
                {
                    detailedInfo.CoverPath = album.PicUrl;
                }
            }

            foreach (
                var song in await CreateAvailableSongsAsync(result?.Songs, api, detailedInfo.Year)
            )
            {
                detailedInfo.AddSong(song);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"读取网易云专辑详细信息失败: {briefInfo.ID}");
        }

        detailedInfo.CompleteDescription();
        return detailedInfo;
    }

    public static async Task<IBriefOnlineArtistInfo> CreateArtistFromSongAsync(
        IBriefOnlineSongInfo info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        var songInfo = Require<BriefCloudOnlineSongInfo>(info);
        return await CreateArtistByIdAsync(songInfo.ArtistID, api);
    }

    public static async Task<IBriefOnlineArtistInfo> CreateArtistFromAlbumAsync(
        IBriefOnlineAlbumInfo info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        var albumInfo = Require<BriefCloudOnlineAlbumInfo>(info);
        return await CreateArtistByIdAsync(albumInfo.ArtistID, api);
    }

    public static async Task<IBriefOnlineArtistInfo> CreateArtistByIdAsync(
        long artistId,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(api);

        var (_, result) = await api.GetArtistAlbumsAsync(artistId, 0, 0);
        var info = BriefCloudOnlineArtistInfo.FromDto(artistId, result?.Artist);
        return info;
    }

    public static Task<List<IBriefSongInfoBase>> GetSongsByArtistAsync(
        IBriefOnlineArtistInfo info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);
        return CloudArtistDetailSearchHelper.GetSongsByArtistAsync(
            Require<BriefCloudOnlineArtistInfo>(info),
            api
        );
    }

    public static async Task<IDetailedOnlineArtistInfo> CreateDetailedArtistAsync(
        IBriefOnlineArtistInfo info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);
        return await CloudArtistDetailSearchHelper.SearchArtistDetailAsync(
            Require<BriefCloudOnlineArtistInfo>(info),
            api
        );
    }

    public static Task LoadMoreArtistAsync(IDetailedOnlineArtistInfo info, CloudMusicApiService api)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);
        return CloudArtistDetailSearchHelper.SearchMoreArtistDetailAsync(
            Require<DetailedCloudOnlineArtistInfo>(info),
            api
        );
    }

    public static async Task<IDetailedOnlinePlaylistInfo> CreateDetailedPlaylistAsync(
        IBriefOnlinePlaylistInfo info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);

        var briefInfo = Require<BriefCloudOnlinePlaylistInfo>(info);
        var detailedInfo = DetailedCloudOnlinePlaylistInfo.FromBrief(briefInfo);
        try
        {
            var (_, result) = await api.GetPlaylistDetailAsync(briefInfo.ID);
            var playlist = result?.Playlist;
            detailedInfo.Introduction = playlist?.Description;
            var trackIds = playlist?.TrackIds?.Select(track => track.Id).ToArray() ?? [];
            if (trackIds.Length > 0)
            {
                detailedInfo.SongList =
                [
                    .. (
                        await CloudSongSearchHelper.SearchSongsByIDsAsync(trackIds, api)
                    ).Cast<IBriefOnlineSongInfo>(),
                ];
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"读取网易云歌单详细信息失败: {briefInfo.ID}");
        }

        return detailedInfo;
    }

    public static async Task<string?> GetCoverPathAsync(
        IBriefSongInfoBase info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);

        return info switch
        {
            BriefLocalSongInfo localInfo => localInfo.HasCover ? localInfo.Path : null,
            BriefUnknownSongInfo => null,
            BriefCloudOnlineSongInfo cloudInfo => (
                await CreateDetailedCloudSongAsync(cloudInfo, api)
            ).CoverPath,
            _ => throw new NotSupportedException(
                $"Unsupported song model: {info.GetType().FullName}"
            ),
        };
    }

    public static async Task<IDetailedSongInfoBase> CreateDetailedSongAsync(
        IBriefSongInfoBase info,
        CloudMusicApiService api
    )
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(api);

        return info switch
        {
            BriefLocalSongInfo localInfo => new DetailedLocalSongInfo(localInfo),
            BriefUnknownSongInfo unknownInfo => new DetailedUnknownSongInfo(unknownInfo),
            BriefCloudOnlineSongInfo cloudInfo => await CreateDetailedCloudSongAsync(
                cloudInfo,
                api
            ),
            _ => throw new NotSupportedException(
                $"Unsupported song model: {info.GetType().FullName}"
            ),
        };
    }

    /// <summary>
    /// Builds one artist album after fetching its tracks and availability.
    /// </summary>
    public static async Task<CloudOnlineArtistAlbumInfo> CreateArtistAlbumAsync(
        CloudArtistAlbumDto dto,
        CloudMusicApiService api,
        bool isDetailed
    )
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(api);

        var info = CloudOnlineArtistAlbumInfo.FromDto(dto, isDetailed);
        try
        {
            var (_, result) = await api.GetAlbumAsync(dto.Id);
            foreach (
                var song in await CreateAvailableSongsAsync(
                    result?.Songs,
                    api,
                    GetYear(dto.PublishTime)
                )
            )
            {
                info.AddSong(song);
            }

            info.MarkAvailable();
        }
        catch (Exception ex)
        {
            info.SetUnavailable();
            _logger.ZLogInformation(ex, $"读取网易云艺术家专辑失败: {dto.Id}");
        }

        return info;
    }

    private static async Task<DetailedCloudOnlineSongInfo> CreateDetailedCloudSongAsync(
        BriefCloudOnlineSongInfo info,
        CloudMusicApiService api
    )
    {
        var detailedInfo = DetailedCloudOnlineSongInfo.FromBrief(info);
        var songUrlTask = api.GetSongUrlsAsync([info.ID]);
        var albumTask = api.GetAlbumAsync(info.AlbumID);
        var lyricTask = api.GetLyricAsync(info.ID);
        await Task.WhenAll(songUrlTask, albumTask, lyricTask);

        var (_, songUrlResult) = songUrlTask.Result;
        var (_, albumResult) = albumTask.Result;
        var (_, lyricResult) = lyricTask.Result;
        try
        {
            var songUrl = songUrlResult?.Data?.FirstOrDefault();
            var album = albumResult?.Album;
            var lyric = lyricResult?.Lrc;
            detailedInfo.Path = songUrl?.Url ?? string.Empty;

            if (info.Album != _unknownAlbum)
            {
                detailedInfo.Album = info.Album;
                detailedInfo.CoverPath = album?.PicUrl;
                detailedInfo.AlbumArtistsStr = IDetailedSongInfoBase.GetAlbumArtistsStr(
                    GetArtistNames(album?.Artists)
                );
                detailedInfo.YearStr = IBriefSongInfoBase.GetYearStr(
                    GetYear(album?.PublishTime ?? 0)
                );
            }

            detailedInfo.ArtistsStr =
                info.ArtistsStr == _unknownArtist ? string.Empty : info.ArtistsStr;
            detailedInfo.ArtistAndAlbumStr = IDetailedSongInfoBase.GetArtistAndAlbumStr(
                detailedInfo.Album,
                detailedInfo.ArtistsStr
            );
            detailedInfo.ItemType = $".{songUrl?.Type}";
            detailedInfo.BitRate = $"{songUrl?.BitRate / 1000} kbps";
            detailedInfo.Lyric = lyric?.Lyric ?? string.Empty;
            if (string.IsNullOrWhiteSpace(detailedInfo.Path))
            {
                detailedInfo.IsPlayAvailable = false;
            }
        }
        catch (Exception ex)
        {
            detailedInfo.IsPlayAvailable = false;
            _logger.ZLogInformation(ex, $"读取网易云音乐{info.ID}详细信息时发生错误");
        }

        return detailedInfo;
    }

    private static async Task<List<BriefCloudOnlineSongInfo>> CreateAvailableSongsAsync(
        IReadOnlyList<CloudSongTrackDto>? tracks,
        CloudMusicApiService api,
        ushort year
    )
    {
        if (tracks is null || tracks.Count == 0)
        {
            return [];
        }

        var songIds = tracks.Select(song => song.Id).Distinct().ToArray();
        var (_, result) = await api.GetSongUrlsAsync(songIds);
        var availabilityMap =
            result
                ?.Data?.GroupBy(item => item.Id)
                .ToDictionary(group => group.Key, group => group.Any(item => item.Url is not null))
            ?? [];

        var songs = new List<BriefCloudOnlineSongInfo>(tracks.Count);
        foreach (var track in tracks)
        {
            if (!availabilityMap.GetValueOrDefault(track.Id))
            {
                continue;
            }

            var song = new BriefCloudOnlineSongInfo(track, year);
            if (song.IsPlayAvailable)
            {
                songs.Add(song);
            }
        }

        return songs;
    }

    private static ushort GetYear(long publishTime) =>
        publishTime > 0
            ? (ushort)DateTimeOffset.FromUnixTimeMilliseconds(publishTime).Year
            : (ushort)0;

    private static string GetAlbumArtistsStr(IEnumerable<CloudSearchArtistReferenceDto>? artists)
    {
        var names = GetArtistNames(artists);
        return IAlbumInfoBase.GetArtistsStr(names.Length == 0 ? [_unknownArtist] : names);
    }

    private static string[] GetArtistNames(IEnumerable<CloudSearchArtistReferenceDto>? artists) =>
        artists
            ?.Select(artist => artist.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Cast<string>()
            .ToArray()
        ?? [];

    private static T Require<T>(object value)
        where T : class
    {
        return value as T
            ?? throw new NotSupportedException(
                $"Cloud Music provider cannot handle model: {value.GetType().FullName}"
            );
    }
}
