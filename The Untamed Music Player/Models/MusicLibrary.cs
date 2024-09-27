using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.ViewModels;
using Windows.Storage;

namespace The_Untamed_Music_Player.Models;
public class MusicLibrary : INotifyPropertyChanged
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private ObservableCollection<StorageFolder> _folders = [];
    public ObservableCollection<StorageFolder> Folders
    {
        get => _folders;
        set
        {
            _folders = value;
            OnPropertyChanged(nameof(SettingsViewModel.EmptyFolderMessageVisibility));
        }
    }

    private List<FileSystemWatcher> _folderWatchers = new();
    public List<FileSystemWatcher> FolderWatchers
    {
        get => _folderWatchers;
        set => _folderWatchers = value;
    }

    private readonly HashSet<string> _musicPaths = [];

    private List<BriefMusicInfo> _musics = [];
    public List<BriefMusicInfo> Musics
    {
        get => _musics;
        set
        {
            _musics = value;
            OnPropertyChanged(nameof(HasMusics));
        }
    }

    public bool HasMusics => Musics.Count != 0;

    private Dictionary<string, AlbumInfo> _albums = [];
    public Dictionary<string, AlbumInfo> Albums
    {
        get => _albums;
        set
        {
            _albums = value;
            OnPropertyChanged(nameof(Albums));
        }
    }

    private Dictionary<string, ArtistInfo> _artists = [];
    public Dictionary<string, ArtistInfo> Artists
    {
        get => _artists;
        set
        {
            _artists = value;
            OnPropertyChanged(nameof(Artists));
        }
    }

    private readonly HashSet<string> _musicGenres = [];

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

    public async Task LoadLibrary()
    {
        if (Folders != null && Folders.Any())
        {
            foreach (var folder in Folders)
            {
                await LoadMusic(folder, folder.DisplayName);
            }
        }
        ClearAllArtistMusicAlbums();
        _musicPaths.Clear();
        _musicGenres.Clear();
        Genres.Add("MusicInfo_AllGenres".GetLocalized());
        Genres = new ObservableCollection<string>(Genres.OrderBy(x => x, new GenreComparer()));
        OnPropertyChanged(nameof(HasMusics));
    }

    public async Task LoadLibraryAgain()
    {
        IsProgressRingActive = true;
        IsRefreshButtonEnabled = false;
        Musics.Clear();
        Artists.Clear();
        Albums.Clear();
        Genres.Clear();
        if (Folders != null && Folders.Any())
        {
            foreach (var folder in Folders)
            {
                await LoadMusic(folder, folder.DisplayName);
            }
        }
        ClearAllArtistMusicAlbums();
        _musicPaths.Clear();
        _musicGenres.Clear();
        Genres.Add("MusicInfo_AllGenres".GetLocalized());
        Genres = new ObservableCollection<string>(Genres.OrderBy(x => x, new GenreComparer()));
        OnPropertyChanged(nameof(HasMusics));
        IsProgressRingActive = false;
        IsRefreshButtonEnabled = true;
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
                AddFolderWatcher(path);
            }
            OnPropertyChanged(nameof(SettingsViewModel.EmptyFolderMessageVisibility));
            await Data.MusicLibrary.LoadLibrary();
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
                if (_musicPaths.Add(file.Path))
                {
                    var briefMusicInfo = new BriefMusicInfo(file.Path, foldername);
                    Musics.Add(briefMusicInfo);

                    if (_musicGenres.Add(briefMusicInfo.GenreStr))
                    {
                        Genres.Add(briefMusicInfo.GenreStr);
                    }

                    UpdateAlbumInfo(briefMusicInfo);
                    UpdateArtistInfo(briefMusicInfo);
                }
            }

            var subFolders = await folder.GetFoldersAsync();
            foreach (var subFolder in subFolders)
            {
                var subfoldername = foldername + "/" + subFolder.DisplayName;
                await LoadMusic(subFolder, subfoldername);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
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

    public void ClearAllArtistMusicAlbums()
    {
        foreach (var artistInfo in Artists.Values)
        {
            artistInfo.ClearAlbums();
        }
    }

    private void AddFolderWatcher(string path)
    {
        var watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            IncludeSubdirectories = true // 启用对子文件夹的监视
        };

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;

        watcher.EnableRaisingEvents = true;
        FolderWatchers.Add(watcher);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var fileExtension = Path.GetExtension(e.FullPath).ToLower();
        if (Data.SupportedAudioTypes.Contains(fileExtension))
        {
            try
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    await LoadLibraryAgain();
                });
            }
            catch { }
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        var fileExtension = Path.GetExtension(e.FullPath).ToLower();
        if (Data.SupportedAudioTypes.Contains(fileExtension))
        {
            try
            {
                _dispatcherQueue.TryEnqueue(async () =>
               {
                   await LoadLibraryAgain();
               });
            }
            catch { }
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.WriteLine(e.GetException());
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
