using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LyricViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly IWindowService _windowService = App.GetService<IWindowService>();
    private readonly RootPlayBarViewModel _rootPlayBarViewModel =
        App.GetService<RootPlayBarViewModel>();
    private readonly MusicPlayer _musicPlayer;
    private readonly SharedPlaybackState _playState;
    private readonly PlayQueueManager _playQueueManager;
    private readonly LyricManager _lyricManager;

    [ObservableProperty]
    public partial bool IsShowCoverEnabled { get; set; }

    public LyricViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
        _playState = _musicPlayer.State;
        _playQueueManager = _musicPlayer.QueueManager;
        _lyricManager = _musicPlayer.LyricManager;

        IsShowCoverEnabled = _playState.CurrentSong?.Cover is not null;
        _playState.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SharedPlaybackState.CurrentSong))
        {
            IsShowCoverEnabled = _playState.CurrentSong?.Cover is not null;
        }
    }

    public void ListView_ItemClick(LyricSlice lyricSlice)
    {
        _musicPlayer.LyricPositionUpdate(lyricSlice.StartTime);
    }

    [RelayCommand]
    public void PlayButton()
    {
        var currentSong = _playState.CurrentBriefSong;
        _musicPlayer.PlaySongByInfo(currentSong!);
    }

    [RelayCommand]
    public void PlayNextButton()
    {
        var currentSong = _playState.CurrentBriefSong;
        _playQueueManager.AddSongsToNextPlay([currentSong!]);
    }

    [RelayCommand]
    public void AddToPlayQueueButton()
    {
        var currentSong = _playState.CurrentBriefSong;
        _playQueueManager.AddSongsToEnd([currentSong!]);
    }

    [RelayCommand]
    public async Task AddToPlaylistButton(PlaylistInfo playlist)
    {
        var currentSong = _playState.CurrentBriefSong;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, currentSong!);
    }

    [RelayCommand]
    public async Task ShowAlbumButton()
    {
        _rootPlayBarViewModel.DetailModeUpdate();
        var info = _playState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(localAlbumInfo, nameof(LyricPage)),
                    NavigationTransition.Suppress
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
                    NavigationTransition.Suppress
                );
            }
        }
    }

    [RelayCommand]
    public async Task ShowArtistButton()
    {
        _rootPlayBarViewModel.DetailModeUpdate();
        var info = _playState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = App.GetService<MusicLibrary>()
                .GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalArtistDetailPage),
                    new LocalArtistNavigationArgs(localArtistInfo, nameof(LyricPage)),
                    NavigationTransition.Suppress
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
                    NavigationTransition.Suppress
                );
            }
        }
    }

    [RelayCommand]
    public void ShowCoverButton()
    {
        var currentSong = _playState.CurrentSong;
        if (currentSong?.Cover is null)
        {
            return;
        }

        var windowId = Guid.CreateVersion7();
        var window = new ImageViewerWindow(windowId, currentSong);
        _windowService.AddImageViewerWindow(windowId, window);
    }

    [RelayCommand]
    public void AddLyricAdjustButton()
    {
        _lyricManager.AddLyricAdjust();
    }

    [RelayCommand]
    public void SubtractLyricAdjustButton()
    {
        _lyricManager.SubtractLyricAdjust();
    }

    public void Dispose()
    {
        _playState.PropertyChanged -= OnStateChanged;
    }
}
