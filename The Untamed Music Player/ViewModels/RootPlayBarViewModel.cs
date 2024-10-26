using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;
public partial class RootPlayBarViewModel : INotifyPropertyChanged
{
    public static RootPlayBarView? RootPlayBarView;

    private bool _isDetail = false;
    public bool IsDetail
    {
        get => _isDetail;
        set
        {
            _isDetail = value;
            OnPropertyChanged(nameof(IsDetail));
        }
    }

    private bool _isFullScreen = false;
    public bool IsFullScreen
    {
        get => _isFullScreen;
        set
        {
            _isFullScreen = value;
            OnPropertyChanged(nameof(IsFullScreen));
        }
    }

    private bool _isDesktopLyricWindowStarted = false;
    public bool IsDesktopLyricWindowStarted
    {
        get => _isDesktopLyricWindowStarted;
        set
        {
            _isDesktopLyricWindowStarted = value;
            OnPropertyChanged(nameof(IsDesktopLyricWindowStarted));
        }
    }

    private Visibility _buttonVisibility = Visibility.Collapsed;
    public Visibility ButtonVisibility
    {
        get => _buttonVisibility;
        set
        {
            _buttonVisibility = value;
            OnPropertyChanged(nameof(ButtonVisibility));
        }
    }

    private bool _availability = false;
    public bool Availability
    {
        get => _availability;
        set
        {
            _availability = value;
            OnPropertyChanged(nameof(Availability));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public RootPlayBarViewModel()
    {
        Data.RootPlayBarViewModel = this;

        RootPlayBarView?.GetProgressSlider().AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(Data.MusicPlayer.ProgressLock), true);
        RootPlayBarView?.GetProgressSlider().AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(Data.MusicPlayer.SliderUpdate), true);
        RootPlayBarView?.GetProgressSlider().AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(Data.MusicPlayer.ProgressUpdate), true);
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

    public Visibility GetShuffleModeIcon(bool shufflemode)
    {
        return shufflemode ? Visibility.Collapsed : Visibility.Visible;
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

    public void CoverBtnClickToDetail(object sender, RoutedEventArgs e)
    {
        if (!IsDetail)
        {
            if (Data.MainWindow != null)
            {
                Data.LyricPage = new LyricPage();
                var frame = Data.MainWindow.GetShellFrame();

                // 创建渐变动画
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.1))
                };

                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2))
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
                fadeOutAnimation.Completed += (s, a) =>
                {
                    fadeInStoryboard.Begin();
                    frame.Content = Data.LyricPage;
                };

                fadeOutStoryboard.Begin();

                IsDetail = true;
            }
        }
        else
        {
            if (Data.MainWindow != null)
            {
                var mainPage = Data.ShellPage;
                var frame = Data.MainWindow.GetShellFrame();

                // 创建渐变动画
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.1))
                };

                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromSeconds(0.2))
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
                fadeOutAnimation.Completed += (s, a) =>
                {
                    fadeInStoryboard.Begin();
                    frame.Content = mainPage;
                };

                fadeOutStoryboard.Begin();

                // 强制调用 Dispose 方法
                Data.LyricPage?.Dispose();

                IsDetail = false;
            }
        }
    }

    public void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(Data.MainWindow);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

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

    public void DesktopLyricButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsDesktopLyricWindowStarted)
        {
            Data.DesktopLyricWindow = new DesktopLyricWindow();
            Data.DesktopLyricWindow.Activate();
            IsDesktopLyricWindowStarted = true;
        }
        else
        {
            Data.DesktopLyricWindow?.Close();
            Data.DesktopLyricWindow?.Dispose();
            IsDesktopLyricWindowStarted = false;
        }
    }
}
