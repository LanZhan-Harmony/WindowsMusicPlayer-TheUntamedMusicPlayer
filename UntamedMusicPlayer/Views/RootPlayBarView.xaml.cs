using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.Playback;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.ViewModels;
using Windows.Media.Playback;
using Windows.System;

namespace UntamedMusicPlayer.Views;

public sealed partial class RootPlayBarView : UserControl
{
    private bool _hasPointerPressed = false;
    private LyricPage? _lyricPage;
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();

    public RootPlayBarViewModel ViewModel { get; }
    public MusicPlayer MusicPlayer { get; } = App.GetService<MusicPlayer>();
    public SharedPlaybackState PlayState => MusicPlayer.State;
    public PlayQueueManager PlayQueueManager => MusicPlayer.QueueManager;

    public RootPlayBarView()
    {
        InitializeComponent();
        ViewModel = App.GetService<RootPlayBarViewModel>();
        ViewModel.DetailModeUpdateRequested += OnDetailModeUpdateRequested;
    }

    public Visibility ToVisibility(bool isVisible) =>
        isVisible ? Visibility.Visible : Visibility.Collapsed;

    private void OnDetailModeUpdateRequested()
    {
        var frame = _navigationService.GetShellFrame();
        if (frame is null)
        {
            return;
        }

        RunCompositionFadeTransition(
            frame,
            () =>
            {
                if (ViewModel.IsDetail)
                {
                    _lyricPage = new LyricPage();
                    frame.Content = _lyricPage;
                }
                else
                {
                    var mainPage = _navigationService.GetShellPage();
                    if (mainPage is null)
                    {
                        return;
                    }

                    frame.Content = mainPage;
                    CurrentSongHighlightExtensions.ReactivateHighlightForPage(mainPage);
                    _lyricPage?.Dispose();
                    _lyricPage = null;
                }
            }
        );
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

    public string GetCurrent(TimeSpan current) =>
        current.Hours > 0 ? $"{current:hh\\:mm\\:ss}" : $"0:{current:mm\\:ss}";

    public string GetRemaining(TimeSpan current, TimeSpan total)
    {
        var remaining = total - current;
        return remaining.Hours > 0 ? $"{remaining:hh\\:mm\\:ss}" : $"0:{remaining:mm\\:ss}";
    }

    public double GetPositionPercentage(TimeSpan current, TimeSpan total) =>
        total.TotalMilliseconds == 0
            ? 0
            : current.TotalMilliseconds / total.TotalMilliseconds * 100;

    public string GetPlayPauseIcon(MediaPlaybackState playstate) =>
        playstate switch
        {
            MediaPlaybackState.Playing => "\uE62E",
            _ => "\uF5B0",
        };

    public string GetPlayPauseTooltip(MediaPlaybackState playstate) =>
        playstate switch
        {
            MediaPlaybackState.Playing => "PlayBar_Pause".GetLocalized(),
            _ => "PlayBar_Play".GetLocalized(),
        };

    public Visibility GetSliderVisibility(MediaPlaybackState playstate) =>
        playstate switch
        {
            MediaPlaybackState.Buffering => Visibility.Collapsed,
            _ => Visibility.Visible,
        };

    public Visibility GetProgressVisibility(MediaPlaybackState playstate) =>
        playstate switch
        {
            MediaPlaybackState.Buffering => Visibility.Visible,
            _ => Visibility.Collapsed,
        };

    public Visibility GetArtistAndAlbumStrVisibility(IDetailedSongInfoBase? detailedLocalSongInfo)
    {
        if (detailedLocalSongInfo is null)
        {
            return Visibility.Collapsed;
        }
        return detailedLocalSongInfo.ArtistAndAlbumStr == ""
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public Visibility GetNotDetailedVisibility(bool isdetail) =>
        isdetail ? Visibility.Collapsed : Visibility.Visible;

    public Thickness GetSongTitleMargin(string artistAndAlbumStr) =>
        string.IsNullOrWhiteSpace(artistAndAlbumStr)
            ? new Thickness(11, 0, 4, -8)
            : new Thickness(11, 0, 4, 4);

    public string GetShuffleModeToolTip(ShuffleState shufflemode) =>
        shufflemode == ShuffleState.Shuffled
            ? "PlayBar_ShuffleOn".GetLocalized()
            : "PlayBar_ShuffleOff".GetLocalized();

    public string GetShuffleModeIcon(ShuffleState shufflemode) =>
        shufflemode == ShuffleState.Shuffled ? "\uE8B1" : "\uE30D";

    public string GetRepeatModeIcon(RepeatState repeatmode) =>
        repeatmode switch
        {
            RepeatState.RepeatAll => "\uE8EE",
            RepeatState.RepeatOne => "\uE8ED",
            _ => "\uF5E7",
        };

    public string GetRepeatModeToolTip(RepeatState repeatmode) =>
        repeatmode switch
        {
            RepeatState.RepeatAll => "PlayBar_RepeatAll".GetLocalized(),
            RepeatState.RepeatOne => "PlayBar_RepeatOne".GetLocalized(),
            _ => "PlayBar_RepeatOff".GetLocalized(),
        };

    public string GetVolumeIcon(double volume, bool ismute) =>
        ismute
            ? "\uE74F"
            : volume switch
            {
                >= 67 => "\uE995",
                >= 34 => "\uE994",
                >= 1 => "\uE993",
                _ => "\uE74F",
            };

    public string GetMoreShuffleModeText(ShuffleState shufflemode) =>
        shufflemode == ShuffleState.Shuffled
            ? "PlayBar_More_ShuffleOn".GetLocalized()
            : "PlayBar_More_ShuffleOff".GetLocalized();

    public string GetMoreRepeatModeText(RepeatState repeatmode) =>
        repeatmode switch
        {
            RepeatState.RepeatAll => "PlayBar_More_RepeatAll".GetLocalized(),
            RepeatState.RepeatOne => "PlayBar_More_RepeatOne".GetLocalized(),
            _ => "PlayBar_More_RepeatOff".GetLocalized(),
        };

    public string GetFullScreenIcon(bool isFullscreen) => isFullscreen ? "\uE73F" : "\uE740";

    private void SpeedListView_Loaded(object sender, RoutedEventArgs e) =>
        (sender as ListView)!.SelectedIndex = PlayState.Speed switch
        {
            0.25 => 0,
            0.5 => 1,
            1 => 2,
            1.5 => 3,
            2 => 4,
            _ => 2,
        };

    private void SpeedListView_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        PlayState.Speed = (sender as ListView)!.SelectedIndex switch
        {
            0 => 0.25,
            1 => 0.5,
            2 => 1,
            3 => 1.5,
            4 => 2,
            _ => 1,
        };

    private async void PlayBarProperty_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PropertiesDialog(PlayState.CurrentSong!) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }

    private void ProgressSlider_Loaded(object sender, RoutedEventArgs e)
    {
        var slider = (sender as Slider)!;
        slider.AddHandler(
            PointerPressedEvent,
            new PointerEventHandler(PointerPressedLyricUpdate),
            true
        );
        slider.AddHandler(
            PointerMovedEvent,
            new PointerEventHandler(PointerMovedLyricUpdate),
            true
        );
        slider.AddHandler(
            PointerReleasedEvent,
            new PointerEventHandler(PointerReleasedPositionUpdate),
            true
        );
        slider.AddHandler(KeyDownEvent, new KeyEventHandler(KeyDownLyricUpdate), true);
        slider.AddHandler(KeyUpEvent, new KeyEventHandler(KeyUpPositionUpdate), true);
    }

    public void PointerPressedLyricUpdate(object sender, PointerRoutedEventArgs _)
    {
        _hasPointerPressed = true;
        MusicPlayer.LyricUpdateByPercentage(((Slider)sender).Value, true);
    }

    public void PointerMovedLyricUpdate(object sender, PointerRoutedEventArgs _)
    {
        if (_hasPointerPressed)
        {
            MusicPlayer.LyricUpdateByPercentage(((Slider)sender).Value, false);
        }
    }

    public void PointerReleasedPositionUpdate(object sender, PointerRoutedEventArgs _)
    {
        _hasPointerPressed = false;
        MusicPlayer.SetPositionByPercentage(((Slider)sender).Value);
    }

    public void KeyDownLyricUpdate(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
        {
            MusicPlayer.LyricUpdateByPercentage(((Slider)sender).Value, true);
        }
    }

    public void KeyUpPositionUpdate(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
        {
            MusicPlayer.SetPositionByPercentage(((Slider)sender).Value);
        }
    }

    private async void EqualizerButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EqualizerDialog { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }

    private void CoverBtnClickToDetail(object sender, RoutedEventArgs e) =>
        ViewModel.DetailModeUpdate();

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem menuItem)
        {
            while (menuItem.Items.Count > 3)
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = playlist,
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: PlaylistInfo playlist })
        {
            ViewModel.AddToPlaylistButtonCommand.Execute(playlist);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
        {
            ViewModel.AddToPlaylistButtonCommand.Execute(dialog.CreatedPlaylist);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        var currentSong = PlayState.CurrentSong;
        var dialog = new PropertiesDialog(currentSong!) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }
}
