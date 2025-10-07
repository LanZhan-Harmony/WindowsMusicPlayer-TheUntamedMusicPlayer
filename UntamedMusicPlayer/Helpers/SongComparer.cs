using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Helpers;

public static class SongComparer
{
    /// <summary>
    /// 比较两首歌曲是否相同
    /// </summary>
    /// <param name="currentSong">当前播放的歌曲</param>
    /// <param name="compareSong">要比较的歌曲</param>
    /// <returns>如果是同一首歌曲则返回 true，否则返回 false</returns>
    public static bool CurrentIsSameSong(
        IDetailedSongInfoBase currentSong,
        IBriefSongInfoBase compareSong
    )
    {
        return (currentSong, compareSong) switch
        {
            // 本地歌曲比较：通过路径比较
            (DetailedLocalSongInfo detailedLocalSong, BriefLocalSongInfo localSong) =>
                detailedLocalSong.Path == localSong.Path,

            // 在线未知歌曲比较：通过路径比较
            (DetailedUnknownSongInfo detailedUnknownSong, BriefUnknownSongInfo unknownSong) =>
                detailedUnknownSong.Path == unknownSong.Path,

            // 在线歌曲比较：通过 ID 比较
            (IDetailedOnlineSongInfo detailedOnlineSong, IBriefOnlineSongInfo onlineSong) =>
                detailedOnlineSong.ID == onlineSong.ID,

            // 类型不匹配
            _ => false,
        };
    }

    /// <summary>
    /// 比较两首简要歌曲信息是否相同
    /// </summary>
    /// <param name="song1"></param>
    /// <param name="song2"></param>
    /// <returns></returns>
    public static bool IsSameSong(IBriefSongInfoBase song1, IBriefSongInfoBase song2)
    {
        return (song1, song2) switch
        {
            // 本地歌曲比较：通过路径比较
            (BriefLocalSongInfo localSong1, BriefLocalSongInfo localSong2) => localSong1.Path
                == localSong2.Path,

            // 在线未知歌曲比较：通过路径比较
            (BriefUnknownSongInfo unknownSong1, BriefUnknownSongInfo unknownSong2) =>
                unknownSong1.Path == unknownSong2.Path,

            // 在线歌曲比较：通过 ID 比较
            (IBriefOnlineSongInfo onlineSong1, IBriefOnlineSongInfo onlineSong2) => onlineSong1.ID
                == onlineSong2.ID,

            // 类型不匹配
            _ => false,
        };
    }
}
