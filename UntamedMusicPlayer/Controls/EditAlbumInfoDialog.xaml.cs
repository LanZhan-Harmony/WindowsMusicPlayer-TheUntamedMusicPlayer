using System.ComponentModel;
using ATL;
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
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Controls;

public sealed partial class EditAlbumInfoDialog
    : ContentDialog,
        INotifyPropertyChanged,
        IRecipient<ThemeChangeMessage>
{
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

        await Task.Run(async () =>
        {
            ATL.Settings.ID3v2_tagSubVersion = 3;
            var hasChanges = false;
            foreach (var tempSong in _tempSongs)
            {
                try
                {
                    var itemType = Path.GetExtension(tempSong.OriginalSong.Path).ToLower();
                    if (!Data.SupportedAudioTypesForTagEditing.Contains(itemType))
                    {
                        continue;
                    }

                    var musicFile = new Track(tempSong.OriginalSong.Path)
                    {
                        Album = _name,
                        AlbumArtist = _albumArtist,
                        Year = int.TryParse(_year, out var year) ? year : 0,
                        Genre = _genre,
                        TrackNumber = int.TryParse(tempSong.TrackStr, out var track) ? track : 0,
                        Title = tempSong.Title,
                        Artist = tempSong.ArtistsStr,
                        Composer = tempSong.ArtistsStr,
                    };
                    if (!await musicFile.SaveAsync())
                    {
                        _logger.EditingSongInfoIO(tempSong.Title);
                    }
                    else
                    {
                        hasChanges = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.EditingSongInfoOther(tempSong.Title, ex);
                }
            }
            if (hasChanges)
            {
                _ = Data.MusicLibrary.LoadLibraryAgainAsync();
            }
        });

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
