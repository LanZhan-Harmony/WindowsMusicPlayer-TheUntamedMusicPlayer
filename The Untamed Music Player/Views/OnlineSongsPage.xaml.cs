using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;
public sealed partial class OnlineSongsPage : Page
{
    private ScrollViewer? _scrollViewer;

    public OnlineSongsViewModel ViewModel
    {
        get; set;
    }

    public OnlineSongsPage()
    {
        ViewModel = App.GetService<OnlineSongsViewModel>();
        InitializeComponent();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Visible;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
    }

    private void OnlineSongsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = SongListView.FindDescendant<ScrollViewer>() ?? throw new Exception("Cannot find ScrollViewer in ListView"); // 检索 ListView 内部使用的 ScrollViewer

        _scrollViewer.ViewChanged += async (s, e) =>
        {
            if (!Data.OnlineMusicLibrary.OnlineMusicInfoList.HasAllLoaded && _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight >= _scrollViewer.ExtentHeight - 50)
            {
                await Data.OnlineMusicLibrary.SearchMore();
                await Task.Delay(3000);
            }
        };
    }
}
