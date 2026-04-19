using System.ComponentModel;
using System.Diagnostics;
using ATL;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Controls;

public sealed partial class EditSongInfoDialog
    : ContentDialog,
        INotifyPropertyChanged,
        IRecipient<ThemeChangeMessage>
{
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
        if (!Data.SupportedAudioTypesForTagEditing.Contains(_song.ItemType))
        {
            Data.IsMusicProcessing = false;
            return;
        }
        LyricEditor.Document.GetText(TextGetOptions.None, out var updatedLyrics);
        _lyric = updatedLyrics.TrimEnd().Replace('\r', '\n');

        await Task.Run(async () =>
        {
            try
            {
                ATL.Settings.ID3v2_tagSubVersion = 3;
                var musicFile = new Track(_song.Path)
                {
                    Title = _title,
                    Artist = _contributingArtists,
                    Composer = _contributingArtists,
                    Album = _album,
                    AlbumArtist = _albumArtist,
                    TrackNumber = int.TryParse(_track, out var track) ? track : 0,
                    Year = int.TryParse(_year, out var year) ? year : 0,
                    Genre = _genre,
                };
                musicFile.Lyrics.Clear();
                if (!string.IsNullOrWhiteSpace(_lyric))
                {
                    musicFile.Lyrics.Add(new LyricsInfo { UnsynchronizedLyrics = _lyric });
                }

                if (_hasCoverDeleted)
                {
                    musicFile.EmbeddedPictures.Clear();
                }
                else if (_hasCoverChanged)
                {
                    if (File.Exists(_coverFilePath))
                    {
                        musicFile.EmbeddedPictures.Clear();
                        var picture = PictureInfo.fromBinaryData(File.ReadAllBytes(_coverFilePath));
                        musicFile.EmbeddedPictures.Add(picture);
                    }
                }
                if (!await musicFile.SaveAsync())
                {
                    _logger.EditingSongInfoIO(_title);
                }
                else
                {
                    _ = Data.MusicLibrary.LoadLibraryAgainAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.EditingSongInfoOther(_title, ex);
            }
        });

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
            if (musicFile.Tag.Pictures.Length == 0)
            {
                return;
            }
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
