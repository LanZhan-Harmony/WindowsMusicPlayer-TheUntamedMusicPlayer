using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.Storage;
using ZLogger;

namespace UntamedMusicPlayer.Controls;

public sealed partial class EditPlaylistInfoDialog
    : ContentDialog,
        INotifyPropertyChanged,
        IRecipient<ThemeChangeMessage>
{
    private readonly ILogger _logger = LoggingService.CreateLogger<EditPlaylistInfoDialog>();
    private readonly PlaylistInfo _playlist;
    private readonly string _originalName;
    private string _name;
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

    private bool _isCoverEdited;

    private bool _isCoverAdded = false;

    private readonly List<string> _coverPaths;

    private WriteableBitmap? Cover
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Cover));
        }
    }

    private bool IsDeleteCoverButtonEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsDeleteCoverButtonEnabled));
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

    public EditPlaylistInfoDialog(PlaylistInfo info)
    {
        StrongReferenceMessenger.Default.Register(this);
        _playlist = info;
        _originalName = info.Name;
        _name = info.Name;
        foreach (var song in info.SongList)
        {
            Songs.Add(new DisplaySongInfo(song.Song));
        }
        SongCount = Songs.Count;
        var originalCover = CoverManager.GetPlaylistCoverBitmap(info);
        if (originalCover is not null)
        {
            Cover = new WriteableBitmap(originalCover.PixelWidth, originalCover.PixelHeight); // 注意要创建副本
            originalCover.PixelBuffer.CopyTo(Cover.PixelBuffer);
            Cover.Invalidate();
        }
        _isCoverEdited = info.IsCoverEdited;
        _coverPaths = [.. info.CoverPaths]; // 注意要创建副本
        IsDeleteCoverButtonEnabled = Cover is not null;
        IsSaveCoverButtonEnabled = Cover is not null && _isCoverEdited;
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    public void Receive(ThemeChangeMessage message)
    {
        RequestedTheme = message.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
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
            _isCoverEdited = true;
            _isCoverAdded = true;
            _coverPaths.Clear();
            _coverPaths.Add(targetFile.Path);
            await GetCover();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"更改{_playlist.Name}播放列表封面失败");
        }
        finally
        {
            IsDeleteCoverButtonEnabled = Cover is not null;
            IsSaveCoverButtonEnabled = Cover is not null && _isCoverEdited;
            IsChangingCoverProgressRingActive = false;
            (sender as Button)!.IsEnabled = true;
        }
    }

    private void DeleteCoverButton_Click(object sender, RoutedEventArgs e)
    {
        _isCoverEdited = true;
        if (_isCoverAdded && _coverPaths.Count > 0)
        {
            if (File.Exists(_coverPaths[0]))
            {
                try
                {
                    File.Delete(_coverPaths[0]);
                }
                catch { }
            }
            _isCoverAdded = false;
        }
        _coverPaths.Clear();
        Cover = null;
        IsDeleteCoverButtonEnabled = false;
        IsSaveCoverButtonEnabled = false;
    }

    private async void SaveCoverButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        try
        {
            if (_coverPaths.Count == 0 || !File.Exists(_coverPaths[0]))
            {
                return;
            }
            var extension = Path.GetExtension(_coverPaths[0]);
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
                var imageBytes = await File.ReadAllBytesAsync(_coverPaths[0]);
                await File.WriteAllBytesAsync(file.Path, imageBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"保存{_playlist.Name}播放列表封面失败");
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
            var shouldSetCoverPath = (Cover is null) && !_isCoverEdited;
            string? coverPath = null;
            foreach (var file in files)
            {
                var (_, path, songs) = await M3u8Helper.GetNameAndSongsFromM3u8(file.Path);
                foreach (var song in songs)
                {
                    Songs.Add(new DisplaySongInfo(song));
                }
                if (shouldSetCoverPath && coverPath is null && path is not null)
                {
                    coverPath = path;
                }
            }
            if (shouldSetCoverPath && coverPath is not null)
            {
                _isCoverEdited = true;
                _isCoverAdded = true;
                _coverPaths.Clear();
                _coverPaths.Add(coverPath);
                await GetCover();
            }
        }
        catch { }
        finally
        {
            SongCount = Songs.Count;
            IsDeleteCoverButtonEnabled = Cover is not null;
            IsSaveCoverButtonEnabled = Cover is not null && _isCoverEdited;
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
            await ImportPlaylistDialog.LoadMusicAsync(songList, storageFolder);
            foreach (var song in songList)
            {
                Songs.Add(new DisplaySongInfo(song));
            }
        }
        catch { }
        finally
        {
            SongCount = Songs.Count;
            IsImportingProgressRingActive = false;
        }
    }

    private async Task GetCover()
    {
        await Task.Run(async () =>
        {
            try
            {
                var imagePath = _coverPaths[0];
                if (!File.Exists(imagePath))
                {
                    return;
                }
                const int canvasSize = 256;
                var imageBytes = await File.ReadAllBytesAsync(imagePath);
                var resizedImageBytes = await CoverManager.ResizeImageToFitRegionAsync(
                    imageBytes,
                    canvasSize,
                    canvasSize
                );
                if (resizedImageBytes is null)
                {
                    return;
                }
                DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    async () =>
                    {
                        try
                        {
                            Cover ??= new(256, 256);
                            using var pixelStream = Cover.PixelBuffer.AsStream();
                            await pixelStream.WriteAsync(resizedImageBytes);
                            Cover.Invalidate();
                        }
                        catch { }
                    }
                );
            }
            catch { }
        });
    }

    private async void SaveButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_originalName != _name)
        {
            var uniqueName = Data.PlaylistLibrary.GetUniquePlaylistName(_name);
            _playlist.Name = uniqueName;
            StrongReferenceMessenger.Default.Send(
                new PlaylistRenameMessage(_originalName, uniqueName)
            );
        }
        if (_isCoverEdited)
        {
            _playlist.ClearCover(); // 会自动设置 IsCoverEdited 为 true
            _playlist.CoverPaths = _coverPaths;
            CoverManager.ForcePlaylistCoverRefresh(_playlist);
        }
        _playlist.SongList.Clear();
        await _playlist.AddRange([.. Songs.Select(s => s.Song)]);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(true));
        StrongReferenceMessenger.Default.Send(new PlaylistChangeMessage(_playlist));
        _ = FileManager.SavePlaylistDataAsync(Data.PlaylistLibrary.Playlists);
    }

    private new void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_isCoverAdded && _coverPaths.Count > 0)
        {
            if (File.Exists(_coverPaths[0]))
            {
                try
                {
                    File.Delete(_coverPaths[0]);
                }
                catch { }
            }
        }
        StrongReferenceMessenger.Default.Unregister<ThemeChangeMessage>(this);
    }
}
