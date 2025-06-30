using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using The_Untamed_Music_Player.Contracts.Models;
using Windows.Storage;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.Models;

public class FileManager
{
    /// <summary>
    /// 保存音乐库数据到文件
    /// </summary>
    public static async void SaveLibraryDataAsync(
        ObservableCollection<StorageFolder> folders,
        MusicLibraryData data
    )
    {
        var songs = data.Songs;
        var albums = data.Albums;
        var artists = data.Artists;
        var genres = data.Genres;

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
                folderFingerprints[folder.Path] = await CalculateFolderFingerprintAsync(folder);
            }
            await SaveObjectToFileAsync(libraryFolder, "FolderFingerprints", folderFingerprints);

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

            Debug.WriteLine("音乐库数据保存成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存音乐库数据错误: {ex.Message}");
        }
    }

    public static async void SavePlayQueueDataAsync(
        ObservableCollection<IBriefMusicInfoBase> playQueue,
        ObservableCollection<IBriefMusicInfoBase> shuffledPlayQueue
    )
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
        Debug.WriteLine("播放队列数据保存成功");
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
            if (savedFingerprints == null)
            {
                return (true, data);
            }

            // 检查当前文件夹集合是否与保存时相同
            if (folders.Count != savedFingerprints.Count)
            {
                return (true, data);
            }

            // 并行检查每个文件夹的指纹是否变化
            var fingerprintTasks = new List<Task<(bool isMatch, string folderPath)>>();
            foreach (var folder in folders)
            {
                fingerprintTasks.Add(CheckFolderFingerprintAsync(folder, savedFingerprints));
            }

            var fingerprintResults = await Task.WhenAll(fingerprintTasks);
            if (fingerprintResults.Any(result => !result.isMatch))
            {
                return (true, data); // 有文件夹指纹不匹配，需要重新扫描
            }

            // 并行加载所有数据文件
            var songsTask = LoadObjectFromFileAsync<BriefMusicInfo[]>(libraryFolder, "Songs");
            var albumsTask = LoadObjectFromFileAsync<Dictionary<string, AlbumInfo>>(
                libraryFolder,
                "Albums"
            );
            var artistsTask = LoadObjectFromFileAsync<Dictionary<string, ArtistInfo>>(
                libraryFolder,
                "Artists"
            );
            var genresTask = LoadObjectFromFileAsync<string[]>(libraryFolder, "Genres");

            await Task.WhenAll(songsTask, albumsTask, artistsTask, genresTask);

            var songsArray = songsTask.Result;
            var albumsDict = albumsTask.Result;
            var artistsDict = artistsTask.Result;
            var genresArray = genresTask.Result;

            if (
                songsArray == null
                || albumsDict == null
                || artistsDict == null
                || genresArray == null
            )
            {
                return (true, data);
            }

            // 填充数据结构
            data.Songs = [.. songsArray];
            data.Albums = new ConcurrentDictionary<string, AlbumInfo>(albumsDict);
            data.Artists = new ConcurrentDictionary<string, ArtistInfo>(artistsDict);
            data.Genres = [.. genresArray];

            // 并行加载所有专辑封面
            foreach (var album in albumsDict.Values)
            {
                album.LoadCover();
            }

            Debug.WriteLine("音乐库数据加载成功");
            return (false, data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载音乐库数据错误: {ex.Message}");
            return (true, data);
        }
    }

    public static async Task<(
        ObservableCollection<IBriefMusicInfoBase> playQueue,
        ObservableCollection<IBriefMusicInfoBase> shuffledPlayQueue
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

            var playQueuetask = LoadObjectFromFileAsync<IBriefMusicInfoBase[]>(
                playQueueFolder,
                "PlayQueue"
            );
            var shuffledPlayQueuetask = LoadObjectFromFileAsync<IBriefMusicInfoBase[]>(
                playQueueFolder,
                "ShuffledPlayQueue"
            );

            await Task.WhenAll(playQueuetask, shuffledPlayQueuetask);

            var playQueueArray = playQueuetask.Result ?? [];
            var shuffledPlayQueueArray = shuffledPlayQueuetask.Result ?? [];

            return (
                new ObservableCollection<IBriefMusicInfoBase>(playQueueArray),
                new ObservableCollection<IBriefMusicInfoBase>(shuffledPlayQueueArray)
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
        var data = MemoryPack.MemoryPackSerializer.Serialize(obj);
        var file = await folder.CreateFileAsync(
            fileName + ".bin",
            CreationCollisionOption.ReplaceExisting
        );
        await FileIO.WriteBytesAsync(file, data);
    }

    /// <summary>
    /// 从文件加载对象
    /// </summary>
    public static async Task<T?> LoadObjectFromFileAsync<T>(StorageFolder folder, string fileName)
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
            return MemoryPack.MemoryPackSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载对象错误: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 轻量级文件夹变化检测，通过文件计数和时间戳实现快速检查
    /// </summary>
    public static async Task<string> CalculateFolderFingerprintAsync(StorageFolder folder)
    {
        try
        {
            var fileCount = 0;
            var totalSize = 0L;
            var latestModified = 0L;

            // 获取所有音乐文件
            var items = await folder.GetItemsAsync();

            // 计算文件数和最新修改时间
            foreach (var item in items)
            {
                if (
                    item is StorageFile file
                    && Data.SupportedAudioTypes.Contains(file.FileType.ToLower())
                )
                {
                    fileCount++;
                    var props = await file.GetBasicPropertiesAsync();
                    totalSize += (long)props.Size;
                    latestModified = Math.Max(latestModified, props.DateModified.Ticks);
                }
                else if (item is StorageFolder subFolder)
                {
                    // 递归计算子文件夹指纹
                    var subFingerprint = await CalculateFolderFingerprintAsync(subFolder);
                    // 将子文件夹指纹的哈希值加入计算
                    if (long.TryParse(subFingerprint.Split('|')[0], out var subFiles))
                    {
                        fileCount += (int)subFiles;
                    }
                    if (long.TryParse(subFingerprint.Split('|')[1], out var subSize))
                    {
                        totalSize += subSize;
                    }
                    if (long.TryParse(subFingerprint.Split('|')[2], out var subModified))
                    {
                        latestModified = Math.Max(latestModified, subModified);
                    }
                }
            }

            // 返回 "文件数|总大小|最新修改时间" 作为指纹
            return $"{fileCount}|{totalSize}|{latestModified}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"计算文件夹指纹错误: {ex.Message}");
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// 检查文件夹指纹是否匹配
    /// </summary>
    private static async Task<(bool isMatch, string folderPath)> CheckFolderFingerprintAsync(
        StorageFolder folder,
        Dictionary<string, string> savedFingerprints
    )
    {
        if (!savedFingerprints.TryGetValue(folder.Path, out var savedFingerprint))
        {
            return (false, folder.Path); // 找不到保存的指纹，不匹配
        }

        var currentFingerprint = await CalculateFolderFingerprintAsync(folder);
        return (currentFingerprint == savedFingerprint, folder.Path);
    }
}

/// <summary>
/// 音乐库数据容器
/// </summary>
public class MusicLibraryData
{
    public ConcurrentBag<BriefMusicInfo> Songs { get; set; } = null!;
    public ConcurrentDictionary<string, AlbumInfo> Albums { get; set; } = null!;
    public ConcurrentDictionary<string, ArtistInfo> Artists { get; set; } = null!;
    public List<string> Genres { get; set; } = null!;

    public MusicLibraryData() { }

    public MusicLibraryData(
        ConcurrentBag<BriefMusicInfo> songs,
        ConcurrentDictionary<string, AlbumInfo> albums,
        ConcurrentDictionary<string, ArtistInfo> artists,
        List<string> genres
    )
    {
        Songs = songs;
        Albums = albums;
        Artists = artists;
        Genres = genres;
    }
}
