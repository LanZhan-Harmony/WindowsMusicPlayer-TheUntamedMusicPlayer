using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.ViewModels;
using Windows.Media.Playback;
using Windows.System;

namespace UntamedMusicPlayer.Views;

public sealed partial class RootPlayBarView : UserControl
{
    private bool _hasPointerPressed = false;
    private Storyboard? _songInfoTransitionStoryboard;

    public RootPlayBarViewModel ViewModel { get; }

    public RootPlayBarView()
    {
        RootPlayBarViewModel.RootPlayBarView = this;
        InitializeComponent();
        ViewModel = App.GetService<RootPlayBarViewModel>();
        Data.RootPlayBarView = this;

        Data.PlayState.PropertyChanged += OnStateChanged;
        UpdateSongInfoWithoutAnimation();
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SharedPlaybackState.CurrentSong))
        {
            AnimateSongInfoToCurrentSong();
        }
    }

    private void UpdateSongInfoWithoutAnimation()
    {
        var title = Data.PlayState.CurrentSong?.Title ?? "";
        var artistAndAlbum = Data.PlayState.CurrentSong?.ArtistAndAlbumStr ?? "";

        SongTitleTextBlock.Text = title;
        ArtistAndAlbumTextBlock.Text = artistAndAlbum;
        ArtistAndAlbumTextBlock.Visibility = string.IsNullOrWhiteSpace(artistAndAlbum)
            ? Visibility.Collapsed
            : Visibility.Visible;

        CurrentSongInfoPanel.Opacity = 1;
        IncomingSongInfoPanel.Visibility = Visibility.Collapsed;
        IncomingSongInfoPanel.Opacity = 0;
    }

    private void AnimateSongInfoToCurrentSong()
    {
        var title = Data.PlayState.CurrentSong?.Title ?? "";
        var artistAndAlbum = Data.PlayState.CurrentSong?.ArtistAndAlbumStr ?? "";
        if (SongTitleTextBlock.Text == title && ArtistAndAlbumTextBlock.Text == artistAndAlbum)
        {
            return;
        }

        StopSongInfoTransition();

        IncomingSongTitleTextBlock.Text = title;
        IncomingArtistAndAlbumTextBlock.Text = artistAndAlbum;
        IncomingArtistAndAlbumTextBlock.Visibility = string.IsNullOrWhiteSpace(artistAndAlbum)
            ? Visibility.Collapsed
            : Visibility.Visible;

        IncomingSongInfoPanel.Visibility = Visibility.Visible;

        var currentFadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200),
        };
        Storyboard.SetTarget(currentFadeOut, CurrentSongInfoPanel);
        Storyboard.SetTargetProperty(currentFadeOut, nameof(Opacity));

        var currentSlideUp = new DoubleAnimation
        {
            From = 0,
            To = -70,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        Storyboard.SetTarget(currentSlideUp, CurrentSongInfoTransform);
        Storyboard.SetTargetProperty(currentSlideUp, nameof(CompositeTransform.TranslateY));

        var incomingFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(400),
        };
        Storyboard.SetTarget(incomingFadeIn, IncomingSongInfoPanel);
        Storyboard.SetTargetProperty(incomingFadeIn, nameof(Opacity));

        var incomingSlideIn = new DoubleAnimation
        {
            From = 70,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(incomingSlideIn, IncomingSongInfoTransform);
        Storyboard.SetTargetProperty(incomingSlideIn, nameof(CompositeTransform.TranslateY));

        _songInfoTransitionStoryboard = new Storyboard();
        _songInfoTransitionStoryboard.Children.Add(currentFadeOut);
        _songInfoTransitionStoryboard.Children.Add(currentSlideUp);
        _songInfoTransitionStoryboard.Children.Add(incomingFadeIn);
        _songInfoTransitionStoryboard.Children.Add(incomingSlideIn);
        _songInfoTransitionStoryboard.Completed += SongInfoTransitionStoryboard_Completed;
        _songInfoTransitionStoryboard.Begin();
    }

    private void SongInfoTransitionStoryboard_Completed(object? sender, object e)
    {
        ArtistAndAlbumTextBlock.Visibility = IncomingArtistAndAlbumTextBlock.Visibility;
        IncomingSongInfoPanel.Visibility = Visibility.Collapsed;
        StopSongInfoTransition();
    }

    private void StopSongInfoTransition()
    {
        SongTitleTextBlock.Text = IncomingSongTitleTextBlock.Text;
        ArtistAndAlbumTextBlock.Text = IncomingArtistAndAlbumTextBlock.Text;
        if (_songInfoTransitionStoryboard is null)
        {
            return;
        }
        _songInfoTransitionStoryboard.Completed -= SongInfoTransitionStoryboard_Completed;
        _songInfoTransitionStoryboard.Stop();
        _songInfoTransitionStoryboard = null;
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
        (sender as ListView)!.SelectedIndex = Data.PlayState.Speed switch
        {
            0.25 => 0,
            0.5 => 1,
            1 => 2,
            1.5 => 3,
            2 => 4,
            _ => 2,
        };

    private void SpeedListView_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        Data.PlayState.Speed = (sender as ListView)!.SelectedIndex switch
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
        var dialog = new PropertiesDialog(Data.PlayState.CurrentSong!) { XamlRoot = XamlRoot };
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
        Data.MusicPlayer.LyricUpdateByPercentage(((Slider)sender).Value, true);
    }

    public void PointerMovedLyricUpdate(object sender, PointerRoutedEventArgs _)
    {
        if (_hasPointerPressed)
        {
            Data.MusicPlayer.LyricUpdateByPercentage(((Slider)sender).Value, false);
        }
    }

    public void PointerReleasedPositionUpdate(object sender, PointerRoutedEventArgs _)
    {
        _hasPointerPressed = false;
        Data.MusicPlayer.SetPositionByPercentage(((Slider)sender).Value);
    }

    public void KeyDownLyricUpdate(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
        {
            Data.MusicPlayer.LyricUpdateByPercentage(((Slider)sender).Value, true);
        }
    }

    public void KeyUpPositionUpdate(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
        {
            Data.MusicPlayer.SetPositionByPercentage(((Slider)sender).Value);
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
            foreach (var playlist in Data.PlaylistLibrary.Playlists)
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
            ViewModel.AddToPlaylistButton_Click(playlist);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
        {
            ViewModel.AddToPlaylistButton_Click(dialog.CreatedPlaylist);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        var currentSong = Data.PlayState.CurrentSong;
        var dialog = new PropertiesDialog(currentSong!) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }

    private void RootPlayBarView_Unloaded(object sender, RoutedEventArgs e)
    {
        Data.PlayState.PropertyChanged -= OnStateChanged;
        StopSongInfoTransition();
    }
}
