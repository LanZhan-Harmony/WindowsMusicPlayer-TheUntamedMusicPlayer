using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.Constants;
using UntamedMusicPlayer.Core.Models;
using Windows.Storage;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Core.Services;

/// <summary>
/// Recursively scans folders and builds the playable-song snapshot used by the local library.
/// </summary>
public sealed class LocalMusicLibraryScanner
{
    private readonly ILogger _logger = CoreLoggingService.CreateLogger<LocalMusicLibraryScanner>();

    public async Task ScanAsync(
        StorageFolder folder,
        string folderName,
        ConcurrentDictionary<string, byte> musicFolders,
        ConcurrentBag<BriefLocalSongInfo> songs
    )
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(musicFolders);
        ArgumentNullException.ThrowIfNull(songs);

        try
        {
            var entries = await folder.GetItemsAsync();
            var childTasks = new List<Task>();

            foreach (var subFolder in entries.OfType<StorageFolder>())
            {
                if (musicFolders.TryAdd(subFolder.Path, 0))
                {
                    var childFolderName = $"{folderName}/{subFolder.DisplayName}";
                    childTasks.Add(ScanAsync(subFolder, childFolderName, musicFolders, songs));
                }
            }

            var supportedFiles = entries
                .AsValueEnumerable()
                .OfType<StorageFile>()
                .Where(file => AppConstants.SupportedAudioTypes.Contains(file.FileType.ToLower()));

            foreach (var file in supportedFiles)
            {
                var song = new BriefLocalSongInfo(file.Path, folderName);
                if (song.IsPlayAvailable)
                {
                    songs.Add(song);
                }
            }

            await Task.WhenAll(childTasks);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"加载音乐文件失败: {folder.Path}");
        }
    }
}
