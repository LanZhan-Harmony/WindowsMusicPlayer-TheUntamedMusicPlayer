using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MemoryPack;
using Windows.Storage;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.Models;

public class FileManager
{
    /// <summary>
    /// 保存音乐库数据到文件
    /// </summary>
    public static void SaveLibraryDataAsync(
        ObservableCollection<StorageFolder> folders,
        MusicLibraryData data
    )
    {
        Task.Run(async () =>
        {
            var songs = data.Songs;
            var albums = data.Albums;
            var artists = data.Artists;
            var genres = data.Genres;
            var musicFolders = data.MusicFolders;

            if (songs.IsEmpty)
            {
                return; // 没有数据，不需要保存
            }

            try
            {
                // 创建音乐库数据目录
                var localFolder = ApplicationData.Current.LocalFolder;
                var libraryFolder = await localFolder.CreateFolderAsync(
                    "LibraryData",
                    CreationCollisionOption.OpenIfExists
                );

                // 计算并保存文件夹指纹
                var folderFingerprints = new Dictionary<string, string>();
                foreach (var folder in folders)
                {
                    folderFingerprints[folder.Path] = GetFolderFingerprintFastAsync(folder);
                }
                await SaveObjectToFileAsync(
                    libraryFolder,
                    "FolderFingerprints",
                    folderFingerprints
                );

                var songsTask = SaveObjectToFileAsync(libraryFolder, "Songs", songs); // 保存歌曲列表
                var albumsTask = SaveObjectToFileAsync(libraryFolder, "Albums", albums); // 保存专辑数据
                var artistsTask = SaveObjectToFileAsync(libraryFolder, "Artists", artists); // 保存艺术家数据
                var genresTask = SaveObjectToFileAsync(libraryFolder, "Genres", genres); // 保存流派列表
                var musicFoldersTask = SaveObjectToFileAsync(
                    libraryFolder,
                    "MusicFolders",
                    musicFolders
                ); // 保存音乐文件夹列表

                await Task.WhenAll(
                    songsTask,
                    albumsTask,
                    artistsTask,
                    genresTask,
                    musicFoldersTask
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存音乐库数据错误: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 保存播放队列数据到文件
    /// </summary>
    /// <param name="playQueue"></param>
    /// <param name="shuffledPlayQueue"></param>
    public static async Task SavePlayQueueDataAsync(
        ObservableCollection<IndexedPlayQueueSong> playQueue,
        ObservableCollection<IndexedPlayQueueSong> shuffledPlayQueue
    )
    {
        await Task.Run(async () =>
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var playQueueFolder = await localFolder.CreateFolderAsync(
                    "PlayQueueData",
                    CreationCollisionOption.OpenIfExists
                );

                await SaveObjectToFileAsync(playQueueFolder, "PlayQueue", playQueue); // 保存播放队列
                await SaveObjectToFileAsync(
                    playQueueFolder,
                    "ShuffledPlayQueue",
                    shuffledPlayQueue
                ); // 保存随机播放队列
            }
            catch { }
        });
    }

    /// <summary>
    /// 保存播放列表数据到文件
    /// </summary>
    /// <param name="playlists"></param>
    public static async Task SavePlaylistDataAsync(List<PlaylistInfo> playlists)
    {
        await Task.Run(async () =>
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var playlistFolder = await localFolder.CreateFolderAsync(
                    "PlaylistData",
                    CreationCollisionOption.OpenIfExists
                );

                await SaveObjectToFileAsync(playlistFolder, "Playlists", playlists); // 保存播放列表
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存播放列表数据错误: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 从文件加载音乐库数据
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    public static async Task<(bool needRescan, MusicLibraryData data)> LoadLibraryDataAsync(
        ObservableCollection<StorageFolder> folders
    )
    {
        var data = new MusicLibraryData();

        try
        {
            // 获取本地文件夹
            var localFolder = ApplicationData.Current.LocalFolder;

            // 尝试打开音乐库数据目录
            StorageFolder libraryFolder;
            try
            {
                libraryFolder = await localFolder.GetFolderAsync("LibraryData");
            }
            catch
            {
                // 文件夹不存在，需要重新扫描
                return (true, data);
            }

            // 读取保存的文件夹指纹
            var savedFingerprints = await LoadObjectFromFileAsync<Dictionary<string, string>>(
                libraryFolder,
                "FolderFingerprints"
            );
            if (savedFingerprints is null)
            {
                return (true, data);
            }

            // 检查当前文件夹集合是否与保存时相同
            if (folders.Count != savedFingerprints.Count)
            {
                return (true, data);
            }

            // 使用更快速的文件夹变化检测
            foreach (var folder in folders)
            {
                if (!savedFingerprints.TryGetValue(folder.Path, out var savedFingerprint))
                {
                    return (true, data); // 找不到保存的指纹，需要重新扫描
                }

                var currentFingerprint = GetFolderFingerprintFastAsync(folder);
                if (currentFingerprint != savedFingerprint)
                {
                    return (true, data); // 指纹不匹配，需要重新扫描
                }
            }

            // 并行加载所有数据文件
            var songsTask = LoadObjectFromFileAsync<ConcurrentBag<BriefLocalSongInfo>>(
                libraryFolder,
                "Songs"
            );
            var albumsTask = LoadObjectFromFileAsync<ConcurrentDictionary<string, LocalAlbumInfo>>(
                libraryFolder,
                "Albums"
            );
            var artistsTask = LoadObjectFromFileAsync<
                ConcurrentDictionary<string, LocalArtistInfo>
            >(libraryFolder, "Artists");
            var genresTask = LoadObjectFromFileAsync<List<string>>(libraryFolder, "Genres");
            var musicFoldersTask = LoadObjectFromFileAsync<ConcurrentDictionary<string, byte>>(
                libraryFolder,
                "MusicFolders"
            );

            await Task.WhenAll(songsTask, albumsTask, artistsTask, genresTask, musicFoldersTask);

            var songsList = songsTask.Result;
            var albumsDict = albumsTask.Result;
            var artistsDict = artistsTask.Result;
            var genresList = genresTask.Result;
            var musicFoldersDict = musicFoldersTask.Result;

            if (
                songsList is null
                || albumsDict is null
                || artistsDict is null
                || genresList is null
                || musicFoldersDict is null
            )
            {
                return (true, data);
            }

            // 填充数据结构
            data.Songs = songsList;
            data.Albums = albumsDict;
            data.Artists = artistsDict;
            data.Genres = genresList;
            data.MusicFolders = musicFoldersDict;

            return (false, data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载音乐库数据错误: {ex.Message}");
            return (true, data);
        }
    }

    /// <summary>
    /// 从文件加载播放队列数据
    /// </summary>
    /// <returns></returns>
    public static async Task<(
        ObservableCollection<IndexedPlayQueueSong> playQueue,
        ObservableCollection<IndexedPlayQueueSong> shuffledPlayQueue
    )> LoadPlayQueueDataAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder playQueueFolder;
            try
            {
                playQueueFolder = await localFolder.GetFolderAsync("PlayQueueData");
            }
            catch
            {
                return ([], []);
            }

            var playQueuetask = LoadObjectFromFileAsync<ObservableCollection<IndexedPlayQueueSong>>(
                playQueueFolder,
                "PlayQueue"
            );
            var shuffledPlayQueuetask = LoadObjectFromFileAsync<
                ObservableCollection<IndexedPlayQueueSong>
            >(playQueueFolder, "ShuffledPlayQueue");

            await Task.WhenAll(playQueuetask, shuffledPlayQueuetask);

            var playQueueList = playQueuetask.Result ?? [];
            var shuffledPlayQueueList = shuffledPlayQueuetask.Result ?? [];

            return (playQueueList, shuffledPlayQueueList);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载播放队列数据错误: {ex.Message}");
            return ([], []);
        }
    }

    /// <summary>
    /// 从文件加载播放列表数据
    /// </summary>
    /// <returns></returns>
    public static async Task<List<PlaylistInfo>> LoadPlaylistDataAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder playlistFolder;
            try
            {
                playlistFolder = await localFolder.GetFolderAsync("PlaylistData");
            }
            catch
            {
                return [];
            }

            return await LoadObjectFromFileAsync<List<PlaylistInfo>>(playlistFolder, "Playlists")
                ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载播放列表数据错误: {ex.Message}");
            return [];
        }
    }

    public static async Task<List<PlaylistInfo>> LoadPlaylistDataAsync(StorageFile file)
    {
        try
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            var data = new byte[buffer.Length];
            using var dataReader = DataReader.FromBuffer(buffer);
            dataReader.ReadBytes(data);
            return MemoryPackSerializer.Deserialize<List<PlaylistInfo>>(data) ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载播放列表数据错误: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 使用二进制序列化保存对象到文件
    /// </summary>
    public static async Task SaveObjectToFileAsync<T>(StorageFolder folder, string fileName, T obj)
    {
        var data = MemoryPackSerializer.Serialize(obj);
        var file = await folder.CreateFileAsync(
            fileName + ".bin",
            CreationCollisionOption.ReplaceExisting
        );
        await FileIO.WriteBytesAsync(file, data);
    }

    /// <summary>
    /// 从文件加载对象
    /// </summary>
    public static async Task<T?> LoadObjectFromFileAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T
    >(StorageFolder folder, string fileName)
    {
        try
        {
            StorageFile file;
            try
            {
                file = await folder.GetFileAsync(fileName + ".bin");
            }
            catch
            {
                return default;
            }

            var buffer = await FileIO.ReadBufferAsync(file);
            var data = new byte[buffer.Length];
            using var dataReader = DataReader.FromBuffer(buffer);
            dataReader.ReadBytes(data);
            return MemoryPackSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载对象错误: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 超快速文件夹变化检测，使用缓存和最简单的检测方法
    /// 仅检查文件夹修改时间，不完全准确但极快
    /// </summary>
    private static string GetFolderFingerprintFastAsync(StorageFolder folder)
    {
        var folderPath = folder.Path;
        try
        {
            var dirInfo = new DirectoryInfo(folderPath);
            if (!dirInfo.Exists)
            {
                var guid = $"{Guid.NewGuid()}";
                return guid;
            }

            // 只使用文件夹的最后写入时间和语言作为指纹，这是最快的方法
            var fingerprint =
                $"{dirInfo.LastWriteTime.Ticks}-{CultureInfo.CurrentUICulture.Name.ToLowerInvariant()}";
            return fingerprint;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"快速计算文件夹指纹错误: {ex.Message}");
            var guid = $"{Guid.NewGuid()}";
            return guid;
        }
    }
}

/// <summary>
/// 音乐库数据容器
/// </summary>
public class MusicLibraryData
{
    public ConcurrentBag<BriefLocalSongInfo> Songs { get; set; } = null!;
    public ConcurrentDictionary<string, LocalAlbumInfo> Albums { get; set; } = null!;
    public ConcurrentDictionary<string, LocalArtistInfo> Artists { get; set; } = null!;
    public List<string> Genres { get; set; } = null!;
    public ConcurrentDictionary<string, byte> MusicFolders { get; set; } = null!;

    public MusicLibraryData() { }

    public MusicLibraryData(
        ConcurrentBag<BriefLocalSongInfo> songs,
        ConcurrentDictionary<string, LocalAlbumInfo> albums,
        ConcurrentDictionary<string, LocalArtistInfo> artists,
        List<string> genres,
        ConcurrentDictionary<string, byte> musicFolders
    )
    {
        Songs = songs;
        Albums = albums;
        Artists = artists;
        Genres = genres;
        MusicFolders = musicFolders;
    }
}
