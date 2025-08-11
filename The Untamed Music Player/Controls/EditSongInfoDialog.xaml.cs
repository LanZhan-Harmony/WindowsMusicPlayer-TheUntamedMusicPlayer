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
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditSongInfoDialog : ContentDialog, INotifyPropertyChanged
{
    private static readonly string[] _supportedEditTypes = [".mp3", ".flac"];
    private readonly DetailedLocalSongInfo _song;
    private string _title;
    private string _contributingArtists;
    private string _album;
    private string _albumArtist;
    private string _track;
    private string _genre;
    private string _year;
    private string _lyric;
    private bool _hasCoverChanged = false;
    private bool _hasCoverDeleted = false;
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

    private bool IsSaveCoverButtonEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsSaveCoverButtonEnabled));
        }
    } = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EditSongInfoDialog(BriefLocalSongInfo info)
    {
        var detailedInfo = new DetailedLocalSongInfo(info);
        _song = detailedInfo;
        _title = detailedInfo.Title;
        _contributingArtists = detailedInfo.ArtistsStr;
        _album = detailedInfo.Album;
        _albumArtist = detailedInfo.AlbumArtistsStr;
        _track = detailedInfo.TrackStr;
        _genre = detailedInfo.GenreStr;
        _year = detailedInfo.YearStr;
        _lyric = detailedInfo.Lyric;
        Cover = detailedInfo.Cover;
        IsSaveCoverButtonEnabled = Cover is not null;
        RequestedTheme = Data.MainViewModel!.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
        LyricEditor.Document.SetText(TextSetOptions.None, _lyric);
    }

    private async void SaveButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Data.IsMusicProcessing = true;
        try
        {
            if (!_supportedEditTypes.Contains(_song.ItemType))
            {
                throw new UnauthorizedAccessException();
            }
            var file = await StorageFile.GetFileFromPathAsync(_song.Path);
            var musicProperties = await file.Properties.GetMusicPropertiesAsync();
            musicProperties.Title = _title;
            musicProperties.Artist = string.IsNullOrWhiteSpace(_contributingArtists)
                ? null
                : _contributingArtists;
            musicProperties.Composers.Clear();
            foreach (
                var artist in _contributingArtists
                    .Split(
                        BriefLocalSongInfo.Delimiters,
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Distinct()
            )
            {
                musicProperties.Composers.Add(artist);
            }
            musicProperties.Album = string.IsNullOrWhiteSpace(_album) ? null : _album;
            musicProperties.AlbumArtist = string.IsNullOrWhiteSpace(_albumArtist)
                ? null
                : _albumArtist;
            if (uint.TryParse(_track, out var track))
            {
                musicProperties.TrackNumber = track;
            }
            else if (_song.TrackStr != "")
            {
                musicProperties.TrackNumber = 0;
            }
            if (uint.TryParse(_year, out var year))
            {
                musicProperties.Year = year;
            }
            else if (_song.Year != 0)
            {
                musicProperties.Year = 0;
            }
            musicProperties.Genre.Clear();
            if (!string.IsNullOrWhiteSpace(_genre))
            {
                foreach (
                    var genre in _genre
                        .Split(
                            BriefLocalSongInfo.Delimiters,
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        )
                        .Distinct()
                )
                {
                    musicProperties.Genre.Add(genre);
                }
            }
            await musicProperties.SavePropertiesAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"使用MusicProperties保存歌曲信息失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }

        try
        {
            using var musicFile = TagLib.File.Create(_song.Path);
            LyricEditor.Document.GetText(TextGetOptions.None, out var updatedLyrics);
            _lyric = updatedLyrics.TrimEnd().Replace('\r', '\n');
            musicFile.Tag.Lyrics = string.IsNullOrWhiteSpace(_lyric) ? null : _lyric;
            musicFile.Tag.Pictures =
                _hasCoverDeleted ? []
                : _hasCoverChanged && System.IO.File.Exists(_coverFilePath)
                    ? [new Picture(_coverFilePath)]
                : musicFile.Tag.Pictures;
            musicFile.Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"使用Taglib保存歌曲信息失败: {ex.Message}");
        }
        Data.IsMusicProcessing = false;
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
                _hasCoverDeleted = false;
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
            IsSaveCoverButtonEnabled = Cover is not null;
        }
    }

    private void DeleteCoverButton_Click(object sender, RoutedEventArgs e)
    {
        if (Cover is not null)
        {
            Cover = null;
            _hasCoverDeleted = true;
            _hasCoverChanged = false;
            IsSaveCoverButtonEnabled = false;
        }
    }

    private async void SaveCoverButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        try
        {
            var openPicker = new FolderPicker();
            var window = App.MainWindow;
            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("*");
            var folder = await openPicker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            using var musicFile = TagLib.File.Create(_song.Path);
            var picture = musicFile.Tag.Pictures[0];
            var bytes = picture.Data.Data;
            var extension = picture.MimeType.Split('/')[1];
            var fileName = $"{_song.Title}.{extension}";
            var uniqueFilePath = GetUniqueFilePath(Path.Combine(folder.Path, fileName));
            await System.IO.File.WriteAllBytesAsync(uniqueFilePath, bytes);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存封面失败: {ex.Message}");
        }
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    private static string GetUniqueFilePath(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var count = 1; ; count++)
        {
            var uniquePath = Path.Combine(directory, $"{fileNameWithoutExt}({count}){extension}");
            if (!System.IO.File.Exists(uniquePath))
            {
                return uniquePath;
            }
        }
    }
}
