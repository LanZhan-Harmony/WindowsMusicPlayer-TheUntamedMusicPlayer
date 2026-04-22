using System.Collections.ObjectModel;
using MemoryPack;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Services;
using Windows.Storage;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Models;

[MemoryPackable]
public sealed partial class PlaylistInfo
{
    private static readonly ILogger _logger = LoggingService.CreateLogger<PlaylistInfo>();
    public string Name { get; set; } = null!;
    public string TotalSongNumStr { get; set; } = null!;
    public long ModifiedDate { get; set; }
    public ObservableCollection<IndexedPlaylistSong> SongList { get; set; } = [];
    public bool IsCoverEdited { get; set; } = false;
    public List<string> CoverPaths { get; set; } = new(4);

    [MemoryPackConstructor]
    public PlaylistInfo() { }

    public PlaylistInfo(string name, string? coverPath = null)
    {
        Name = name;
        TotalSongNumStr = GetTotalSongNumStr(0);
        IsCoverEdited = coverPath is not null;
        if (IsCoverEdited)
        {
            CoverPaths.Add(coverPath!);
        }
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 仅在创建播放列表时使用
    /// </summary>
    /// <param name="songs"></param>
    /// <returns></returns>
    public async Task AddSongs(List<IBriefSongInfoBase> songs)
    {
        foreach (var song in songs)
        {
            var coverPathIndex = IsCoverEdited ? -1 : await TryAddCoverPath(song);
            var indexedSong = new IndexedPlaylistSong(SongList.Count, song, coverPathIndex);
            SongList.Add(indexedSong);
        }
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    public PlaylistInfo(string name, PlaylistInfo info)
    {
        Name = name;
        TotalSongNumStr = info.TotalSongNumStr;
        ModifiedDate = info.ModifiedDate;
        SongList = info.SongList;
        IsCoverEdited = info.IsCoverEdited;
        CoverPaths = info.CoverPaths;
    }

    /// <summary>
    /// 重新索引所有歌曲，确保Index对应真实位置
    /// </summary>
    public void ReindexSongs()
    {
        for (var i = 0; i < SongList.Count; i++)
        {
            SongList[i].Index = i;
        }
    }

    /// <summary>
    /// 向播放列表中添加一首歌曲
    /// </summary>
    public async Task Add(IBriefSongInfoBase song)
    {
        var coverPathIndex = IsCoverEdited ? -1 : await TryAddCoverPath(song);
        var indexedSong = new IndexedPlaylistSong(SongList.Count, song, coverPathIndex);
        SongList.Add(indexedSong);
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 向播放列表中添加多首歌曲
    /// </summary>
    public async Task AddRange(IEnumerable<IBriefSongInfoBase> songs)
    {
        foreach (var song in songs)
        {
            var coverPathIndex = IsCoverEdited ? -1 : await TryAddCoverPath(song);
            var indexedSong = new IndexedPlaylistSong(SongList.Count, song, coverPathIndex);
            SongList.Add(indexedSong);
        }
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 从播放列表中删除一首歌曲
    /// </summary>
    public async Task Delete(IndexedPlaylistSong song)
    {
        if (song.Index < 0 || song.Index >= SongList.Count)
        {
            return;
        }

        var needRefillCover = false;

        if (song.CoverPathIndex >= 0 && song.CoverPathIndex < CoverPaths.Count)
        {
            CoverPaths.RemoveAt(song.CoverPathIndex); // 删除对应的封面路径
            needRefillCover = true;

            // 更新其他歌曲的封面路径索引
            for (var i = 0; i < SongList.Count; i++)
            {
                if (SongList[i].CoverPathIndex > song.CoverPathIndex)
                {
                    SongList[i].CoverPathIndex--;
                }
            }
        }
        SongList.RemoveAt(song.Index);
        ReindexSongs(); // 重新索引以确保Index对应真实位置
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        if (needRefillCover)
        {
            if (CoverPaths.Count < 4)
            {
                await RefillCoverPaths();
            }
        }
    }

    /// <summary>
    /// 将一首歌曲上移
    /// </summary>
    public void MoveUp(IndexedPlaylistSong song)
    {
        var currentPosition = song.Index;
        if (currentPosition <= 0 || currentPosition >= SongList.Count)
        {
            return;
        }

        // 交换位置
        (SongList[currentPosition], SongList[currentPosition - 1]) = (
            SongList[currentPosition - 1],
            SongList[currentPosition]
        );

        // 更新索引
        SongList[currentPosition].Index = currentPosition;
        SongList[currentPosition - 1].Index = currentPosition - 1;

        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 将一首歌曲下移
    /// </summary>
    public void MoveDown(IndexedPlaylistSong song)
    {
        var currentPosition = song.Index;
        if (currentPosition < 0 || currentPosition >= SongList.Count - 1)
        {
            return;
        }

        // 交换位置
        (SongList[currentPosition], SongList[currentPosition + 1]) = (
            SongList[currentPosition + 1],
            SongList[currentPosition]
        );

        // 更新索引
        SongList[currentPosition].Index = currentPosition;
        SongList[currentPosition + 1].Index = currentPosition + 1;

        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 获取所有歌曲
    /// </summary>
    public IBriefSongInfoBase[] GetAllSongs()
    {
        return [.. SongList.AsValueEnumerable().Select(indexedSong => indexedSong.Song)];
    }

    /// <summary>
    /// 尝试添加封面路径，返回封面路径索引
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    private async Task<int> TryAddCoverPath(IBriefSongInfoBase song)
    {
        if (CoverPaths.Count >= 4)
        {
            return -1;
        }
        var coverPath = await IBriefSongInfoBase.GetCoverPathAsync(song);
        if (!string.IsNullOrWhiteSpace(coverPath))
        {
            CoverPaths.Add(coverPath);
            return CoverPaths.Count - 1;
        }
        return -1;
    }

    /// <summary>
    /// 重新填充封面路径，从剩余歌曲中寻找新的封面
    /// </summary>
    private async Task RefillCoverPaths()
    {
        foreach (var indexedSong in SongList)
        {
            if (CoverPaths.Count >= 4)
            {
                break;
            }

            // 如果这首歌还没有封面路径索引，尝试添加它的封面
            if (indexedSong.CoverPathIndex == -1)
            {
                var coverPath = await IBriefSongInfoBase.GetCoverPathAsync(indexedSong.Song);
                if (!string.IsNullOrWhiteSpace(coverPath))
                {
                    CoverPaths.Add(coverPath);
                    indexedSong.CoverPathIndex = CoverPaths.Count - 1;
                }
            }
        }
    }

    /// <summary>
    /// 清除封面(注意: 危险! 会删除封面文件)
    /// </summary>
    public void ClearCover()
    {
        if (
            CoverPaths.Count > 0
            && Path.Combine(ApplicationData.Current.LocalFolder.Path, "PlaylistCover")
                == Path.GetDirectoryName(CoverPaths[0])
            && Data.SupportedCoverTypes.Contains(Path.GetExtension(CoverPaths[0]).ToLower())
        )
        {
            try
            {
                File.Delete(CoverPaths[0]);
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"播放列表{Name}删除封面图片失败");
            }
        }
        CoverPaths.Clear();
        IsCoverEdited = true;
        SongList.AsValueEnumerable().Select(i => i.CoverPathIndex = -1);
    }

    public void PrepareForRemoval()
    {
        if (
            IsCoverEdited
            && CoverPaths.Count > 0
            && Path.Combine(ApplicationData.Current.LocalFolder.Path, "PlaylistCover")
                == Path.GetDirectoryName(CoverPaths[0])
            && Data.SupportedCoverTypes.Contains(Path.GetExtension(CoverPaths[0]).ToLower())
        )
        {
            try
            {
                File.Delete(CoverPaths[0]);
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"播放列表{Name}删除封面图片失败");
            }
        }
    }

    private static string GetTotalSongNumStr(int totalSongNum)
    {
        return totalSongNum == 1
            ? $"{totalSongNum} {"PlaylistInfo_Item".GetLocalized()}"
            : $"{totalSongNum} {"PlaylistInfo_Items".GetLocalized()}";
    }
}

[MemoryPackable]
public sealed partial class IndexedPlaylistSong
{
    /// <summary>
    /// 在播放列表中的位置索引 (0-based)
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 歌曲信息
    /// </summary>
    public IBriefSongInfoBase Song { get; set; } = null!;

    /// <summary>
    /// 封面路径索引，用于追踪该歌曲在CoverPaths中的位置 (-1表示没有封面)
    /// </summary>
    public int CoverPathIndex { get; set; } = -1;

    [MemoryPackConstructor]
    public IndexedPlaylistSong() { }

    public IndexedPlaylistSong(int index, IBriefSongInfoBase song, int coverPathIndex = -1)
    {
        Index = index;
        Song = song;
        CoverPathIndex = coverPathIndex;
    }
}
