using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditPlaylistInfoDialog : ContentDialog, INotifyPropertyChanged
{
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
        _playlist = info;
        _originalName = info.Name;
        _name = info.Name;
        foreach (var song in info.SongList)
        {
            Songs.Add(new DisplaySongInfo(song.Song));
        }
        SongCount = Songs.Count;
        Cover = info.Cover;
        _isCoverEdited = info.IsCoverEdited;
        _coverPaths = [.. info.CoverPaths]; // 注意要创建副本
        IsSaveCoverButtonEnabled = Cover is not null;
        RequestedTheme = Data.MainViewModel!.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
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
            var openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var type in Data.SupportedCoverTypes)
            {
                openPicker.FileTypeFilter.Add(type);
            }
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(openPicker, hWnd);
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
            var extension = Path.GetExtension(file.Name);
            var newFileName = $"{Guid.CreateVersion7()}{extension}";
            var targetFile = await playlistCoverFolder.CreateFileAsync(
                newFileName,
                CreationCollisionOption.ReplaceExisting
            );
            await file.CopyAndReplaceAsync(targetFile);
            _isCoverEdited = true;
            _coverPaths.Clear();
            _coverPaths.Add(targetFile.Path);
            await GetCover();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"更改封面失败: {ex.Message}");
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
        _isCoverEdited = true;
        _coverPaths.Clear();
        Cover = null;
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
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = fileName,
                FileTypeChoices =
                {
                    new("EditSongInfoDialog_CoverImage".GetLocalized(), [extension]),
                },
            };
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(savePicker, hWnd);
            var file = await savePicker.PickSaveFileAsync();
            if (file is not null)
            {
                var imageBytes = await File.ReadAllBytesAsync(_coverPaths[0]);
                await FileIO.WriteBytesAsync(file, imageBytes);
            }
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

    private async void ImportFromM3u8FilesButton_Click(object sender, RoutedEventArgs e)
    {
        IsImportingProgressRingActive = true;
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                FileTypeFilter = { ".m3u8", ".m3u" },
            };
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hWnd);
            var files = await picker.PickMultipleFilesAsync();
            if (files.Count == 0)
            {
                return;
            }
            var shouldSetCoverPath = (Cover is null) && !_isCoverEdited;
            string? coverPath = null;
            foreach (var file in files)
            {
                var (_, path, songs) = await M3u8Helper.GetNameAndSongsFromM3u8(file);
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
                _coverPaths.Clear();
                _coverPaths.Add(coverPath);
                await GetCover();
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
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                FileTypeFilter = { "*" },
            };
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hWnd);
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }
            var songList = new ConcurrentBag<IBriefSongInfoBase>();
            await ImportPlaylistDialog.LoadMusicAsync(songList, folder);
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
                var resizedImageBytes = await PlaylistInfo.ResizeImageToFitRegionAsync(
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
            _playlist.ClearCover();
            _playlist.CoverPaths = _coverPaths;
            _playlist.Cover = Cover;
        }
        _playlist.SongList.Clear();
        await _playlist.AddRange([.. Songs.Select(s => s.Song)]);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(true));
        StrongReferenceMessenger.Default.Send(new PlaylistChangeMessage(_playlist));
        _ = FileManager.SavePlaylistDataAsync(Data.PlaylistLibrary.Playlists);
    }

    private new void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_isCoverEdited && _coverPaths.Count > 0)
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
    }
}
