using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class RootPlayBarViewModel : ObservableObject
{
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
        Data.MusicPlayer.BarViewAvailabilityChanged += OnBarViewAvailabilityChanged;
    }

    private void OnBarViewAvailabilityChanged(bool value)
    {
        ButtonVisibility = value ? Visibility.Visible : Visibility.Collapsed;
        Availability = value;
    }

    public void DetailModeUpdate()
    {
        if (!IsDetail)
        {
            Data.LyricPage = new LyricPage();
            var frame = Data.MainWindow!.GetShellFrame();

            // 创建渐变动画
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
            };

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.2)),
            };

            var fadeOutStoryboard = new Storyboard();
            fadeOutStoryboard.Children.Add(fadeOutAnimation);
            Storyboard.SetTarget(fadeOutAnimation, frame);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");

            var fadeInStoryboard = new Storyboard();
            fadeInStoryboard.Children.Add(fadeInAnimation);
            Storyboard.SetTarget(fadeInAnimation, frame);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");

            // 动画完成后设置新内容
            fadeOutAnimation.Completed += (_, _) =>
            {
                fadeInStoryboard.Begin();
                frame.Content = Data.LyricPage;
            };

            fadeOutStoryboard.Begin();

            IsDetail = true;
        }
        else
        {
            var mainPage = Data.ShellPage;
            var frame = Data.MainWindow!.GetShellFrame();

            // 创建渐变动画
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
            };

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.2)),
            };

            var fadeOutStoryboard = new Storyboard();
            fadeOutStoryboard.Children.Add(fadeOutAnimation);
            Storyboard.SetTarget(fadeOutAnimation, frame);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");

            var fadeInStoryboard = new Storyboard();
            fadeInStoryboard.Children.Add(fadeInAnimation);
            Storyboard.SetTarget(fadeInAnimation, frame);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");

            // 动画完成后设置新内容
            fadeOutAnimation.Completed += (_, _) =>
            {
                fadeInStoryboard.Begin();
                frame.Content = mainPage;

                CurrentSongHighlightExtensions.ReactivateHighlightForPage(mainPage);
            };

            fadeOutStoryboard.Begin();

            Data.LyricPage?.Dispose(); // 强制调用 Dispose 方法

            IsDetail = false;
        }
    }

    public void FullScreenButton_Click(object _1, RoutedEventArgs _2)
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

    public void DesktopLyricButton_Click(object _1, RoutedEventArgs _2)
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

    public void PlayButton_Click(object _1, RoutedEventArgs _2)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.MusicPlayer.PlaySongByInfo(currentSong!);
    }

    public void PlayNextButton_Click(object _1, RoutedEventArgs _2)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.PlayQueueManager.AddSongsToNextPlay([currentSong!]);
    }

    public void AddToPlayQueueButton_Click(object _1, RoutedEventArgs _2)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.PlayQueueManager.AddSongsToEnd([currentSong!]);
    }

    public async void AddToPlaylistButton_Click(PlaylistInfo playlist)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        await Data.PlaylistLibrary.AddToPlaylist(playlist, currentSong!);
    }

    public async void ShowAlbumButton_Click(object _1, RoutedEventArgs _2)
    {
        if (IsDetail)
        {
            DetailModeUpdate();
        }
        var info = Data.PlayState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = Data.MusicLibrary.GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                Data.SelectedLocalAlbum = localAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalAlbumDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineAlbumInfo is not null)
            {
                Data.SelectedOnlineAlbum = onlineAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineAlbumDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    public async void ShowArtistButton_Click(object _1, RoutedEventArgs _2)
    {
        if (IsDetail)
        {
            DetailModeUpdate();
        }
        var info = Data.PlayState.CurrentBriefSong;
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = Data.MusicLibrary.GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                Data.SelectedLocalArtist = localArtistInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalArtistDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineArtistInfo is not null)
            {
                Data.SelectedOnlineArtist = onlineArtistInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineArtistDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }
}
