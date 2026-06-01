using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class RootPlayBarViewModel : ObservableObject
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    public bool IsDesktopLyricWindowStarted { get; set; } = false;

    public static RootPlayBarView? RootPlayBarView { get; set; }

    [ObservableProperty]
    public partial bool IsDetail { get; set; } = false;

    [ObservableProperty]
    public partial bool IsFullScreen { get; set; } = false;

    [ObservableProperty]
    public partial Visibility ButtonVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial bool Availability { get; set; } = false;

    public RootPlayBarViewModel()
    {
        Data.RootPlayBarViewModel = this;
        ButtonVisibility = Data.PlayState.CurrentSong is null
            ? Visibility.Collapsed
            : Visibility.Visible;
        Availability = Data.PlayState is not null;
        App.GetService<MusicPlayer>().BarViewAvailabilityChanged += OnBarViewAvailabilityChanged;
    }

    private void OnBarViewAvailabilityChanged(bool value)
    {
        ButtonVisibility = value ? Visibility.Visible : Visibility.Collapsed;
        Availability = value;
    }

    private static void RunCompositionFadeTransition(UIElement target, Action onFadeOutCompleted)
    {
        var visual = ElementCompositionPreview.GetElementVisual(target);
        var compositor = visual.Compositor;

        var fadeOutAnimation = compositor.CreateScalarKeyFrameAnimation();
        fadeOutAnimation.InsertKeyFrame(1f, 0f);
        fadeOutAnimation.Duration = TimeSpan.FromSeconds(0.1);

        var fadeInAnimation = compositor.CreateScalarKeyFrameAnimation();
        fadeInAnimation.InsertKeyFrame(1f, 1f);
        fadeInAnimation.Duration = TimeSpan.FromSeconds(0.2);

        var fadeOutBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        fadeOutBatch.Completed += (_, _) =>
        {
            onFadeOutCompleted();
            visual.Opacity = 0f;

            var fadeInBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            visual.StartAnimation(nameof(Visual.Opacity), fadeInAnimation);
            fadeInBatch.End();
        };

        visual.StartAnimation(nameof(Visual.Opacity), fadeOutAnimation);
        fadeOutBatch.End();
    }

    public void DetailModeUpdate()
    {
        if (!IsDetail)
        {
            Data.LyricPage = new LyricPage();
            var frame = _navigationService.GetShellFrame();
            if (frame is null)
            {
                return;
            }

            RunCompositionFadeTransition(
                frame,
                () =>
                {
                    frame.Content = Data.LyricPage;
                }
            );

            IsDetail = true;
        }
        else
        {
            var mainPage = _navigationService.GetShellPage();
            var frame = _navigationService.GetShellFrame();
            if (mainPage is null || frame is null)
            {
                return;
            }

            RunCompositionFadeTransition(
                frame,
                () =>
                {
                    frame.Content = mainPage;
                    CurrentSongHighlightExtensions.ReactivateHighlightForPage(mainPage);
                }
            );

            Data.LyricPage?.Dispose(); // 强制调用 Dispose 方法

            IsDetail = false;
        }
    }

    [RelayCommand]

    public void FullScreenButton()

    {
        var appWindow = App.MainWindow!.AppWindow;
        if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
        {
            appWindow.SetPresenter(AppWindowPresenterKind.Default);
            IsFullScreen = false;
        }
        else
        {
            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            IsFullScreen = true;
        }
    }

    [RelayCommand]

    public void DesktopLyricButton()

    {
        if (!IsDesktopLyricWindowStarted)
        {
            Data.DesktopLyricWindow = new DesktopLyricWindow();
            IsDesktopLyricWindowStarted = true;
        }
        else
        {
            Data.DesktopLyricWindow?.Dispose();
            IsDesktopLyricWindowStarted = false;
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
        if (IsDetail)
        {
            DetailModeUpdate();
        }
        var info = Data.PlayState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(localAlbumInfo, nameof(RootPlayBarView)),
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
                    new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(RootPlayBarView)),
                    new SuppressNavigationTransitionInfo()
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
        var info = Data.PlayState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = App.GetService<MusicLibrary>().GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalArtistDetailPage),
                    new LocalArtistNavigationArgs(localArtistInfo, nameof(RootPlayBarView)),
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
                    new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(RootPlayBarView)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }








}

