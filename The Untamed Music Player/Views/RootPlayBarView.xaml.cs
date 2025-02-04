using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class RootPlayBarView : Page
{
    public RootPlayBarViewModel ViewModel
    {
        get;
    }

    public RootPlayBarView()
    {
        RootPlayBarViewModel.RootPlayBarView = this;
        InitializeComponent();
        ViewModel = App.GetService<RootPlayBarViewModel>();
        Data.RootPlayBarView = this;
        DataContext = ViewModel;
    }

    public string GetCurrent(TimeSpan current)
    {
        if (current.Hours > 0)
        {
            return $"{current:hh\\:mm\\:ss}";
        }
        else
        {
            return $"0:{current:mm\\:ss}";
        }
    }

    public string GetRemaining(TimeSpan current, TimeSpan total)
    {
        var remaining = total - current;
        if (remaining.Hours > 0)
        {
            return $"{remaining:hh\\:mm\\:ss}";
        }
        else
        {
            return $"0:{remaining:mm\\:ss}";
        }
    }

    public string GetPlayPauseIcon(byte playstate)
    {
        return playstate switch
        {
            0 => "\uF5B0",
            1 => "\uF8AE",
            2 => "\uF5B0",
            _ => "\uF5B0",
        };
    }

    public string GetPlayPauseTooltip(byte playstate)
    {
        return playstate switch
        {
            0 => "PlayBar_Play".GetLocalized(),
            1 => "PlayBar_Pause".GetLocalized(),
            2 => "PlayBar_Play".GetLocalized(),
            _ => "PlayBar_Play".GetLocalized(),
        };
    }

    public Visibility GetSliderVisibility(byte playstate)
    {
        return playstate switch
        {
            0 => Visibility.Visible,
            1 => Visibility.Visible,
            2 => Visibility.Collapsed,
            _ => Visibility.Visible,
        };
    }

    public Visibility GetProgressVisibility(byte playstate)
    {
        return playstate switch
        {
            0 => Visibility.Collapsed,
            1 => Visibility.Collapsed,
            2 => Visibility.Visible,
            _ => Visibility.Collapsed,
        };
    }

    public Visibility GetArtistAndAlbumStrVisibility(DetailedMusicInfo detailedmusicinfo)
    {
        return detailedmusicinfo.ArtistAndAlbumStr == "" ? Visibility.Collapsed : Visibility.Visible;
    }

    public Visibility GetNotDetailedVisibility(bool isdetail)
    {
        return isdetail ? Visibility.Collapsed : Visibility.Visible;
    }

    public string GetShuffleModeToolTip(bool shufflemode)
    {
        return shufflemode ? "PlayBar_ShuffleOn".GetLocalized() : "PlayBar_ShuffleOff".GetLocalized();
    }

    public FluentIcons.Common.Symbol GetShuffleModeIcon(bool shufflemode)
    {
        return shufflemode ? FluentIcons.Common.Symbol.ArrowShuffle : FluentIcons.Common.Symbol.ArrowShuffleOff;
    }

    public string GetRepeatModeIcon(byte repeatmode)
    {
        return repeatmode switch
        {
            0 => "\uF5E7",
            1 => "\uE8EE",
            2 => "\uE8ED",
            _ => "\uF5E7",
        };
    }

    public string GetRepeatModeToolTip(byte repeatmode)
    {
        return repeatmode switch
        {
            0 => "PlayBar_RepeatOff".GetLocalized(),
            1 => "PlayBar_RepeatAll".GetLocalized(),
            2 => "PlayBar_RepeatOne".GetLocalized(),
            _ => "PlayBar_RepeatOff".GetLocalized(),
        };
    }

    public string GetVolumeIcon(double volume, bool ismute)
    {
        if (ismute)
        {
            return "\uE74F";
        }
        return volume switch
        {
            >= 67 => "\uE995",
            >= 34 => "\uE994",
            >= 1 => "\uE993",
            _ => "\uE74F",
        };
    }

    public string GetMoreShuffleModeText(bool shufflemode)
    {
        return shufflemode ? "PlayBar_More_ShuffleOn".GetLocalized() : "PlayBar_More_ShuffleOff".GetLocalized();
    }

    public string GetMoreRepeatModeText(byte repeatmode)
    {
        return repeatmode switch
        {
            0 => "PlayBar_More_RepeatOff".GetLocalized(),
            1 => "PlayBar_More_RepeatAll".GetLocalized(),
            2 => "PlayBar_More_RepeatOne".GetLocalized(),
            _ => "PlayBar_More_RepeatOff".GetLocalized(),
        };
    }

    public string GetFullScreenIcon(bool isFullscreen)
    {
        return isFullscreen ? "\uE73F" : "\uE740";
    }

    public Slider GetProgressSlider()
    {
        return ProgressSlider;
    }

    private void SpeedListView_Loaded(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SpeedListView_Loaded(sender, e);
    }

    private void SpeedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Data.MusicPlayer.SpeedListView_SelectionChanged(sender, e);
    }

    private async void PlayBarProperty_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PropertiesDialog() { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }
}
