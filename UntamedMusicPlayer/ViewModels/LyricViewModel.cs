using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMusicPlayer.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.Views;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LyricViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    [ObservableProperty]
    public partial bool IsShowCoverEnabled { get; set; }

    public LyricViewModel()
    {
        IsShowCoverEnabled = Data.PlayState.CurrentSong?.Cover is not null;
        Data.PlayState.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SharedPlaybackState.CurrentSong))
        {
            IsShowCoverEnabled = Data.PlayState.CurrentSong?.Cover is not null;
        }
    }

    public void ListView_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LyricSlice lyricSlice)
        {
            var time = lyricSlice.StartTime;
            App.GetService<MusicPlayer>().LyricPositionUpdate(time);
        }
    }

    [RelayCommand]

    public void PlayButton()

    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        App.GetService<MusicPlayer>().PlaySongByInfo(currentSong!);
    }

    [RelayCommand]

    public void PlayNextButton()

    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.PlayQueueManager.AddSongsToNextPlay([currentSong!]);
    }

    [RelayCommand]

    public void AddToPlayQueueButton()

    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.PlayQueueManager.AddSongsToEnd([currentSong!]);
    }

    [RelayCommand]

    public async Task AddToPlaylistButton(PlaylistInfo playlist)

    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, currentSong!);
    }

    [RelayCommand]

    public async Task ShowAlbumButton()

    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
        var info = Data.PlayState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(localAlbumInfo, nameof(LyricPage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineAlbumDetailPage),
                    new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(LyricPage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    [RelayCommand]

    public async Task ShowArtistButton()

    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
        var info = Data.PlayState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = App.GetService<MusicLibrary>().GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalArtistDetailPage),
                    new LocalArtistNavigationArgs(localArtistInfo, nameof(LyricPage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineArtistDetailPage),
                    new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(LyricPage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    [RelayCommand]

    public async Task ShowCoverButton()

    {
        Data.ImageViewerWindows ??= [];
        var windowId = Guid.CreateVersion7();
        Data.ImageViewerWindows.Add(
            windowId,
            new ImageViewerWindow(windowId, Data.PlayState.CurrentSong!)
        );
    }

    [RelayCommand]

    public async Task AddLyricAdjustButton()

    {
        Data.LyricManager.AddLyricAdjust();
    }

    [RelayCommand]

    public async Task SubtractLyricAdjustButton()

    {
        Data.LyricManager.SubtractLyricAdjust();
    }









    public void Dispose()
    {
        Data.PlayState.PropertyChanged -= OnStateChanged;
    }

}

