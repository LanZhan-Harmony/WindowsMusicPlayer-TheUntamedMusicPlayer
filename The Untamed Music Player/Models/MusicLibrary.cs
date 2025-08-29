using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using Windows.Storage;
using ZLinq;

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
    /// 音乐文件夹及其子文件夹(临时), 注意不要用HashSet, 因为并行
    /// </summary>
    private ConcurrentDictionary<string, byte> _musicFolders = [];

    /// <summary>
    /// 音乐流派(临时)
    /// </summary>
    private readonly ConcurrentDictionary<string, byte> _musicGenres = [];

    /// <summary>
    /// 文件夹监视器
    /// </summary>
    public List<FileSystemWatcher> FolderWatchers { get; set; } = [];

    /// <summary>
    /// 歌曲列表
    /// </summary>
    public ConcurrentBag<BriefLocalSongInfo> Songs { get; set; } = [];

    /// <summary>
    /// 专辑列表
    /// </summary>
    public ConcurrentDictionary<string, LocalAlbumInfo> Albums { get; set; } = [];

    /// <summary>
    /// 艺术家列表
    /// </summary>
    public ConcurrentDictionary<string, LocalArtistInfo> Artists { get; set; } = [];

    /// <summary>
    /// 是否显示正在重新扫描进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = false;

    /// <summary>
    /// 文件夹列表
    /// </summary>
    public ObservableCollection<StorageFolder> Folders { get; set; } = [];

    /// <summary>
    /// 流派列表
    /// </summary>
    public List<string> Genres { get; set; } = [];

    public MusicLibrary()
        : base(StrongReferenceMessenger.Default)
    {
        LoadFoldersAsync();
    }

    public async void LoadFoldersAsync()
    {
        await _librarySemaphore.WaitAsync(); // 防止本函数未执行完就执行 LoadLibraryAsync
        var folderPaths = await ApplicationData.Current.LocalFolder.ReadAsync<List<string>>(
            "MusicFolders"
        ); //ApplicationData.Current.LocalFolder：获取应用程序的本地存储文件夹。ReadAsync<List<string>>("MusicFolders")：调用 SettingsStorageExtensions 类中的扩展方法 ReadAsync，从名为 "MusicFolders" 的文件中读取数据，然后将其反序列化为 List<string> 类型。
        if (folderPaths is not null)
        {
            foreach (var path in folderPaths)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        continue;
                    }
                    var folder = await StorageFolder.GetFolderFromPathAsync(path);
                    Folders.Add(folder);
                }
                catch { }
            }
            Data.SettingsViewModel?.NotifyEmptyFolderMessageVisibilityChanged();
        }
        _librarySemaphore.Release();
    }

    public async Task LoadLibraryAsync()
    {
        await Task.Run(async () =>
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
                    _musicFolders = libraryData.MusicFolders;
                    await InitializeCovers();
                    _dispatcherQueue.TryEnqueue(() =>
                        Messenger.Send(new HaveMusicMessage(!Songs.IsEmpty))
                    );
                }
                else
                {
                    var loadMusicTasks = new List<Task>();
                    if (Folders.Count > 0)
                    {
                        foreach (var folder in Folders)
                        {
                            _musicFolders.TryAdd(folder.Path, 0);
                            loadMusicTasks.Add(LoadMusicAsync(folder, folder.DisplayName));
                        }
                    }
                    await Task.WhenAll(loadMusicTasks);
                    Genres =
                    [
                        .. _musicGenres
                            .Keys.AsValueEnumerable()
                            .Concat(["SongInfo_AllGenres".GetLocalized()])
                            .OrderBy(x => x, new GenreComparer()),
                    ];
                    await InitializeCovers();
                    _dispatcherQueue.TryEnqueue(() =>
                        Messenger.Send(new HaveMusicMessage(!Songs.IsEmpty))
                    );
                    _musicGenres.Clear();
                    var data = new MusicLibraryData(Songs, Albums, Artists, Genres, _musicFolders);
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
                _ = Task.Run(LoadCovers);
                _ = Task.Run(AddFolderWatcher);
                _librarySemaphore.Release();
                GC.Collect();
            }
        });
    }

    public async Task LoadLibraryAgainAsync()
    {
        await Task.Run(async () =>
        {
            await _librarySemaphore.WaitAsync();
            try
            {
                _dispatcherQueue.TryEnqueue(() => IsProgressRingActive = true);
                Songs.Clear();
                Artists.Clear();
                Albums.Clear();
                _musicFolders.Clear();
                var loadMusicTasks = new List<Task>();
                if (Folders.Count > 0)
                {
                    foreach (var folder in Folders)
                    {
                        _musicFolders.TryAdd(folder.Path, 0);
                        loadMusicTasks.Add(LoadMusicAsync(folder, folder.DisplayName));
                    }
                }
                await Task.WhenAll(loadMusicTasks);
                await InitializeCovers();
                Genres =
                [
                    .. _musicGenres
                        .Keys.AsValueEnumerable()
                        .Concat(["SongInfo_AllGenres".GetLocalized()])
                        .OrderBy(x => x, new GenreComparer()),
                ];
                _dispatcherQueue.TryEnqueue(() =>
                    Messenger.Send(new HaveMusicMessage(!Songs.IsEmpty))
                );
                _musicGenres.Clear();
                FolderWatchers.Clear();
                var data = new MusicLibraryData(Songs, Albums, Artists, Genres, _musicFolders);
                _ = Task.Run(LoadCovers);
                _ = Task.Run(AddFolderWatcher);
                FileManager.SaveLibraryDataAsync(Folders, data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
            finally
            {
                _dispatcherQueue.TryEnqueue(() => IsProgressRingActive = false);
                _librarySemaphore.Release();
                GC.Collect();
            }
        });
    }

    public Task<bool> InitializeCovers()
    {
        var tcs = new TaskCompletionSource<bool>();
        _dispatcherQueue.TryEnqueue(() =>
        {
            foreach (var album in Albums.Values)
            {
                album.InitializeCover();
            }
            tcs.SetResult(true);
        });
        return tcs.Task;
    }

    public void LoadCovers()
    {
        foreach (var album in Albums.Values)
        {
            album.LoadCover();
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
            var supportedFiles = entries
                .AsValueEnumerable()
                .OfType<StorageFile>()
                .Where(file => Data.SupportedAudioTypes.Contains(file.FileType.ToLower()));

            foreach (var file in supportedFiles)
            {
                var briefLocalSongInfo = new BriefLocalSongInfo(file.Path, foldername);
                if (!briefLocalSongInfo.IsPlayAvailable)
                {
                    continue;
                }
                Songs.Add(briefLocalSongInfo);
                _musicGenres.TryAdd(briefLocalSongInfo.GenreStr, 0);
                UpdateAlbumInfo(briefLocalSongInfo);
                UpdateArtistInfo(briefLocalSongInfo);
            }

            // 等待所有子文件夹的扫描任务完成
            await Task.WhenAll(loadMusicTasks);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    private void UpdateAlbumInfo(BriefLocalSongInfo briefLocalSongInfo)
    {
        var album = briefLocalSongInfo.Album;

        if (!Albums.TryGetValue(album, out var localAlbumInfo))
        {
            localAlbumInfo = new LocalAlbumInfo(briefLocalSongInfo);
            Albums[album] = localAlbumInfo;
        }
        else
        {
            localAlbumInfo.Update(briefLocalSongInfo);
        }
    }

    private void UpdateArtistInfo(BriefLocalSongInfo briefLocalSongInfo)
    {
        foreach (var artist in briefLocalSongInfo.Artists)
        {
            if (!Artists.TryGetValue(artist, out var localArtistInfo))
            {
                localArtistInfo = new LocalArtistInfo(briefLocalSongInfo, artist);
                Artists[artist] = localArtistInfo;
            }
            else
            {
                localArtistInfo.Update(briefLocalSongInfo);
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
                    NotifyFilter =
                        NotifyFilters.FileName
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.LastWrite,
                    IncludeSubdirectories = false,
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
        if (_isHandlingChange || Data.IsMusicProcessing)
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
        if (_isHandlingChange || Data.IsMusicProcessing)
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
    /// <param name="localAlbumInfo"></param>
    /// <returns></returns>
    public BriefLocalSongInfo[] GetSongsByAlbum(LocalAlbumInfo localAlbumInfo) =>
        [
            .. Songs
                .AsValueEnumerable()
                .Where(m => m.Album == localAlbumInfo.Name)
                .OrderBy(m => m.Title, new TitleComparer()),
        ];

    /// <summary>
    /// 根据歌曲信息获取专辑信息
    /// </summary>
    /// <param name="briefLocalSongInfo"></param>
    /// <returns></returns>
    public LocalAlbumInfo? GetAlbumInfoBySong(string album) =>
        Albums.TryGetValue(album, out var localAlbumInfo) ? localAlbumInfo : null;

    /// <summary>
    /// 根据艺术家信息获取专辑列表
    /// </summary>
    /// <param name="localArtistInfo"></param>
    /// <returns></returns>
    public List<LocalArtistAlbumInfo> GetAlbumsByArtist(LocalArtistInfo localArtistInfo) =>
        [
            .. localArtistInfo
                .Albums.AsValueEnumerable()
                .Select(album => new LocalArtistAlbumInfo(Albums[album]))
                .OrderBy(m => m.Name, new AlbumTitleComparer()),
        ];

    /// <summary>
    /// 根据艺术家信息获取歌曲列表
    /// </summary>
    /// <param name="localArtistInfo"></param>
    /// <returns></returns>
    public BriefLocalSongInfo[] GetSongsByArtist(LocalArtistInfo localArtistInfo) =>
        [
            .. localArtistInfo
                .Albums.AsValueEnumerable()
                .OrderBy(album => album, new AlbumTitleComparer())
                .SelectMany(album => GetSongsByAlbum(Albums[album])),
        ];

    /// <summary>
    /// 根据歌曲信息获取艺术家信息
    /// </summary>
    /// <param name="briefLocalSongInfo"></param>
    /// <returns></returns>
    public LocalArtistInfo? GetArtistInfoBySong(string artist) =>
        Artists.TryGetValue(artist, out var localArtistInfo) ? localArtistInfo : null;
}
