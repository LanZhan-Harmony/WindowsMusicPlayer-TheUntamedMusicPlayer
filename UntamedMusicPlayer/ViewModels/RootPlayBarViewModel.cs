using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class RootPlayBarViewModel : ObservableObject
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly IWindowService _windowService = App.GetService<IWindowService>();
    private readonly MusicPlayer _musicPlayer;

    public event Action? DetailModeUpdateRequested;

    public bool IsDesktopLyricWindowStarted { get; set; } = false;

    [ObservableProperty]
    public partial bool IsDetail { get; set; } = false;

    [ObservableProperty]
    public partial bool IsFullScreen { get; set; } = false;

    [ObservableProperty]
    public partial bool Availability { get; set; } = false;

    public RootPlayBarViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
        Availability = _musicPlayer.State.CurrentSong is not null;
        IsFullScreen = _windowService.IsFullScreen;
        _musicPlayer.BarViewAvailabilityChanged += OnBarViewAvailabilityChanged;
    }

    private void OnBarViewAvailabilityChanged(bool value)
    {
        Availability = value;
    }

    public void DetailModeUpdate()
    {
        IsDetail = !IsDetail;
        DetailModeUpdateRequested?.Invoke();
    }

    [RelayCommand]
    public void FullScreenButton()
    {
        _windowService.ToggleFullScreen();
        IsFullScreen = _windowService.IsFullScreen;
    }

    [RelayCommand]
    public void DesktopLyricButton()
    {
        if (!IsDesktopLyricWindowStarted)
        {
            _windowService.ShowDesktopLyricWindow(() => IsDesktopLyricWindowStarted = false);
            IsDesktopLyricWindowStarted = true;
        }
        else
        {
            _windowService.CloseDesktopLyricWindow();
            IsDesktopLyricWindowStarted = false;
        }
    }

    [RelayCommand]
    public void PlayButton()
    {
        var currentSong = _musicPlayer.State.CurrentBriefSong;
        _musicPlayer.PlaySongByInfo(currentSong!);
    }

    [RelayCommand]
    public void PlayNextButton()
    {
        var currentSong = _musicPlayer.State.CurrentBriefSong;
        _musicPlayer.QueueManager.AddSongsToNextPlay([currentSong!]);
    }

    [RelayCommand]
    public void AddToPlayQueueButton()
    {
        var currentSong = _musicPlayer.State.CurrentBriefSong;
        _musicPlayer.QueueManager.AddSongsToEnd([currentSong!]);
    }

    [RelayCommand]
    public async Task AddToPlaylistButton(PlaylistInfo playlist)
    {
        var currentSong = _musicPlayer.State.CurrentBriefSong;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, currentSong!);
    }

    [RelayCommand]
    public async Task ShowAlbumButton()
    {
        if (IsDetail)
        {
            DetailModeUpdate();
        }
        var info = _musicPlayer.State.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(localAlbumInfo, nameof(RootPlayBarView)),
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
                    new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(RootPlayBarView)),
                    NavigationTransition.Suppress
                );
            }
        }
    }

    [RelayCommand]
    public async Task ShowArtistButton()
    {
        if (IsDetail)
        {
            DetailModeUpdate();
        }
        var info = _musicPlayer.State.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = App.GetService<MusicLibrary>()
                .GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalArtistDetailPage),
                    new LocalArtistNavigationArgs(localArtistInfo, nameof(RootPlayBarView)),
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
                    new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(RootPlayBarView)),
                    NavigationTransition.Suppress
                );
            }
        }
    }
}
