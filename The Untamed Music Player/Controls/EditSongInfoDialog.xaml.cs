using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using Windows.Storage;
using ZLinq;
using ZLogger;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditSongInfoDialog
    : ContentDialog,
        INotifyPropertyChanged,
        IRecipient<ThemeChangeMessage>
{
    private static readonly string[] _supportedEditTypes = [".mp3", ".flac"];
    private readonly ILogger _logger = LoggingService.CreateLogger<EditSongInfoDialog>();
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
        StrongReferenceMessenger.Default.Register(this);
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
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
        LyricEditor.Document.SetText(TextSetOptions.None, _lyric);
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

        if (_supportedEditTypes.Contains(_song.ItemType))
        {
            try
            {
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
                        .AsValueEnumerable()
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
                await musicProperties.SavePropertiesAsync();
            }
            catch (COMException)
            {
                _logger.EditingSongInfoIO(_title);
                return;
            }
            catch (Exception ex)
            {
                _logger.EditingSongInfoOther(_title, ex);
            }
        }

        try
        {
            using var musicFile = TagLib.File.Create(_song.Path);
            LyricEditor.Document.GetText(TextGetOptions.None, out var updatedLyrics);
            _lyric = updatedLyrics.TrimEnd().Replace('\r', '\n');
            musicFile.Tag.Lyrics = string.IsNullOrWhiteSpace(_lyric) ? null : _lyric;
            musicFile.Tag.Pictures =
                _hasCoverDeleted ? []
                : _hasCoverChanged && File.Exists(_coverFilePath)
                    ? [new TagLib.Picture(_coverFilePath)]
                : musicFile.Tag.Pictures;
            musicFile.Save();
        }
        catch (IOException)
        {
            _logger.EditingSongInfoIO(_title);
        }
        catch (Exception ex)
        {
            _logger.EditingSongInfoOther(_title, ex);
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
            var openPicker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            Array.ForEach(Data.SupportedCoverTypes, openPicker.FileTypeFilter.Add);
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
            _logger.ZLogInformation(ex, $"更改{_song.Title}歌曲封面失败");
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
            using var musicFile = TagLib.File.Create(_song.Path);
            var picture = musicFile.Tag.Pictures[0];
            var bytes = picture.Data.Data;
            var extension = $".{picture.MimeType.Split('/')[1]}";
            var fileName = _song.Title;
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
                await File.WriteAllBytesAsync(file.Path, bytes);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"保存{_song.Title}歌曲封面失败");
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
