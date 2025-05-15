using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.ViewModels;
using Windows.Storage;

namespace The_Untamed_Music_Player.Models;
public partial class MusicLibrary : ObservableRecipient
{
    /// <summary>
    /// 调度器队列
    /// </summary>
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    /// 信号量, 只允许一个线程访问
    /// </summary>
    private readonly SemaphoreSlim _librarySemaphore = new(1, 1);

    /// <summary>
    /// 是否正在处理文件夹变更事件, 防止同时音乐库
    /// </summary>
    private bool _isHandlingChange = false;

    /// <summary>
    /// 音乐文件夹及其子文件夹(临时)
    /// </summary>
    private readonly ConcurrentDictionary<string, byte> _musicFolders = [];

    /// <summary>
    /// 音乐流派(临时)
    /// </summary>
    private readonly ConcurrentDictionary<string, byte> _musicGenres = [];

    /// <summary>
    /// 是否有音乐
    /// </summary>
    public bool HasMusics => !Songs.IsEmpty;

    /// <summary>
    /// 文件夹监视器
    /// </summary>
    public List<FileSystemWatcher> FolderWatchers { get; set; } = [];

    /// <summary>
    /// 歌曲列表
    /// </summary>
    public ConcurrentBag<BriefMusicInfo> Songs { get; set; } = [];

    /// <summary>
    /// 专辑列表
    /// </summary>
    public ConcurrentDictionary<string, AlbumInfo> Albums { get; set; } = [];

    /// <summary>
    /// 艺术家列表
    /// </summary>
    public ConcurrentDictionary<string, ArtistInfo> Artists { get; set; } = [];

    /// <summary>
    /// 是否显示正在重新扫描进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = false;

    /// <summary>
    /// 文件夹列表
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<StorageFolder> Folders { get; set; } = [];

    /// <summary>
    /// 流派列表
    /// </summary>
    [ObservableProperty]
    public partial List<string> Genres { get; set; } = [];

    public MusicLibrary()
    {
        LoadFoldersAsync();
    }

    public async void LoadFoldersAsync()
    {
        var folderPaths = await ApplicationData.Current.LocalFolder.ReadAsync<List<string>>("MusicFolders");//ApplicationData.Current.LocalFolder：获取应用程序的本地存储文件夹。ReadAsync<List<string>>("MusicFolders")：调用 SettingsStorageExtensions 类中的扩展方法 ReadAsync，从名为 "MusicFolders" 的文件中读取数据，并将其反序列化为 List<string> 类型。
        if (folderPaths is not null)
        {
            foreach (var path in folderPaths)
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                Folders.Add(folder);
            }
            OnPropertyChanged(nameof(SettingsViewModel.EmptyFolderMessageVisibility));
        }
    }

    public async Task LoadLibraryAsync()
    {
        await _librarySemaphore.WaitAsync(); // 等待信号量, 只允许一个线程访问此函数
        try
        {
            Songs.Clear();
            Artists.Clear();
            Albums.Clear();
            var (needRescan, libraryData) = await FileManager.LoadLibraryDataAsync(Folders);
            if (!needRescan)
            {
                Songs = libraryData.Songs;
                Albums = libraryData.Albums;
                Artists = libraryData.Artists;
                Genres = libraryData.Genres;
                await Task.Run(AddFolderWatcher);
            }
            else
            {
                var loadMusicTasks = new List<Task>();
                if (Folders.Any())
                {
                    foreach (var folder in Folders)
                    {
                        _musicFolders.TryAdd(folder.Path, 0);
                        loadMusicTasks.Add(LoadMusicAsync(folder, folder.DisplayName));
                    }
                }
                await Task.WhenAll(loadMusicTasks);
                await EnqueueAndWaitAsync(() =>
                {
                    OnPropertyChanged(nameof(HasMusics));
                });
                Genres = [.. _musicGenres.Keys
                    .Concat(["MusicInfo_AllGenres".GetLocalized()])
                    .OrderBy(x => x, new GenreComparer())];
                _musicGenres.Clear();
                _musicFolders.Clear();
                var data = new MusicLibraryData(Songs, Albums, Artists, Genres);
                await Task.Run(AddFolderWatcher);
                FileManager.SaveLibraryDataAsync(Folders, data);
            }
            Data.HasMusicLibraryLoaded = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
        finally
        {
            _librarySemaphore.Release();
            GC.Collect();
        }
    }

    public async Task LoadLibraryAgainAsync()
    {
        await _librarySemaphore.WaitAsync();
        try
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsProgressRingActive = true;
            });
            Songs.Clear();
            Artists.Clear();
            Albums.Clear();
            var loadMusicTasks = new List<Task>();
            if (Folders.Any())
            {
                foreach (var folder in Folders)
                {
                    _musicFolders.TryAdd(folder.Path, 0);
                    loadMusicTasks.Add(LoadMusicAsync(folder, folder.DisplayName));
                }
            }
            await Task.WhenAll(loadMusicTasks);
            await EnqueueAndWaitAsync(() =>
            {
                OnPropertyChanged(nameof(HasMusics));
            });
            Genres = [.. _musicGenres.Keys
                .Concat(["MusicInfo_AllGenres".GetLocalized()])
                .OrderBy(x => x, new GenreComparer())];
            _musicGenres.Clear();
            FolderWatchers.Clear();
            _musicFolders.Clear();
            var data = new MusicLibraryData(Songs, Albums, Artists, Genres);
            await Task.Run(AddFolderWatcher);
            FileManager.SaveLibraryDataAsync(Folders, data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsProgressRingActive = false;
            });
            _librarySemaphore.Release();
            GC.Collect();
        }
    }

    private async Task LoadMusicAsync(StorageFolder folder, string foldername)
    {
        try
        {
            var entries = await folder.GetItemsAsync();
            var loadMusicTasks = new List<Task>();

            // 先分配扫描子文件夹的任务
            foreach (var subFolder in entries.OfType<StorageFolder>())
            {
                if (_musicFolders.TryAdd(subFolder.Path, 0))
                {
                    var subfoldername = $"{foldername}/{subFolder.DisplayName}";
                    loadMusicTasks.Add(LoadMusicAsync(subFolder, subfoldername));
                }
            }

            // 同时处理当前文件夹的文件
            var supportedFiles = entries.OfType<StorageFile>().Where(file => Data.SupportedAudioTypes.Contains(file.FileType.ToLower()));

            foreach (var file in supportedFiles)
            {
                var briefMusicInfo = await BriefMusicInfo.CreateAsync(file.Path, foldername);
                Songs.Add(briefMusicInfo);
                _musicGenres.TryAdd(briefMusicInfo.GenreStr, 0);
                UpdateAlbumInfo(briefMusicInfo);
                UpdateArtistInfo(briefMusicInfo);
                briefMusicInfo.Cover = null;
            }

            // 等待所有子文件夹的扫描任务完成
            await Task.WhenAll(loadMusicTasks);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    private void UpdateAlbumInfo(BriefMusicInfo briefMusicInfo)
    {
        var album = briefMusicInfo.Album;

        if (!Albums.TryGetValue(album, out var albumInfo))
        {
            albumInfo = new AlbumInfo(briefMusicInfo);
            Albums[album] = albumInfo;
        }
        else
        {
            albumInfo.Update(briefMusicInfo);
        }
    }

    private void UpdateArtistInfo(BriefMusicInfo briefMusicInfo)
    {
        foreach (var artist in briefMusicInfo.Artists)
        {
            if (!Artists.TryGetValue(artist, out var artistInfo))
            {
                artistInfo = new ArtistInfo(briefMusicInfo, artist);
                Artists[artist] = artistInfo;
            }
            else
            {
                artistInfo.Update(briefMusicInfo);
            }
        }
    }

    private void AddFolderWatcher()
    {
        try
        {
            foreach (var folder in _musicFolders.Keys)
            {
                var watcher = new FileSystemWatcher(folder)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = false
                };

                watcher.Changed -= OnChanged;
                watcher.Created -= OnChanged;
                watcher.Deleted -= OnChanged;
                watcher.Renamed -= OnRenamed;

                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                watcher.EnableRaisingEvents = true;
                FolderWatchers.Add(watcher);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (_isHandlingChange || Data.IsMusicDownloading)
        {
            return;
        }
        _isHandlingChange = true;
        try
        {
            var fileExtension = Path.GetExtension(e.FullPath).ToLower();
            if (Data.SupportedAudioTypes.Contains(fileExtension))
            {
                await LoadLibraryAgainAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
        finally
        {
            _isHandlingChange = false;
        }
    }

    private async void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (_isHandlingChange || Data.IsMusicDownloading)
        {
            return;
        }
        _isHandlingChange = true;
        try
        {
            var fileExtension = Path.GetExtension(e.FullPath).ToLower();
            if (Data.SupportedAudioTypes.Contains(fileExtension))
            {
                await LoadLibraryAgainAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
        finally
        {
            _isHandlingChange = false;
        }
    }

    /// <summary>
    /// 根据专辑信息获取歌曲列表
    /// </summary>
    /// <param name="albumInfo"></param>
    /// <returns></returns>
    public IOrderedEnumerable<BriefMusicInfo> GetSongsByAlbum(AlbumInfo albumInfo) => Songs
        .Where(m => m.Album == albumInfo.Name)
        .OrderBy(m => m.Title, new TitleComparer());

    /// <summary>
    /// 根据歌曲信息获取专辑信息
    /// </summary>
    /// <param name="briefMusicInfo"></param>
    /// <returns></returns>
    public AlbumInfo? GetAlbumInfoBySong(string album) => Albums
        .TryGetValue(album, out var albumInfo) ? albumInfo : null;

    /// <summary>
    /// 根据艺术家信息获取专辑列表
    /// </summary>
    /// <param name="artistInfo"></param>
    /// <returns></returns>
    public List<BriefAlbumInfo> GetAlbumsByArtist(ArtistInfo artistInfo) => [.. artistInfo.Albums
        .Select(album => new BriefAlbumInfo(Albums[album]))
        .OrderBy(m => m.Name, new AlbumTitleComparer())];

    /// <summary>
    /// 根据艺术家信息获取歌曲列表
    /// </summary>
    /// <param name="artistInfo"></param>
    /// <returns></returns>
    public ObservableCollection<BriefMusicInfo> GetSongsByArtist(ArtistInfo artistInfo) => [.. artistInfo.Albums
        .OrderBy(album => album, new AlbumTitleComparer())
        .SelectMany(album => GetSongsByAlbum(Albums[album]))];

    /// <summary>
    /// 根据歌曲信息获取艺术家信息
    /// </summary>
    /// <param name="briefMusicInfo"></param>
    /// <returns></returns>
    public ArtistInfo? GetArtistInfoBySong(string artist) => Artists
        .TryGetValue(artist, out var artistInfo) ? artistInfo : null;

    private async Task EnqueueAndWaitAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                // 执行原始操作
                action();
                // 标记任务完成
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                // 如果发生异常，设置为异常状态
                tcs.SetException(ex);
            }
        });

        // 等待操作完成
        await tcs.Task;
    }
}
