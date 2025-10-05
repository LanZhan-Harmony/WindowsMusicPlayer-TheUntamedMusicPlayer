using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Helpers;
using ZLinq;

namespace UntamedMusicPlayer.Helpers;

public static partial class M3u8Helper
{
    [GeneratedRegex(@"^#SourceMode:(\d+),ID:(\d+)$", RegexOptions.Compiled)]
    private static partial Regex SourceModeRegex();

    // 用于保持顺序的歌曲项
    private record SongItem(int Index, int Type, object Data);

    public static async Task<(string, string?, List<IBriefSongInfoBase>)> GetNameAndSongsFromM3u8(
        string m3u8File
    )
    {
        var m3u8Directory = Path.GetDirectoryName(m3u8File)!;
        var playlistName = Path.GetFileNameWithoutExtension(m3u8File);
        string? coverPath = null;
        var songItems = new List<SongItem>();

        try
        {
            await Task.Run(async () =>
            {
                var currentIndex = 0;
                var lines = await File.ReadAllLinesAsync(m3u8File);
                foreach (var line in lines)
                {
                    try
                    {
                        var trimmedLine = line.Trim();
                        if (
                            string.IsNullOrEmpty(trimmedLine)
                            || trimmedLine.StartsWith("#EXTM3U")
                            || trimmedLine.StartsWith("#EXT")
                        ) // 跳过空行和标准M3U注释行
                        {
                            continue;
                        }

                        if (coverPath is null && TryParseCoverPath(trimmedLine, out var path))
                        {
                            coverPath = path;
                            continue;
                        }

                        if (TryParseSourceMode(trimmedLine, out var mode, out var id)) // 解析SourceMode
                        {
                            if (mode == 2) // 目前仅处理 SourceMode 2
                            {
                                songItems.Add(new SongItem(currentIndex++, 2, id));
                            }
                            continue;
                        }

                        if (TryParseSongPath(trimmedLine, m3u8Directory, out var songItem)) // 解析音频路径/URL
                        {
                            songItems.Add(
                                new SongItem(currentIndex++, songItem.Type, songItem.Data)
                            );
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            });

            var songs = await ProcessSongItemsAsync(songItems);
            return (playlistName, coverPath, songs);
        }
        catch
        {
            return (playlistName, null, []);
        }
    }

    private static bool TryParseCoverPath(string line, out string? coverPath)
    {
        coverPath = null;
        if (line.StartsWith("#CoverPath:"))
        {
            coverPath = line[11..];
            if (File.Exists(coverPath))
            {
                var folder = Path.GetDirectoryName(coverPath)!;
                var extension = Path.GetExtension(coverPath);
                var newFileName = $"{Guid.CreateVersion7()}{extension}";
                var newPath = Path.Combine(folder, newFileName);
                try
                {
                    File.Copy(coverPath, newPath);
                    coverPath = newPath;
                    return true;
                }
                catch
                {
                    coverPath = null;
                    return false;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 解析SourceMode行
    /// </summary>
    private static bool TryParseSourceMode(string line, out short mode, out long id)
    {
        mode = 0;
        id = 0;

        var match = SourceModeRegex().Match(line);
        return match.Success
            && short.TryParse(match.Groups[1].Value, out mode)
            && long.TryParse(match.Groups[2].Value, out id);
    }

    /// <summary>
    /// 解析歌曲路径/URL
    /// </summary>
    private static bool TryParseSongPath(
        string line,
        string m3u8Directory,
        out (int Type, object Data) result
    )
    {
        result = default;

        if (Path.IsPathRooted(line)) // 直接本地路径（绝对路径）
        {
            return ProcessLocalPath(line, out result);
        }

        if (!Uri.TryCreate(line, UriKind.RelativeOrAbsolute, out var uri)) // 尝试解析为URI
        {
            return false;
        }

        if (uri.IsAbsoluteUri)
        {
            if (uri.IsFile) // file:// scheme
            {
                var localPath = Uri.UnescapeDataString(uri.LocalPath); // 处理URL编码
                return ProcessLocalPath(localPath, out result);
            }
            else // URL
            {
                result = (1, uri);
                return true;
            }
        }
        else // 相对路径
        {
            var fullPath = Path.Combine(m3u8Directory, line);
            return ProcessLocalPath(fullPath, out result);
        }
    }

    /// <summary>
    /// 处理本地文件路径
    /// </summary>
    private static bool ProcessLocalPath(string localPath, out (int Type, object Data) result)
    {
        result = default;
        if (!Data.SupportedAudioTypes.Contains(Path.GetExtension(localPath))) // 检查文件扩展名
        {
            return false;
        }
        if (!File.Exists(localPath)) // 检查文件是否存在
        {
            return false;
        }
        result = (0, localPath);
        return true;
    }

    private static async Task<List<IBriefSongInfoBase>> ProcessSongItemsAsync(
        List<SongItem> songItems
    )
    {
        if (songItems.Count == 0)
        {
            return [];
        }

        // 按类型分组
        var type0Items = songItems.Where(x => x.Type == 0).ToArray();
        var type1Items = songItems.Where(x => x.Type == 1).ToArray();
        var type2Items = songItems.Where(x => x.Type == 2).ToArray();

        // 并发处理结果存储
        var results = new ConcurrentDictionary<int, IBriefSongInfoBase>();

        // 创建并行任务
        var tasks = new List<Task>();

        // 处理本地文件 (Type 0)
        if (type0Items.Length > 0)
        {
            tasks.Add(
                Task.Run(() =>
                {
                    Parallel.ForEach(
                        type0Items,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount / 2,
                        },
                        item =>
                        {
                            var fullPath = (string)item.Data;
                            var song = new BriefLocalSongInfo(fullPath, "");
                            if (song.IsPlayAvailable)
                            {
                                results[item.Index] = song;
                            }
                        }
                    );
                })
            );
        }

        // 处理在线URL (Type 1)
        if (type1Items.Length > 0)
        {
            tasks.Add(
                Task.Run(() =>
                {
                    Parallel.ForEach(
                        type1Items,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount / 2,
                        },
                        item =>
                        {
                            var url = (Uri)item.Data;
                            var song = new BriefUnknownSongInfo(url);
                            if (song.IsPlayAvailable)
                            {
                                results[item.Index] = song;
                            }
                        }
                    );
                })
            );
        }

        // 处理云音乐ID (Type 2)
        if (type2Items.Length > 0)
        {
            var cloudIds = type2Items.Select(x => (long)x.Data).ToArray();
            var cloudTask = CloudSongSearchHelper
                .SearchSongsByIDsAsync(cloudIds)
                .ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        var cloudSongs = task.Result;
                        for (var i = 0; i < Math.Min(type2Items.Length, cloudSongs.Count); i++)
                        {
                            results[type2Items[i].Index] = cloudSongs[i];
                        }
                    }
                });
            tasks.Add(cloudTask);
        }

        // 等待所有任务完成
        await Task.WhenAll(tasks);

        // 按原始索引顺序返回结果
        return
        [
            .. songItems
                .AsValueEnumerable()
                .Where(item => results.ContainsKey(item.Index))
                .OrderBy(item => item.Index)
                .Select(item => results[item.Index]),
        ];
    }

    public static async Task ExportPlaylistsToM3u8Async(string folder)
    {
        var playlists = Data.PlaylistLibrary.Playlists;
        await Task.Run(async () =>
        {
            foreach (var playlist in playlists)
            {
                try
                {
                    var m3u8Content = GenerateM3u8Content(playlist);
                    var fileName = $"{playlist.Name}.m3u8";
                    var filePath = GetUniqueFilePath(Path.Combine(folder, fileName));
                    await File.WriteAllTextAsync(filePath, m3u8Content, Encoding.UTF8);
                }
                catch { }
            }
        });
    }

    private static string GenerateM3u8Content(PlaylistInfo playlist)
    {
        var estimatedCapacity = 50 + playlist.Name.Length + (playlist.SongList.Count * 100); // 估算容量
        var sb = new StringBuilder(estimatedCapacity);
        sb.AppendLine("#EXTM3U");
        sb.AppendLine($"#{playlist.Name}");
        if (playlist.IsCoverEdited && playlist.CoverPaths.Count > 0)
        {
            sb.AppendLine($"#CoverPath:{playlist.CoverPaths[0]}");
        }

        foreach (var info in playlist.SongList)
        {
            var song = info.Song;
            var sourceMode = IBriefSongInfoBase.GetSourceMode(song);
            if (sourceMode is 0 or 1)
            {
                sb.AppendLine(song.Path);
            }
            else
            {
                sb.AppendLine($"#SourceMode:{sourceMode},ID:{(song as IBriefOnlineSongInfo)!.ID}");
            }
        }

        return sb.ToString();
    }

    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var count = 1; ; count++)
        {
            var uniquePath = Path.Combine(directory, $"{fileNameWithoutExt}({count}){extension}");
            if (!File.Exists(uniquePath))
            {
                return uniquePath;
            }
        }
    }
}
