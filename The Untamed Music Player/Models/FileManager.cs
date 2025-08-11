using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MemoryPack;
using The_Untamed_Music_Player.Contracts.Models;
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

                // 计算并保存文件夹指纹 - 使用快速方法
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

                // 保存歌曲列表 - 将 ConcurrentBag 转换为数组
                await SaveObjectToFileAsync(libraryFolder, "Songs", songs.ToArray());

                // 保存专辑数据 - 将 ConcurrentDictionary 转换为 Dictionary
                await SaveObjectToFileAsync(
                    libraryFolder,
                    "Albums",
                    albums.ToDictionary(kv => kv.Key, kv => kv.Value)
                );

                // 保存艺术家数据
                await SaveObjectToFileAsync(
                    libraryFolder,
                    "Artists",
                    artists.ToDictionary(kv => kv.Key, kv => kv.Value)
                );

                // 保存流派列表
                await SaveObjectToFileAsync(libraryFolder, "Genres", genres.ToArray());

                // 保存音乐文件夹列表
                await SaveObjectToFileAsync(
                    libraryFolder,
                    "MusicFolders",
                    musicFolders.ToDictionary(kv => kv.Key, kv => kv.Value)
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
    public static void SavePlayQueueDataAsync(
        ObservableCollection<IBriefSongInfoBase> playQueue,
        ObservableCollection<IBriefSongInfoBase> shuffledPlayQueue
    )
    {
        Task.Run(async () =>
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var playQueueFolder = await localFolder.CreateFolderAsync(
                "PlayQueueData",
                CreationCollisionOption.OpenIfExists
            );

            // 保存播放队列
            await SaveObjectToFileAsync(playQueueFolder, "PlayQueue", playQueue.ToArray());
            // 保存随机播放队列
            await SaveObjectToFileAsync(
                playQueueFolder,
                "ShuffledPlayQueue",
                shuffledPlayQueue.ToArray()
            );
        });
    }

    /// <summary>
    /// 从文件加载音乐库数据
    /// </summary>
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
            var songsTask = LoadObjectFromFileAsync<BriefLocalSongInfo[]>(libraryFolder, "Songs");
            var albumsTask = LoadObjectFromFileAsync<Dictionary<string, LocalAlbumInfo>>(
                libraryFolder,
                "Albums"
            );
            var artistsTask = LoadObjectFromFileAsync<Dictionary<string, LocalArtistInfo>>(
                libraryFolder,
                "Artists"
            );
            var genresTask = LoadObjectFromFileAsync<string[]>(libraryFolder, "Genres");
            var musicFoldersTask = LoadObjectFromFileAsync<Dictionary<string, byte>>(
                libraryFolder,
                "MusicFolders"
            );

            await Task.WhenAll(songsTask, albumsTask, artistsTask, genresTask, musicFoldersTask);

            var songsArray = songsTask.Result;
            var albumsDict = albumsTask.Result;
            var artistsDict = artistsTask.Result;
            var genresArray = genresTask.Result;
            var musicFoldersDict = musicFoldersTask.Result;

            if (
                songsArray is null
                || albumsDict is null
                || artistsDict is null
                || genresArray is null
                || musicFoldersDict is null
            )
            {
                return (true, data);
            }

            // 填充数据结构
            data.Songs = [.. songsArray];
            data.Albums = new ConcurrentDictionary<string, LocalAlbumInfo>(albumsDict);
            data.Artists = new ConcurrentDictionary<string, LocalArtistInfo>(artistsDict);
            data.Genres = [.. genresArray];
            data.MusicFolders = new ConcurrentDictionary<string, byte>(musicFoldersDict);

            // 加载所有专辑封面
            foreach (var album in albumsDict.Values)
            {
                album.LoadCover();
            }

            return (false, data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载音乐库数据错误: {ex.Message}");
            return (true, data);
        }
    }

    public static async Task<(
        ObservableCollection<IBriefSongInfoBase> playQueue,
        ObservableCollection<IBriefSongInfoBase> shuffledPlayQueue
    )> LoadPlayQueueDataAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            // 尝试打开音乐库数据目录
            StorageFolder playQueueFolder;
            try
            {
                playQueueFolder = await localFolder.GetFolderAsync("PlayQueueData");
            }
            catch
            {
                // 文件夹不存在，需要重新扫描
                return ([], []);
            }

            var playQueuetask = LoadObjectFromFileAsync<IBriefSongInfoBase[]>(
                playQueueFolder,
                "PlayQueue"
            );
            var shuffledPlayQueuetask = LoadObjectFromFileAsync<IBriefSongInfoBase[]>(
                playQueueFolder,
                "ShuffledPlayQueue"
            );

            await Task.WhenAll(playQueuetask, shuffledPlayQueuetask);

            var playQueueArray = playQueuetask.Result ?? [];
            var shuffledPlayQueueArray = shuffledPlayQueuetask.Result ?? [];

            return (
                new ObservableCollection<IBriefSongInfoBase>(playQueueArray),
                new ObservableCollection<IBriefSongInfoBase>(shuffledPlayQueueArray)
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载播放队列数据错误: {ex.Message}");
            return ([], []);
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
                var guid = Guid.NewGuid().ToString();
                return guid;
            }

            // 只使用文件夹的最后写入时间作为指纹，这是最快的方法
            var fingerprint = dirInfo.LastWriteTime.Ticks.ToString();
            return fingerprint;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"快速计算文件夹指纹错误: {ex.Message}");
            var guid = Guid.NewGuid().ToString();
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
