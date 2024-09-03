using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.ViewModels;
using Windows.Storage;

namespace The_Untamed_Music_Player.Models;
public class MusicLibrary : INotifyPropertyChanged
{
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

    public bool HasMusics => Musics.Any();

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
                await LoadMusic(folder);
            }
        }
        _musicPaths.Clear();
        OnPropertyChanged(nameof(HasMusics));
    }

    public async Task LoadLibraryAgain()
    {
        Musics?.Clear();
        Artists?.Clear();
        Albums?.Clear();
        if (Folders != null && Folders.Any())
        {
            foreach (var folder in Folders)
            {
                await LoadMusic(folder);
            }
        }
        _musicPaths.Clear();
        OnPropertyChanged(nameof(HasMusics));
    }

    public async void LoadFoldersAsync()
    {
        var folderPaths = await ApplicationData.Current.LocalFolder.ReadAsync<List<string>>("MusicFolders");//	ApplicationData.Current.LocalFolder：获取应用程序的本地存储文件夹。ReadAsync<List<string>>("MusicFolders")：调用 SettingsStorageExtensions 类中的扩展方法 ReadAsync，从名为 "MusicFolders" 的文件中读取数据，并将其反序列化为 List<string> 类型。
        if (folderPaths != null)
        {
            foreach (var path in folderPaths)
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                Folders?.Add(folder);
            }
            OnPropertyChanged(nameof(SettingsViewModel.EmptyFolderMessageVisibility));
            await Data.MusicLibrary.LoadLibrary();
        }
    }

    private async Task LoadMusic(StorageFolder folder)
    {
        try
        {
            var allFiles = await folder.GetFilesAsync();
            var supportedFiles = allFiles.Where(file => Data.SupportedAudioTypes.Contains(file.FileType.ToLower()));

            foreach (var file in supportedFiles)
            {
                if (Musics != null && !_musicPaths.Contains(file.Path))
                {
                    var briefMusicInfo = new BriefMusicInfo(file.Path);
                    Musics.Add(briefMusicInfo);
                    _musicPaths.Add(file.Path);
                    UpdateAlbumInfo(briefMusicInfo);
                    UpdateArtistInfo(briefMusicInfo);
                }
            }

            var subFolders = await folder.GetFoldersAsync();
            foreach (var subFolder in subFolders)
            {
                await LoadMusic(subFolder);
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
        if (!string.IsNullOrEmpty(album) && Albums != null)
        {
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
    }

    private void UpdateArtistInfo(BriefMusicInfo briefMusicInfo)
    {
        if (briefMusicInfo.Artists != null && Artists != null)
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
    }

    public ObservableCollection<BriefMusicInfo> GetMusicByAlbum(AlbumInfo albumInfo)
    {
        var list = new ObservableCollection<BriefMusicInfo>();
        var albumName = albumInfo.Name;
        if (Musics != null)
        {
            foreach (var music in Musics)
            {
                if (music.Album == albumName && music.Path != null)
                {
                    list.Add(new BriefMusicInfo(music.Path));
                }
            }
        }
        return list;
    }

    public ObservableCollection<AlbumInfo> GetAlbumByArtist(ArtistInfo artistInfo)
    {
        var list = new ObservableCollection<AlbumInfo>();
        var artistName = artistInfo.Name;
        var albumDict = artistInfo.Albums;
        if (Musics != null)
        {
            foreach (var music in Musics)
            {
                if (music.Artists != null && music.Artists.Contains(artistName) && music.Album != null && albumDict != null && albumDict.TryGetValue(music.Album, out var albumInfo))
                {
                    list.Add(albumInfo);
                }
            }
        }
        return list;
    }
}
