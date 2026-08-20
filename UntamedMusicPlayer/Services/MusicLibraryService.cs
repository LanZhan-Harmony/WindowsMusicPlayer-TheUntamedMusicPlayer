using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Core.Constants;
using UntamedMusicPlayer.Core.Contracts.Services;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Messages;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.Services;
using Windows.Storage;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Services;

public sealed partial class MusicLibrary : ObservableRecipient
{
    private static readonly ILogger _logger = LoggingService.CreateLogger<MusicLibrary>();

    /// <summary>
    /// UI-independent read index built from the current song snapshot.
    /// </summary>
    public LocalLibraryIndex Index { get; } = new();

    private readonly LocalMusicLibraryScanner _scanner = new();

    /// <summary>
    /// 调度器队列
    /// </summary>
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly IAppStateService _appStateService;
    private readonly ICoverCacheInvalidationService _coverCache;

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
    /// 当前扫描任务收集到的歌曲。扫描完成后会一次性发布到 Index。
    /// </summary>
    private ConcurrentBag<BriefLocalSongInfo> _scannedSongs = [];

    public bool HasLoaded { get; private set; } = false;

    /// <summary>
    /// 文件夹监视器
    /// </summary>
    public List<FileSystemWatcher> FolderWatchers { get; set; } = [];

    /// <summary>
    /// 是否显示正在重新扫描进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = false;

    /// <summary>
    /// 文件夹列表
    /// </summary>
    public ObservableCollection<string> Folders { get; set; } = [];

    public MusicLibrary(IAppStateService appStateService, ICoverCacheInvalidationService coverCache)
        : base(StrongReferenceMessenger.Default)
    {
        _appStateService =
            appStateService ?? throw new ArgumentNullException(nameof(appStateService));
        _coverCache = coverCache ?? throw new ArgumentNullException(nameof(coverCache));
        RunFireAndForget(LoadFoldersAsync());
    }

    public async Task LoadFoldersAsync()
    {
        await _librarySemaphore.WaitAsync(); // 防止本函数未执行完就执行 LoadLibraryAsync
        try
        {
            var folderPaths = await ApplicationData.Current.LocalFolder.ReadAsync<List<string>>(
                "MusicFolders"
            );
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
                        Folders.Add(path);
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogInformation(ex, $"加载音乐文件夹失败：{path}");
                    }
                }
                Messenger.Send(new MusicFoldersChangedMessage());
            }
        }
        finally
        {
            _librarySemaphore.Release();
        }
    }

    public async Task LoadLibraryAsync()
    {
        await Task.Run(async () =>
        {
            await _librarySemaphore.WaitAsync(); // 等待信号量, 只允许一个线程访问此函数
            try
            {
                _scannedSongs = [];
                Index.RebuildFromSongs(_scannedSongs);
                var (needRescan, libraryData) = await FileManager.LoadLibraryDataAsync(Folders);
                if (!needRescan)
                {
                    _scannedSongs = libraryData.Songs;
                    _musicFolders = libraryData.MusicFolders;
                    Index.RebuildFromSongs(_scannedSongs);
                    _dispatcher.TryEnqueue(() =>
                        Messenger.Send(new HaveMusicMessage(Index.HasSongs))
                    );
                }
                else
                {
                    var loadMusicTasks = new List<Task>();
                    if (Folders.Count > 0)
                    {
                        foreach (var folder in Folders)
                        {
                            _musicFolders.TryAdd(folder, 0);
                            var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder);
                            loadMusicTasks.Add(
                                _scanner.ScanAsync(
                                    storageFolder,
                                    storageFolder.DisplayName,
                                    _musicFolders,
                                    _scannedSongs
                                )
                            );
                        }
                    }
                    await Task.WhenAll(loadMusicTasks);
                    Index.RebuildFromSongs(_scannedSongs);
                    _dispatcher.TryEnqueue(() =>
                        Messenger.Send(new HaveMusicMessage(Index.HasSongs))
                    );
                    await FileManager.SaveLibraryDataAsync(Folders, CreateLibraryData());
                }
                HasLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"加载音乐库失败");
            }
            finally
            {
                _ = Task.Run(AddFolderWatcher);
                _coverCache.InvalidateAllSongCovers();
                _librarySemaphore.Release();
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
                _dispatcher.TryEnqueue(() => IsProgressRingActive = true);
                _scannedSongs = [];
                Index.RebuildFromSongs(_scannedSongs);
                _musicFolders.Clear();
                var loadMusicTasks = new List<Task>();
                if (Folders.Count > 0)
                {
                    foreach (var folder in Folders)
                    {
                        _musicFolders.TryAdd(folder, 0);
                        var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder);
                        loadMusicTasks.Add(
                            _scanner.ScanAsync(
                                storageFolder,
                                storageFolder.DisplayName,
                                _musicFolders,
                                _scannedSongs
                            )
                        );
                    }
                }
                await Task.WhenAll(loadMusicTasks);
                Index.RebuildFromSongs(_scannedSongs);
                _dispatcher.TryEnqueue(() => Messenger.Send(new HaveMusicMessage(Index.HasSongs)));
                FolderWatchers.Clear();
                _ = Task.Run(AddFolderWatcher);
                _coverCache.InvalidateAllSongCovers();
                await FileManager.SaveLibraryDataAsync(Folders, CreateLibraryData());
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"重新加载音乐库失败");
            }
            finally
            {
                _dispatcher.TryEnqueue(() => IsProgressRingActive = false);
                _librarySemaphore.Release();
            }
        });
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
            _logger.ZLogInformation(ex, $"添加文件夹监视器失败");
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e) =>
        RunFireAndForget(HandleFolderChangedAsync(e.FullPath, "变更"));

    private void OnRenamed(object sender, RenamedEventArgs e) =>
        RunFireAndForget(HandleFolderChangedAsync(e.FullPath, "重命名"));

    private async Task HandleFolderChangedAsync(string fullPath, string changeDescription)
    {
        if (_isHandlingChange || _appStateService.IsMusicProcessing)
        {
            return;
        }
        _isHandlingChange = true;
        try
        {
            var fileExtension = Path.GetExtension(fullPath).ToLower();
            if (AppConstants.SupportedAudioTypes.Contains(fileExtension))
            {
                await LoadLibraryAgainAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"处理文件夹{changeDescription}事件失败");
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
        Index.GetSongsByAlbum(localAlbumInfo);

    /// <summary>
    /// 根据歌曲信息获取专辑信息
    /// </summary>
    /// <param name="briefLocalSongInfo"></param>
    /// <returns></returns>
    public LocalAlbumInfo? GetAlbumInfoBySong(string album) => Index.GetAlbumInfoBySong(album);

    /// <summary>
    /// 根据艺术家信息获取专辑列表
    /// </summary>
    /// <param name="localArtistInfo"></param>
    /// <returns></returns>
    public List<LocalArtistAlbumInfo> GetAlbumsByArtist(LocalArtistInfo localArtistInfo) =>
        [
            .. Index
                .GetAlbumsByArtist(localArtistInfo)
                .Select(album => new LocalArtistAlbumInfo(
                    album,
                    Index.GetSongsByAlbum(album),
                    album.CoverPath
                ))
                .OrderBy(m => m.Name, new AlbumTitleComparer()),
        ];

    /// <summary>
    /// 根据艺术家信息获取歌曲列表
    /// </summary>
    /// <param name="localArtistInfo"></param>
    /// <returns></returns>
    public BriefLocalSongInfo[] GetSongsByArtist(LocalArtistInfo localArtistInfo) =>
        Index.GetSongsByArtist(localArtistInfo);

    /// <summary>
    /// 根据歌曲信息获取艺术家信息
    /// </summary>
    /// <param name="briefLocalSongInfo"></param>
    /// <returns></returns>
    public LocalArtistInfo? GetArtistInfoBySong(string artist) => Index.GetArtistInfoBySong(artist);

    /// <summary>
    /// Gets the localized genre options used by local-library filters.
    /// The all-genres entry is a presentation option and is not part of the index data.
    /// </summary>
    public List<string> GetGenreOptions() =>
        [
            .. Index
                .Genres.Concat(["SongInfo_AllGenres".GetLocalized()])
                .OrderBy(x => x, new GenreComparer()),
        ];

    private MusicLibraryData CreateLibraryData() => new(Index.GetSongsSnapshot(), _musicFolders);

    private void RunFireAndForget(Task task)
    {
        _ = RunFireAndForgetAsync(task);
    }

    private async Task RunFireAndForgetAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"音乐库异步操作失败");
        }
    }
}
