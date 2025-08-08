using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using WinUIEx.Messaging;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PlayQueuePage : Page
{
    public PlayQueueViewModel ViewModel { get; }

    public PlayQueuePage()
    {
        ViewModel = App.GetService<PlayQueueViewModel>();
        InitializeComponent();

        Data.MusicPlayer.PropertyChanged += MusicPlayer_PropertyChanged;
    }

    private void PlayQueuePage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdatePlayQueueSource();
        var currentSong = Data.MusicPlayer.CurrentBriefSong;
        if (currentSong is null)
        {
            return;
        }
        var listViewSource = PlayqueueListView.ItemsSource;
        if (listViewSource is IEnumerable<IBriefSongInfoBase> songs)
        {
            var targetSong = currentSong switch
            {
                BriefLocalSongInfo localSong => songs.FirstOrDefault(song =>
                    song.Path == localSong.Path
                ),
                IBriefOnlineSongInfo onlineSong => songs
                    .OfType<IBriefOnlineSongInfo>()
                    .FirstOrDefault(song => song.ID == onlineSong.ID),
                _ => null,
            };
            if (targetSong is not null)
            {
                PlayqueueListView.ScrollIntoView(targetSong, ScrollIntoViewAlignment.Leading);
            }
        }
    }

    private void MusicPlayer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName == nameof(Data.MusicPlayer.ShuffleMode)
            || e.PropertyName == nameof(Data.MusicPlayer.PlayQueue)
            || e.PropertyName == nameof(Data.MusicPlayer.ShuffledPlayQueue)
        )
        {
            UpdatePlayQueueSource();
        }
    }

    private void UpdatePlayQueueSource()
    {
        PlayqueueListView.ItemsSource = Data.MusicPlayer.ShuffleMode
            ? Data.MusicPlayer.ShuffledPlayQueue
            : Data.MusicPlayer.PlayQueue;
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        (grid?.FindName("ItemCheckBox") as CheckBox)?.Visibility = Visibility.Visible;
        (grid?.FindName("PlayButton") as Button)?.Visibility = Visibility.Visible;
        (grid?.FindName("MusicFontIcon") as FontIcon)?.Visibility = Visibility.Collapsed;
        (grid?.FindName("PlayingFontIcon") as FontIcon)?.Visibility = Visibility.Collapsed;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        (grid?.FindName("ItemCheckBox") as CheckBox)?.Visibility = Visibility.Collapsed;
        (grid?.FindName("PlayButton") as Button)?.Visibility = Visibility.Collapsed;
        (grid?.FindName("MusicFontIcon") as FontIcon)?.Visibility = Visibility.Visible;
        if (
            grid?.FindName("PlayingFontIcon") is FontIcon playingFontIcon
            && grid.DataContext is IBriefSongInfoBase songInfo
        )
        {
            var currentSong = Data.MusicPlayer.CurrentSong;
            var isCurrentlyPlaying = currentSong switch
            {
                null => false,
                _ when currentSong.IsOnline && songInfo is IBriefOnlineSongInfo onlineSong => (
                    (IDetailedOnlineSongInfo)currentSong
                ).ID == onlineSong.ID
                    && songInfo.PlayQueueIndex == Data.MusicPlayer.PlayQueueIndex,
                _ when !currentSong.IsOnline && songInfo is BriefLocalSongInfo localSong => (
                    (BriefLocalSongInfo)currentSong
                ).Path == localSong.Path
                    && songInfo.PlayQueueIndex == Data.MusicPlayer.PlayQueueIndex,
                _ => false,
            };
            playingFontIcon.Visibility = isCurrentlyPlaying
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.RemoveButton_Click(info);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.MoveUpButton_Click(info);
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.MoveDownButton_Click(info);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            var song = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.ShowAlbumButton_Click(info);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.ShowArtistButton_Click(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
