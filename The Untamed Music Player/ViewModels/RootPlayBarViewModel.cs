using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;
public partial class RootPlayBarViewModel : ObservableRecipient
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

        RootPlayBarView?.GetProgressSlider().AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(Data.MusicPlayer.ProgressLock), true);
        RootPlayBarView?.GetProgressSlider().AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(Data.MusicPlayer.SliderUpdate), true);
        RootPlayBarView?.GetProgressSlider().AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(Data.MusicPlayer.ProgressUpdate), true);
    }

    public void CoverBtnClickToDetail(object sender, RoutedEventArgs e)
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
        else
        {
            var mainPage = Data.ShellPage;
            var frame = Data.MainWindow!.GetShellFrame();

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

    public void FullScreenButton_Click(object sender, RoutedEventArgs e)
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
