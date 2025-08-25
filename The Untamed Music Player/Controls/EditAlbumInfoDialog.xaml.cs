using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using ZLinq;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditAlbumInfoDialog : ContentDialog, INotifyPropertyChanged
{
    private static readonly string[] _supportedEditTypes = [".mp3", ".flac"];
    private readonly ILogger _logger = LoggingService.CreateLogger<EditAlbumInfoDialog>();
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

    private readonly List<TempSongInfo> _tempSongs;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EditAlbumInfoDialog(LocalAlbumInfo info)
    {
        _album = info;
        var songs = Data.MusicLibrary.GetSongsByAlbum(info);
        _tempSongs = [.. songs.AsValueEnumerable().Select(song => new TempSongInfo(song))];
        _name = info.Name;
        _albumArtist = info.ArtistsStr;
        _genre = info.GenreStr;
        _year = $"{info.Year}";
        Cover = info.Cover;
        IsSaveCoverButtonEnabled = Cover is not null;
        RequestedTheme = Data.MainViewModel!.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!uint.TryParse((sender as TextBox)!.Text, out var _))
        {
            (sender as TextBox)!.Text = "";
        }
    }

    private async void SaveButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Data.IsMusicProcessing = true;

        // 保存专辑信息到专辑中的所有歌曲
        foreach (var tempSong in _tempSongs)
        {
            try
            {
                var originalSong = tempSong.OriginalSong;
                var itemType = Path.GetExtension(originalSong.Path).ToLower();

                if (!_supportedEditTypes.Contains(itemType))
                {
                    continue;
                }

                var file = await StorageFile.GetFileFromPathAsync(originalSong.Path);
                var musicProperties = await file.Properties.GetMusicPropertiesAsync();

                musicProperties.Album = string.IsNullOrWhiteSpace(_name) ? null : _name;
                musicProperties.AlbumArtist = string.IsNullOrWhiteSpace(_albumArtist)
                    ? null
                    : _albumArtist;

                if (uint.TryParse(_year, out var year))
                {
                    musicProperties.Year = year;
                }
                else if (_album.Year != 0)
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
                                StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries
                            )
                            .AsValueEnumerable()
                            .Distinct()
                    )
                    {
                        musicProperties.Genre.Add(genre);
                    }
                }

                // 更新单一歌曲的信息
                musicProperties.Title = string.IsNullOrWhiteSpace(tempSong.Title)
                    ? null
                    : tempSong.Title;

                if (uint.TryParse(tempSong.TrackStr, out var track))
                {
                    musicProperties.TrackNumber = track;
                }
                else if (originalSong.TrackStr != "")
                {
                    musicProperties.TrackNumber = 0;
                }

                var artistsStr = string.IsNullOrWhiteSpace(tempSong.ArtistsStr)
                    ? null
                    : tempSong.ArtistsStr;
                musicProperties.Artist = artistsStr;
                musicProperties.Composers.Clear();
                if (!string.IsNullOrWhiteSpace(artistsStr))
                {
                    foreach (
                        var artist in artistsStr
                            .Split(
                                BriefLocalSongInfo.Delimiters,
                                StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries
                            )
                            .AsValueEnumerable()
                            .Distinct()
                    )
                    {
                        musicProperties.Composers.Add(artist);
                    }
                }

                await musicProperties.SavePropertiesAsync();
            }
            catch (COMException)
            {
                _logger.EditingSongInfoIO(tempSong.Title);
            }
            catch (Exception ex)
            {
                _logger.EditingSongInfoOther(tempSong.Title, ex);
            }
        }

        Data.IsMusicProcessing = false;
    }

    private async void SaveCoverButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        try
        {
            if (string.IsNullOrEmpty(_album.CoverPath) || !File.Exists(_album.CoverPath))
            {
                return;
            }

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

            using var musicFile = TagLib.File.Create(_album.CoverPath);
            var picture = musicFile.Tag.Pictures[0];
            var bytes = picture.Data.Data;
            var extension = picture.MimeType.Split('/')[1];
            var fileName = $"{_album.Name}.{extension}";
            var uniqueFilePath = GetUniqueFilePath(Path.Combine(folder.Path, fileName));
            await File.WriteAllBytesAsync(uniqueFilePath, bytes);
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
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var count = 1; ; count++)
        {
            var uniquePath = Path.Combine(directory, $"{fileNameWithoutExt}({count}){extension}");
            if (!File.Exists(uniquePath))
            {
                return uniquePath;
            }
        }
    }
}

public class TempSongInfo(BriefLocalSongInfo originalSong)
{
    public string TrackStr { get; set; } = originalSong.TrackStr;
    public string Title { get; set; } = originalSong.Title;
    public string ArtistsStr { get; set; } =
        originalSong.ArtistsStr == "SongInfo_UnknownArtist".GetLocalized()
            ? ""
            : originalSong.ArtistsStr;
    public BriefLocalSongInfo OriginalSong { get; } = originalSong;
}
