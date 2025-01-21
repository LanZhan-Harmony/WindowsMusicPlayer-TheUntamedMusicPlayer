using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.ViewModels;
using Windows.Storage;

namespace The_Untamed_Music_Player.Models;
public class MusicLibrary : INotifyPropertyChanged
{
    //private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly SemaphoreSlim _librarySemaphore = new(1, 1);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ConcurrentDictionary<string, byte> _musicFolders = [];

    private ObservableCollection<StorageFolder> _folders = [];
    public ObservableCollection<StorageFolder> Folders
    {
        get => _folders;
        set => _folders = value;
    }

    private List<FileSystemWatcher> _folderWatchers = [];
    public List<FileSystemWatcher> FolderWatchers
    {
        get => _folderWatchers;
        set => _folderWatchers = value;
    }

    private ConcurrentBag<BriefMusicInfo> _musics = [];
    public ConcurrentBag<BriefMusicInfo> Musics
    {
        get => _musics;
        set => _musics = value;
    }

    public bool HasMusics => !Musics.IsEmpty;

    private ConcurrentDictionary<string, AlbumInfo> _albums = [];
    public ConcurrentDictionary<string, AlbumInfo> Albums
    {
        get => _albums;
        set
        {
            _albums = value;
            OnPropertyChanged(nameof(Albums));
        }
    }

    private ConcurrentDictionary<string, ArtistInfo> _artists = [];
    public ConcurrentDictionary<string, ArtistInfo> Artists
    {
        get => _artists;
        set
        {
            _artists = value;
            OnPropertyChanged(nameof(Artists));
        }
    }

    private readonly ConcurrentDictionary<string, byte> _musicGenres = [];

    private ObservableCollection<string> _genres = [];
    public ObservableCollection<string> Genres
    {
        get => _genres;
        set => _genres = value;
    }

    private bool _isProgressRingActive;
    public bool IsProgressRingActive
    {
        get => _isProgressRingActive;
        set
        {
            _isProgressRingActive = value;
            OnPropertyChanged(nameof(IsProgressRingActive));
        }
    }

    private bool _isRefreshButtonEnabled = true;
    public bool IsRefreshButtonEnabled
    {
        get => _isRefreshButtonEnabled;
        set
        {
            _isRefreshButtonEnabled = value;
            OnPropertyChanged(nameof(IsRefreshButtonEnabled));
        }
    }

    public MusicLibrary()
    {
        LoadFoldersAsync();
    }

    public async void LoadFoldersAsync()
    {
        var folderPaths = await ApplicationData.Current.LocalFolder.ReadAsync<List<string>>("MusicFolders");//ApplicationData.Current.LocalFolder：获取应用程序的本地存储文件夹。ReadAsync<List<string>>("MusicFolders")：调用 SettingsStorageExtensions 类中的扩展方法 ReadAsync，从名为 "MusicFolders" 的文件中读取数据，并将其反序列化为 List<string> 类型。
        if (folderPaths != null)
        {
            foreach (var path in folderPaths)
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                Folders?.Add(folder);
            }
            OnPropertyChanged(nameof(SettingsViewModel.EmptyFolderMessageVisibility));
        }
    }

    public async Task LoadLibrary()
    {
        await _librarySemaphore.WaitAsync();
        try
        {
            var loadMusicTasks = new List<Task>();
            if (Folders != null && Folders.Any())
            {
                foreach (var folder in Folders)
                {
                    _musicFolders.TryAdd(folder.Path, 0);
                    loadMusicTasks.Add(LoadMusic(folder, folder.DisplayName));
                }
            }
            await Task.WhenAll(loadMusicTasks);
            OnPropertyChanged(nameof(HasMusics));
            Genres = [.. _musicGenres.Keys, "MusicInfo_AllGenres".GetLocalized()];
            Genres = [.. Genres.OrderBy(x => x, new GenreComparer())];
            ClearAllArtistMusicAlbums();
            _musicGenres.Clear();
            await Task.Run(() => AddFolderWatcher());
            _musicFolders.Clear();
        }
        catch { }
        finally
        {
            _librarySemaphore.Release();
        }
    }

    public async Task LoadLibraryAgain()
    {
        await _librarySemaphore.WaitAsync();
        try
        {
            IsProgressRingActive = true;
            IsRefreshButtonEnabled = false;
            Musics.Clear();
            Artists.Clear();
            Albums.Clear();
            Genres.Clear();
            var loadMusicTasks = new List<Task>();
            if (Folders != null && Folders.Any())
            {
                foreach (var folder in Folders)
                {
                    _musicFolders.TryAdd(folder.Path, 0);
                    loadMusicTasks.Add(LoadMusic(folder, folder.DisplayName));
                }
            }
            await Task.WhenAll(loadMusicTasks);
            OnPropertyChanged(nameof(HasMusics));
            Genres = [.. _musicGenres.Keys, "MusicInfo_AllGenres".GetLocalized()];
            Genres = [.. Genres.OrderBy(x => x, new GenreComparer())];
            IsProgressRingActive = false;
            IsRefreshButtonEnabled = true;
            ClearAllArtistMusicAlbums();
            _musicGenres.Clear();
            await Task.Run(() => AddFolderWatcher());
            _musicFolders.Clear();
        }
        catch { }
        finally
        {
            _librarySemaphore.Release();
        }
    }

    private async Task LoadMusic(StorageFolder folder, string foldername)
    {
        try
        {
            var allFiles = await folder.GetFilesAsync();
            var supportedFiles = allFiles.Where(file => Data.SupportedAudioTypes.Contains(file.FileType.ToLower()));

            foreach (var file in supportedFiles)
            {
                var briefMusicInfo = new BriefMusicInfo(file.Path, foldername);
                Musics.Add(briefMusicInfo);

                _musicGenres.TryAdd(briefMusicInfo.GenreStr, 0);
                UpdateAlbumInfo(briefMusicInfo);
                UpdateArtistInfo(briefMusicInfo);
            }

            var subFolders = await folder.GetFoldersAsync();
            var loadMusicTasks = new List<Task>();

            foreach (var subFolder in subFolders)
            {
                if (_musicFolders.TryAdd(subFolder.Path, 0))
                {
                    var subfoldername = foldername + "/" + subFolder.DisplayName;
                    // 启动 LoadMusic 任务但不等待
                    loadMusicTasks.Add(LoadMusic(subFolder, subfoldername));
                }
            }
            await Task.WhenAll(loadMusicTasks);
        }
        catch { }
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

    public void ClearAllArtistMusicAlbums()
    {
        foreach (var artistInfo in Artists.Values)
        {
            artistInfo.ClearAlbums();
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

                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                watcher.EnableRaisingEvents = true;
                FolderWatchers.Add(watcher);
            }
        }
        catch { }
    }

    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        var fileExtension = Path.GetExtension(e.FullPath).ToLower();
        if (Data.SupportedAudioTypes.Contains(fileExtension))
        {
            try
            {
                await LoadLibraryAgain();
            }
            catch { }
        }
    }

    private async void OnRenamed(object sender, RenamedEventArgs e)
    {
        var fileExtension = Path.GetExtension(e.FullPath).ToLower();
        if (Data.SupportedAudioTypes.Contains(fileExtension))
        {
            try
            {
                await LoadLibraryAgain();
            }
            catch { }
        }
    }

    public ObservableCollection<BriefMusicInfo> GetMusicByAlbum(AlbumInfo albumInfo)
    {
        var list = new ObservableCollection<BriefMusicInfo>();
        var albumName = albumInfo.Name;

        foreach (var music in Musics)
        {
            if (music.Album == albumName)
            {
                list.Add(music);
            }
        }

        return list;
    }

    public ObservableCollection<AlbumInfo> GetAlbumByArtist(ArtistInfo artistInfo)
    {
        var list = new ObservableCollection<AlbumInfo>();
        var artistName = artistInfo.Name;

        foreach (var album in Albums.Values)
        {
            if (album.Artists.Contains(artistName))
            {
                list.Add(album);
            }
        }

        return list;
    }
}
