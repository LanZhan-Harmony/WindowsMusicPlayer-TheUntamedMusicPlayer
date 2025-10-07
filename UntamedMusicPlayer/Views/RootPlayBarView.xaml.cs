using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

    public RootPlayBarViewModel ViewModel { get; }

    public RootPlayBarView()
    {
        RootPlayBarViewModel.RootPlayBarView = this;
        InitializeComponent();
        ViewModel = App.GetService<RootPlayBarViewModel>();
        Data.RootPlayBarView = this;
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
}
