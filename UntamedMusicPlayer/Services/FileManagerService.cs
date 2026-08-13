using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using MemoryPack;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using Windows.Storage;
using ZLogger;

namespace UntamedMusicPlayer.Services;

public static class FileManager
{
    private static readonly ILogger _logger = LoggingService.CreateLogger(nameof(FileManager));

    /// <summary>
    /// 保存音乐库数据到文件
    /// </summary>
    public static void SaveLibraryDataAsync(
        ObservableCollection<string> folders,
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
                var libraryFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    "LibraryData",
                    CreationCollisionOption.OpenIfExists
                );

                // 计算并保存文件夹指纹
                var folderFingerprints = new Dictionary<string, string>();
                foreach (var folder in folders)
                {
                    folderFingerprints[folder] = GetFolderFingerprintFast(folder);
                }

                var fingerprintTask = SaveObjectToFileAsync(
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
                    fingerprintTask,
                    songsTask,
                    albumsTask,
                    artistsTask,
                    genresTask,
                    musicFoldersTask
                );
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"保存音乐库数据错误");
            }
        });
    }

    /// <summary>
    /// 保存播放队列数据到文件
    /// </summary>
    /// <param name="normalPlayQueue"></param>
    /// <param name="shuffledPlayQueue"></param>
    public static async Task SavePlayQueueDataAsync(
        ObservableCollection<IndexedPlayQueueSong> normalPlayQueue,
        ObservableCollection<IndexedPlayQueueSong> shuffledPlayQueue
    )
    {
        await Task.Run(async () =>
        {
            try
            {
                var playQueueFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    "PlayQueueData",
                    CreationCollisionOption.OpenIfExists
                );

                var normalQueueTask = SaveObjectToFileAsync(
                    playQueueFolder,
                    "NormalPlayQueue",
                    normalPlayQueue
                ); // 保存播放队列
                var shuffledQueueTask = SaveObjectToFileAsync(
                    playQueueFolder,
                    "ShuffledPlayQueue",
                    shuffledPlayQueue
                ); // 保存随机播放队列
                await Task.WhenAll(normalQueueTask, shuffledQueueTask);
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"保存播放队列数据错误");
            }
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
                var playlistFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    "PlaylistData",
                    CreationCollisionOption.OpenIfExists
                );

                await SaveObjectToFileAsync(playlistFolder, "Playlists", playlists); // 保存播放列表
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"保存播放列表数据错误");
            }
        });
    }

    public static async Task SavePlaylistDataToM3u8Async()
    {
        await Task.Run(async () =>
        {
            try
            {
                var playlistFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    "PlaylistM3u8Data",
                    CreationCollisionOption.OpenIfExists
                );
                var files = await playlistFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    try
                    {
                        await file.DeleteAsync();
                    }
                    catch { }
                }
                await M3u8Helper.ExportPlaylistsToM3u8Async(playlistFolder.Path);
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"保存播放列表数据至M3U8文件错误");
            }
        });
    }

    /// <summary>
    /// 从文件加载音乐库数据
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    public static async Task<(bool needRescan, MusicLibraryData data)> LoadLibraryDataAsync(
        ObservableCollection<string> folders
    )
    {
        var data = new MusicLibraryData();

        try
        {
            // 尝试打开音乐库数据目录
            StorageFolder libraryFolder;
            try
            {
                libraryFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(
                    "LibraryData"
                );
            }
            catch // 文件夹不存在，需要重新扫描
            {
                return (true, data);
            }

            // 读取保存的文件夹指纹
            // NativeAOT 修复: 先加载原始字节，再在具体调用点反序列化
            var fingerprintBytes = await LoadBytesFromFileAsync(
                libraryFolder,
                "FolderFingerprints"
            );
            var savedFingerprints = fingerprintBytes is null
                ? null
                : MemoryPackSerializer.Deserialize<Dictionary<string, string>>(fingerprintBytes);
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
                if (!savedFingerprints.TryGetValue(folder, out var savedFingerprint))
                {
                    return (true, data); // 找不到保存的指纹，需要重新扫描
                }

                var currentFingerprint = GetFolderFingerprintFast(folder);
                if (currentFingerprint != savedFingerprint)
                {
                    return (true, data); // 指纹不匹配，需要重新扫描
                }
            }

            // 并行加载所有数据文件
            var songsTask = LoadBytesFromFileAsync(libraryFolder, "Songs");
            var albumsTask = LoadBytesFromFileAsync(libraryFolder, "Albums");
            var artistsTask = LoadBytesFromFileAsync(libraryFolder, "Artists");
            var genresTask = LoadBytesFromFileAsync(libraryFolder, "Genres");
            var musicFoldersTask = LoadBytesFromFileAsync(libraryFolder, "MusicFolders");

            await Task.WhenAll(songsTask, albumsTask, artistsTask, genresTask, musicFoldersTask);

            var songsBytes = songsTask.Result;
            var albumsBytes = albumsTask.Result;
            var artistsBytes = artistsTask.Result;
            var genresBytes = genresTask.Result;
            var musicFoldersBytes = musicFoldersTask.Result;

            if (
                songsBytes is null
                || albumsBytes is null
                || artistsBytes is null
                || genresBytes is null
                || musicFoldersBytes is null
            )
            {
                return (true, data);
            }

            // NativeAOT 修复: 在具体调用点反序列化
            var songsList = MemoryPackSerializer.Deserialize<ConcurrentBag<BriefLocalSongInfo>>(
                songsBytes
            );
            var albumsDict = MemoryPackSerializer.Deserialize<
                ConcurrentDictionary<string, LocalAlbumInfo>
            >(albumsBytes);
            var artistsDict = MemoryPackSerializer.Deserialize<
                ConcurrentDictionary<string, LocalArtistInfo>
            >(artistsBytes);
            var genresList = MemoryPackSerializer.Deserialize<List<string>>(genresBytes);
            var musicFoldersDict = MemoryPackSerializer.Deserialize<
                ConcurrentDictionary<string, byte>
            >(musicFoldersBytes);

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
            _logger.ZLogInformation(ex, $"加载音乐库数据错误");
            return (true, data);
        }
    }

    /// <summary>
    /// 从文件加载播放队列数据
    /// </summary>
    /// <returns></returns>
    public static async Task<(
        ObservableCollection<IndexedPlayQueueSong> normalPlayQueue,
        ObservableCollection<IndexedPlayQueueSong> shuffledPlayQueue
    )> LoadPlayQueueDataAsync()
    {
        try
        {
            StorageFolder playQueueFolder;
            try
            {
                playQueueFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(
                    "PlayQueueData"
                );
            }
            catch
            {
                return ([], []);
            }

            var normalBytesTask = LoadBytesFromFileAsync(playQueueFolder, "NormalPlayQueue");
            var shuffledBytesTask = LoadBytesFromFileAsync(playQueueFolder, "ShuffledPlayQueue");
            await Task.WhenAll(normalBytesTask, shuffledBytesTask);

            var normalBytes = normalBytesTask.Result;
            var shuffledBytes = shuffledBytesTask.Result;

            var normalPlayQueueList = normalBytes is null
                ? []
                : MemoryPackSerializer.Deserialize<ObservableCollection<IndexedPlayQueueSong>>(
                    normalBytes
                ) ?? [];
            var shuffledPlayQueueList = shuffledBytes is null
                ? []
                : MemoryPackSerializer.Deserialize<ObservableCollection<IndexedPlayQueueSong>>(
                    shuffledBytes
                ) ?? [];

            return (normalPlayQueueList, shuffledPlayQueueList);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"加载播放队列数据错误");
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
            StorageFolder playlistFolder;
            try
            {
                playlistFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(
                    "PlaylistData"
                );
            }
            catch
            {
                return await LoadPlaylistDataFromM3u8Async();
            }

            var playlistBytes = await LoadBytesFromFileAsync(playlistFolder, "Playlists");
            if (playlistBytes is null)
            {
                return await LoadPlaylistDataFromM3u8Async();
            }
            var playlists = MemoryPackSerializer.Deserialize<List<PlaylistInfo>>(playlistBytes);
            if (playlists is null)
            {
                return await LoadPlaylistDataFromM3u8Async();
            }
            return playlists;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"加载播放列表数据错误");
            return await LoadPlaylistDataFromM3u8Async();
        }
    }

    public static async Task<List<PlaylistInfo>> LoadPlaylistDataFromM3u8Async()
    {
        try
        {
            StorageFolder playlistFolder;
            try
            {
                playlistFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(
                    "PlaylistM3u8Data"
                );
            }
            catch
            {
                return [];
            }

            var files = await playlistFolder.GetFilesAsync();
            var playlists = new List<PlaylistInfo>();
            foreach (var file in files)
            {
                var (name, cover, songs) = await M3u8Helper.GetNameAndSongsFromM3u8(file.Path);
                var playlist = new PlaylistInfo(name, cover);
                await playlist.AddRange(songs);
                playlists.Add(playlist);
            }
            return playlists;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"从M3U8文件加载播放列表数据错误");
            return [];
        }
    }

    public static async Task<List<PlaylistInfo>> LoadPlaylistDataFromBinAsync(string file)
    {
        try
        {
            var data = await File.ReadAllBytesAsync(file);
            return MemoryPackSerializer.Deserialize<List<PlaylistInfo>>(data) ?? [];
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"加载播放列表数据错误");
            return [];
        }
    }

    /// <summary>
    /// 保存已序列化的字节数据到文件
    /// </summary>
    /// <remarks>
    /// NativeAOT 兼容: 调用方应在具体调用点使用确切类型调用 MemoryPackSerializer.Serialize，
    /// 而不是在泛型方法内部调用，以避免 GVM 分派链缺失导致的崩溃。
    /// </remarks>
    public static async Task SaveObjectToFileAsync<T>(
        StorageFolder folder,
        string fileName,
        T? value,
        int initialCapacity = 8192
    )
    {
        try
        {
            var filePath = Path.Combine(folder.Path, fileName + ".bin");
            await using var stream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true
            );
            await MemoryPackAotSerializer.SerializeToStreamAsync(stream, value, initialCapacity);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"保存对象错误");
        }
    }

    /// <summary>
    /// 从文件加载原始字节数据
    /// </summary>
    /// <remarks>
    /// NativeAOT 兼容: 调用方应在具体调用点使用确切类型调用 MemoryPackSerializer.Deserialize，
    /// 而不是在泛型方法内部调用，以避免 GVM 分派链缺失导致的崩溃。
    /// </remarks>
    public static async Task<byte[]?> LoadBytesFromFileAsync(StorageFolder folder, string fileName)
    {
        try
        {
            var filePath = Path.Combine(folder.Path, fileName + ".bin");
            if (!File.Exists(filePath))
            {
                return null;
            }
            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"加载对象错误");
            return null;
        }
    }

    /// <summary>
    /// 超快速文件夹变化检测，使用递归目录信息作为指纹
    /// </summary>
    private static string GetFolderFingerprintFast(string folder)
    {
        try
        {
            var dirInfo = new DirectoryInfo(folder);
            if (!dirInfo.Exists)
            {
                return Guid.CreateVersion7().ToString();
            }

            // 初始累加根目录的 Ticks
            var totalTicks = dirInfo.LastWriteTime.Ticks;
            var dirCount = 0;

            // 递归遍历所有子目录以检测深层变化
            var subDirs = dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories);
            foreach (var subDir in subDirs)
            {
                totalTicks += subDir.LastWriteTime.Ticks;
                dirCount++;
            }

            var fingerprint =
                $"{totalTicks}-{dirCount}-{CultureInfo.CurrentUICulture.Name.ToLowerInvariant()}";
            return fingerprint;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"快速计算文件夹指纹失败");
            return Guid.CreateVersion7().ToString();
        }
    }
}

/// <summary>
/// 音乐库数据容器
/// </summary>
public sealed class MusicLibraryData
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
