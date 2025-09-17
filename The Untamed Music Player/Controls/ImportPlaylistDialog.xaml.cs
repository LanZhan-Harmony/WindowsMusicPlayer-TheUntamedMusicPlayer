using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.Services;
using Windows.Storage;
using ZLinq;
using ZLogger;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class ImportPlaylistDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly ILogger _logger = LoggingService.CreateLogger<ImportPlaylistDialog>();
    private ObservableCollection<DisplaySongInfo> Songs { get; set; } = [];
    private int SongCount
    {
        get;
        set
        {
            field = value;
            SongListViewVisibility = value > 0 ? Visibility.Visible : Visibility.Collapsed;
            OnPropertyChanged(nameof(SongCount));
        }
    }
    private Visibility SongListViewVisibility
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SongListViewVisibility));
        }
    } = Visibility.Collapsed;

    private string? _coverPath;

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

    private bool IsChangingCoverProgressRingActive
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsChangingCoverProgressRingActive));
        }
    } = false;

    private bool IsImportingProgressRingActive
    {
        get;
        set
        {
            field = value;
            ImportFromM3u8FilesButton.IsEnabled = !value;
            ImportFromFolderButton.IsEnabled = !value;
            OnPropertyChanged(nameof(IsImportingProgressRingActive));
        }
    } = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ImportPlaylistDialog()
    {
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    private void PlaylistNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
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
            if (file is null)
            {
                return;
            }
            var localFolder = ApplicationData.Current.LocalFolder;
            var playlistCoverFolder = await localFolder.CreateFolderAsync(
                "PlaylistCover",
                CreationCollisionOption.OpenIfExists
            );
            var extension = Path.GetExtension(file.Path);
            var newFileName = $"{Guid.CreateVersion7()}{extension}";
            var targetFile = await playlistCoverFolder.CreateFileAsync(
                newFileName,
                CreationCollisionOption.ReplaceExisting
            );
            await Task.Run(() => File.Copy(file.Path, targetFile.Path, true));
            Cover = new BitmapImage(new Uri(targetFile.Path));
            _coverPath = targetFile.Path;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"更改导入的播放列表封面失败");
        }
        finally
        {
            IsSaveCoverButtonEnabled = Cover is not null;
            IsChangingCoverProgressRingActive = false;
            (sender as Button)!.IsEnabled = true;
        }
    }

    private void DeleteCoverButton_Click(object sender, RoutedEventArgs e)
    {
        Cover = null;
        _coverPath = null;
        IsSaveCoverButtonEnabled = false;
    }

    private async void SaveCoverButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        try
        {
            if (string.IsNullOrEmpty(_coverPath) || !File.Exists(_coverPath))
            {
                return;
            }
            var extension = Path.GetExtension(_coverPath);
            var fileName = string.IsNullOrWhiteSpace(PlaylistNameTextBox.Text)
                ? "PlaylistInfo_UntitledPlaylist".GetLocalized()
                : PlaylistNameTextBox.Text;
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
                var imageBytes = await File.ReadAllBytesAsync(_coverPath);
                await File.WriteAllBytesAsync(file.Path, imageBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"保存导入的播放列表封面失败");
        }
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    private async void ImportFromM3u8FilesButton_Click(object sender, RoutedEventArgs e)
    {
        IsImportingProgressRingActive = true;
        try
        {
            var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                FileTypeFilter = { ".m3u8", ".m3u" },
            };
            var files = await picker.PickMultipleFilesAsync();
            if (files.Count == 0)
            {
                return;
            }
            var shouldSetPlaylistName =
                string.IsNullOrWhiteSpace(PlaylistNameTextBox.Text)
                || PlaylistNameTextBox.Text == "PlaylistInfo_UntitledPlaylist".GetLocalized();
            var shouldSetCoverPath = Cover is null;
            string? playlistName = null;
            string? coverPath = null;
            foreach (var file in files)
            {
                var (name, cover, songs) = await M3u8Helper.GetNameAndSongsFromM3u8(file.Path);
                foreach (var song in songs)
                {
                    Songs.Add(new DisplaySongInfo(song));
                }
                if (shouldSetPlaylistName && playlistName is null)
                {
                    playlistName = name;
                }
                if (shouldSetCoverPath && coverPath is null && cover is not null)
                {
                    coverPath = cover;
                }
            }
            if (shouldSetPlaylistName && playlistName is not null)
            {
                PlaylistNameTextBox.SelectedText = playlistName;
                IsPrimaryButtonEnabled = true;
            }
            if (shouldSetCoverPath && coverPath is not null)
            {
                Cover = new BitmapImage(new Uri(coverPath));
                _coverPath = coverPath;
            }
        }
        catch { }
        finally
        {
            SongCount = Songs.Count;
            IsSaveCoverButtonEnabled = Cover is not null;
            IsImportingProgressRingActive = false;
        }
    }

    private async void ImportFromFolderButton_Click(object sender, RoutedEventArgs e)
    {
        IsImportingProgressRingActive = true;
        try
        {
            var folderPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
            };
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }
            var songList = new ConcurrentBag<IBriefSongInfoBase>();
            var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder.Path);
            await LoadMusicAsync(songList, storageFolder);
            foreach (var song in songList)
            {
                Songs.Add(new DisplaySongInfo(song));
            }
            var shouldSetPlaylistName =
                string.IsNullOrWhiteSpace(PlaylistNameTextBox.Text)
                || PlaylistNameTextBox.Text == "PlaylistInfo_UntitledPlaylist".GetLocalized();
            if (shouldSetPlaylistName)
            {
                PlaylistNameTextBox.SelectedText = storageFolder.DisplayName;
                IsPrimaryButtonEnabled = true;
            }
        }
        catch { }
        finally
        {
            SongCount = Songs.Count;
            IsImportingProgressRingActive = false;
        }
    }

    public static async Task LoadMusicAsync(
        ConcurrentBag<IBriefSongInfoBase> songList,
        StorageFolder folder
    )
    {
        try
        {
            var entries = await folder.GetItemsAsync();
            var loadMusicTasks = new List<Task>();

            // 先分配扫描子文件夹的任务
            foreach (var subFolder in entries.OfType<StorageFolder>())
            {
                loadMusicTasks.Add(LoadMusicAsync(songList, subFolder));
            }

            // 同时处理当前文件夹的文件
            loadMusicTasks.Add(
                Task.Run(() =>
                {
                    var supportedFiles = entries
                        .OfType<StorageFile>()
                        .Where(file => Data.SupportedAudioTypes.Contains(file.FileType.ToLower()));
                    foreach (var file in supportedFiles)
                    {
                        var briefLocalSongInfo = new BriefLocalSongInfo(file.Path, "");
                        if (!briefLocalSongInfo.IsPlayAvailable)
                        {
                            continue;
                        }
                        songList.Add(briefLocalSongInfo);
                    }
                })
            );

            loadMusicTasks.Add(
                Task.Run(async () =>
                {
                    var m3u8Files = entries
                        .OfType<StorageFile>()
                        .Where(file =>
                            file.FileType.Equals(".m3u8", StringComparison.OrdinalIgnoreCase)
                            || file.FileType.Equals(".m3u", StringComparison.OrdinalIgnoreCase)
                        );
                    foreach (var m3u8File in m3u8Files)
                    {
                        var (_, _, songs) = await M3u8Helper.GetNameAndSongsFromM3u8(m3u8File.Path);
                        foreach (var song in songs)
                        {
                            songList.Add(song);
                        }
                    }
                })
            );
            await Task.WhenAll(loadMusicTasks); // 等待所有子文件夹的扫描任务完成
        }
        catch (Exception ex)
        {
            var logger = LoggingService.CreateLogger(nameof(LoadMusicAsync));
            logger.ZLogInformation(ex, $"扫描歌曲导入播放列表失败");
        }
    }

    private async void ImportButtonClick(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args
    )
    {
        var name = string.IsNullOrEmpty(PlaylistNameTextBox.Text)
            ? "PlaylistInfo_UntitledPlaylist".GetLocalized()
            : PlaylistNameTextBox.Text;
        var playlist = new PlaylistInfo(name, _coverPath);
        await playlist.AddSongs([.. Songs.Select(s => s.Song)]);
        Data.PlaylistLibrary!.NewPlaylists([playlist]);
        StrongReferenceMessenger.Default.Send(
            new LogMessage(
                LogLevel.None,
                "PlaylistInfo_ImportPlaylist".GetLocalizedWithReplace("{num}", "1")
            )
        );
    }

    private new void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (Cover is not null)
        {
            if (File.Exists(_coverPath))
            {
                try
                {
                    File.Delete(_coverPath);
                }
                catch { }
            }
        }
    }
}

public class DisplaySongInfo(IBriefSongInfoBase song)
{
    public string Type { get; set; } =
        song switch
        {
            BriefLocalSongInfo => "DisplaySongInfo_SourceMode0".GetLocalized(),
            BriefUnknownSongInfo => "DisplaySongInfo_SourceMode1".GetLocalized(),
            BriefCloudOnlineSongInfo => "DisplaySongInfo_SourceMode2".GetLocalized(),
            _ => "",
        };
    public IBriefSongInfoBase Song { get; set; } = song;
}
