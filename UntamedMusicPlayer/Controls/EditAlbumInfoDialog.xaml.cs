using System.ComponentModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.Storage;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Controls;

public sealed partial class EditAlbumInfoDialog
    : ContentDialog,
        INotifyPropertyChanged,
        IRecipient<ThemeChangeMessage>
{
    private static readonly string[] _supportedEditTypes = [".mp3", ".flac"];
    private readonly ILogger _logger = LoggingService.CreateLogger<EditAlbumInfoDialog>();
    private readonly LocalAlbumInfo _album;
    private string _name;
    private string _albumArtist;
    private string _genre;
    private string _year;
    private readonly bool _isSaveCoverButtonEnabled;
    private BitmapImage? Cover
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Cover));
        }
    }

    private readonly List<TempSongInfo> _tempSongs;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EditAlbumInfoDialog(LocalAlbumInfo info)
    {
        StrongReferenceMessenger.Default.Register(this);
        _album = info;
        var songs = Data.MusicLibrary.GetSongsByAlbum(info);
        _tempSongs = [.. songs.AsValueEnumerable().Select(song => new TempSongInfo(song))];
        _name = info.Name;
        _albumArtist = info.ArtistsStr;
        _genre = info.GenreStr;
        _year = $"{info.Year}";
        Cover = CoverManager.GetAlbumCoverBitmap(info);
        _isSaveCoverButtonEnabled = Cover is not null;
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    public void Receive(ThemeChangeMessage message)
    {
        RequestedTheme = message.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
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
            var picture = CoverManager.GetSongCoverPicture(_album.CoverPath);
            if (picture?.Data.Data is not { Length: > 0 } data)
            {
                return;
            }
            var extension = $".{picture.MimeType.Split('/')[1]}";
            var fileName = _album.Name;
            var savePicker = new FileSavePicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = fileName,
                FileTypeChoices =
                {
                    { "EditSongInfoDialog_CoverImage".GetLocalized(), [extension] },
                },
            };
            var file = await savePicker.PickSaveFileAsync();
            if (file is not null)
            {
                await File.WriteAllBytesAsync(file.Path, data);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"保存{_album.Name}专辑封面失败");
        }
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    private new void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        StrongReferenceMessenger.Default.Unregister<ThemeChangeMessage>(this);
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
