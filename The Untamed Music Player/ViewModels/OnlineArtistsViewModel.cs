using System.Threading.Tasks;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.ViewModels;

public class OnlineArtistsViewModel
{
    public async void PlayButton_Click(IBriefOnlineArtistInfo info)
    {
        IDetailedOnlineArtistInfo detailedInfo = null!;
        switch (Data.OnlineMusicLibrary.MusicLibraryIndex)
        {
            case 0:
                detailedInfo = new DetailedCloudOnlineArtistInfo();
                await CloudArtistDetailSearchHelper.SearchArtistDetailAsync(
                    (BriefCloudOnlineArtistInfo)info,
                    (DetailedCloudOnlineArtistInfo)detailedInfo
                );
                break;
        }
        var songList = detailedInfo.AlbumList.SelectMany(album => album.SongList).ToList();
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Artist:{info.Name}",
            songList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public async void PlayNextButton_Click(IBriefOnlineArtistInfo info)
    {
        IDetailedOnlineArtistInfo detailedInfo = null!;
        switch (Data.OnlineMusicLibrary.MusicLibraryIndex)
        {
            case 0:
                detailedInfo = new DetailedCloudOnlineArtistInfo();
                await CloudArtistDetailSearchHelper.SearchArtistDetailAsync(
                    (BriefCloudOnlineArtistInfo)info,
                    (DetailedCloudOnlineArtistInfo)detailedInfo
                );
                break;
        }
        var songList = detailedInfo.AlbumList.SelectMany(album => album.SongList).ToList();
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayList(
                $"OnlineSongs:Artist:{info.Name}",
                songList,
                (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
                0
            );
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }
}
