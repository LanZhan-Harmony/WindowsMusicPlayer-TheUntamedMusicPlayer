using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditAlbumInfoDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly LocalAlbumInfo _album;
    private string _name;
    private string _albumArtist;
    private string _genre;
    private string _year;
    private BitmapImage? Cover
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Cover));
        }
    }

    private bool IsSaveCoverButtonEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsSaveCoverButtonEnabled));
        }
    } = false;

    public ObservableCollection<BriefLocalSongInfo> Songs { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EditAlbumInfoDialog(LocalAlbumInfo info)
    {
        _album = info;
        Songs = [.. Data.MusicLibrary.GetSongsByAlbum(info)];
        _name = info.Name;
        _albumArtist = info.ArtistsStr;
        _genre = info.GenreStr;
        _year = $"{info.Year}";
        Cover = info.Cover;
        IsSaveCoverButtonEnabled = Cover is not null;
        RequestedTheme = Data.MainViewModel!.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    private void SaveButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) { }

    private void SaveCoverButton_Click(object sender, RoutedEventArgs e) { }
}
