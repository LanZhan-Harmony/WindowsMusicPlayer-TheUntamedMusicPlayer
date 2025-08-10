using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using TagLib;
using The_Untamed_Music_Player.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditSongInfoDialog : ContentDialog, INotifyPropertyChanged
{
    private string _title;
    private string _contributingArtists;
    private string _album;
    private string _albumArtist;
    private string _track;
    private string _genre;
    private string _year;
    private string _lyric;
    private bool _hasCoverChanged = false;
    private string? _coverFilePath;
    private BitmapImage? Cover
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Cover));
        }
    }
    private bool IsChangingCoverProgressRingActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsChangingCoverProgressRingActive));
        }
    } = false;

    private readonly DetailedLocalSongInfo _song;

    public event PropertyChangedEventHandler? PropertyChanged;

    public EditSongInfoDialog(BriefLocalSongInfo info)
    {
        var detailedInfo = new DetailedLocalSongInfo(info);
        _song = detailedInfo;
        _title = detailedInfo.Title;
        _contributingArtists = detailedInfo.ArtistsStr;
        _album = detailedInfo.Album;
        _albumArtist = detailedInfo.AlbumArtistsStr;
        _track = detailedInfo.Track;
        _genre = detailedInfo.GenreStr;
        _year = detailedInfo.YearStr;
        _lyric = detailedInfo.Lyric;
        Cover = detailedInfo.Cover;
        RequestedTheme = Data.MainViewModel!.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
        LyricEditor.Document.SetText(TextSetOptions.None, _lyric);
    }

    private async void SaveButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            Data.IsMusicProcessing = true;
            LyricEditor.Document.GetText(TextGetOptions.None, out var updatedLyrics);
            _lyric = updatedLyrics;
            var file = await StorageFile.GetFileFromPathAsync(_song.Path);
            var musicProperties = await file.Properties.GetMusicPropertiesAsync();
            musicProperties.Title = _title;
            musicProperties.Album = _album;
            musicProperties.Artist = _contributingArtists;
            musicProperties.TrackNumber = uint.TryParse(_track, out var track) ? track : 0;
            musicProperties.AlbumArtist = _albumArtist;
            musicProperties.Year = uint.TryParse(_year, out var yearNumber) ? yearNumber : 0;
            await musicProperties.SavePropertiesAsync();

            using var musicFile = TagLib.File.Create(_song.Path);
            musicFile.Tag.Lyrics = updatedLyrics;
            if (!string.IsNullOrWhiteSpace(_genre))
            {
                musicFile.Tag.Genres =
                [
                    .. _genre
                        .Split(BriefLocalSongInfo.Delimiters, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Distinct(),
                ];
            }
            if (_hasCoverChanged && System.IO.File.Exists(_coverFilePath))
            {
                var picture = new Picture(_coverFilePath);
                musicFile.Tag.Pictures = [picture];
            }

            musicFile.Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存歌曲信息失败: {ex.Message}");
            throw;
        }
        finally
        {
            Data.IsMusicProcessing = false;
        }
    }

    private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = _song.Path;
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\"",
            UseShellExecute = true,
        };
        Process.Start(startInfo);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void ChangeCoverButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        IsChangingCoverProgressRingActive = true;
        try
        {
            var openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            var fileTypes = new[]
            {
                ".png",
                ".jpg",
                ".jpeg",
                ".jpe",
                ".jfif",
                ".bmp",
                ".dip",
                ".gif",
                ".tif",
                ".tiff",
            };
            foreach (var fileType in fileTypes)
            {
                openPicker.FileTypeFilter.Add(fileType);
            }
            var window = App.MainWindow;
            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(openPicker, hWnd);
            var file = await openPicker.PickSingleFileAsync();
            if (file is not null)
            {
                _hasCoverChanged = true;
                _coverFilePath = file.Path;
                Cover = new BitmapImage(new Uri(file.Path));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"更改封面失败: {ex.StackTrace}");
        }
        finally
        {
            IsChangingCoverProgressRingActive = false;
            (sender as Button)!.IsEnabled = true;
        }
    }
}
