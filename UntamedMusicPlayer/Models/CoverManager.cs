using System.Collections.Concurrent;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using UntamedMusicPlayer.Services;
using Windows.Graphics.Imaging;
using ZLogger;

namespace UntamedMusicPlayer.Models;

public static class CoverManager
{
    private static readonly ILogger _logger = LoggingService.CreateLogger(nameof(CoverManager));
    private static readonly ConcurrentDictionary<string, WeakReference<BitmapImage>> _songCovers =
        new();
    private static readonly ConcurrentDictionary<
        string,
        WeakReference<WriteableBitmap>
    > _playlistCovers = new();
    private static readonly Lock _albumLock = new();
    private static readonly Lock _playlistLock = new();
    private static int _accessCount;

    // 定时清理（每 5 分钟）
    private static readonly Timer _cleanupTimer = new(
        _ => CleanupDeadReferences(),
        null,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(5)
    );

    /// <summary>
    /// 获取专辑封面
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
    public static BitmapImage? GetAlbumCoverBitmap(LocalAlbumInfo album)
    {
        // 自动触发清理机制
        if (Interlocked.Increment(ref _accessCount) % 50 == 0)
        {
            Task.Run(CleanupDeadReferences);
        }

        var path = album.CoverPath;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        // 双重检查锁定预检
        if (
            _songCovers.TryGetValue(path, out var weakRef)
            && weakRef.TryGetTarget(out var cachedCover)
        )
        {
            return cachedCover;
        }

        lock (_albumLock)
        {
            if (_songCovers.TryGetValue(path, out weakRef) && weakRef.TryGetTarget(out cachedCover))
            {
                return cachedCover;
            }

            var picture = GetSongCoverPicture(path);
            if (picture?.Data.Data is not { Length: > 0 } data)
            {
                return null;
            }

            try
            {
                var bitmapImage = new BitmapImage { DecodePixelWidth = 160 };
                using var stream = new MemoryStream(data);
                _ = bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                _songCovers[path] = new WeakReference<BitmapImage>(bitmapImage);
                return bitmapImage;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"渲染封面失败: {path}");
                return null;
            }
        }
    }

    /// <summary>
    /// 获取专辑封面原始字节
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
    public static byte[] GetAlbumCoverBytes(LocalAlbumInfo album) =>
        GetSongCoverPicture(album.CoverPath)?.Data.Data ?? [];

    /// <summary>
    /// 从专辑歌曲中提取封面原始字节
    /// </summary>
    /// <param name="coverPath"></param>
    /// <returns></returns>
    public static TagLib.IPicture? GetSongCoverPicture(string? coverPath)
    {
        if (string.IsNullOrEmpty(coverPath))
        {
            return null;
        }
        try
        {
            using var musicFile = TagLib.File.Create(coverPath, TagLib.ReadStyle.PictureLazy);
            var pictures = musicFile.Tag.Pictures;
            if (pictures.Length == 0)
            {
                return null;
            }

            // 优先选取 封面 或 核心媒体图，否则按顺序选取第一张非空图片
            var picture =
                pictures.FirstOrDefault(p =>
                    p.Type is TagLib.PictureType.FrontCover or TagLib.PictureType.Media
                ) ?? pictures.FirstOrDefault(p => p.Type != TagLib.PictureType.NotAPicture);

            if (picture is null)
            {
                return null;
            }

            // 处理延迟加载
            if (picture.Data.IsEmpty && picture is TagLib.ILazy { IsLoaded: false } lazy)
            {
                lazy.Load();
            }

            return !picture.Data.IsEmpty ? picture : null;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"提取歌曲 {coverPath} 封面数据失败");
            return null;
        }
    }

    /// <summary>
    /// 获取播放列表封面
    /// </summary>
    /// <param name="playlist"></param>
    /// <returns></returns>
    public static WriteableBitmap? GetPlaylistCoverBitmap(PlaylistInfo playlist)
    {
        // 触发定期清理
        if (Interlocked.Increment(ref _accessCount) % 50 == 0)
        {
            Task.Run(CleanupDeadReferences);
        }

        var name = playlist.Name;
        var coverPaths = playlist.CoverPaths;

        if (coverPaths.Count == 0)
        {
            return null;
        }

        if (
            _playlistCovers.TryGetValue(name, out var weakRef)
            && weakRef.TryGetTarget(out var cachedCover)
        )
        {
            return cachedCover;
        }

        lock (_playlistLock)
        {
            if (
                _playlistCovers.TryGetValue(name, out weakRef)
                && weakRef.TryGetTarget(out cachedCover)
            )
            {
                return cachedCover;
            }

            var cover = new WriteableBitmap(256, 256);
            if (playlist.IsCoverEdited) // 用户自定义封面，直接从CoverPaths[0]加载图片
            {
                _ = LoadCustomCoverAsync(cover, coverPaths[0], name);
            }
            else // 自动组合封面
            {
                _ = LoadCombinedCoverAsync(cover, coverPaths, name);
            }
            _playlistCovers[name] = new WeakReference<WriteableBitmap>(cover);
            return cover;
        }
    }

    /// <summary>
    /// 加载用户自定义封面
    /// </summary>
    /// <param name="cover"></param>
    /// <param name="imagePath"></param>
    /// <param name="playlistName"></param>
    /// <returns></returns>
    private static async Task LoadCustomCoverAsync(
        WriteableBitmap cover,
        string imagePath,
        string playlistName
    )
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return;
            }

            const int canvasSize = 256;

            // 在后台线程读取和调整图片大小
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var resizedImageBytes = await ResizeImageToFitRegionAsync(
                imageBytes,
                canvasSize,
                canvasSize
            );

            if (resizedImageBytes is null)
            {
                return;
            }

            // 写入像素数据到 WriteableBitmap
            using var pixelStream = cover.PixelBuffer.AsStream();
            await pixelStream.WriteAsync(resizedImageBytes);
            cover.Invalidate();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"播放列表{playlistName}加载自定义封面失败");
        }
    }

    /// <summary>
    /// 加载组合封面（最多4张图片）
    /// </summary>
    /// <param name="cover"></param>
    /// <param name="coverPaths"></param>
    /// <param name="playlistName"></param>
    /// <returns></returns>
    private static async Task LoadCombinedCoverAsync(
        WriteableBitmap cover,
        List<string> coverPaths,
        string playlistName
    )
    {
        try
        {
            const int canvasSize = 256;
            const int halfSize = canvasSize / 2;

            // 在后台线程处理所有图像
            var processedRegions = await Task.Run(async () =>
            {
                var regions = new List<(byte[] pixels, int regionIndex)>();

                for (var i = 0; i < Math.Min(coverPaths.Count, 4); i++)
                {
                    var coverPath = coverPaths[i];
                    try
                    {
                        var imageBytes = await LoadImageBytesAsync(coverPath);
                        if (imageBytes is null)
                        {
                            continue;
                        }

                        var resizedImageBytes = await ResizeImageToFitRegionAsync(
                            imageBytes,
                            halfSize,
                            halfSize
                        );

                        if (resizedImageBytes is not null)
                        {
                            regions.Add((resizedImageBytes, i));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogInformation(
                            ex,
                            $"播放列表{playlistName}处理封面{coverPath}失败"
                        );
                    }
                }

                return regions;
            });

            // 在 UI 线程绘制
            var buffer = cover.PixelBuffer;
            var pixels = new byte[buffer.Length];
            Array.Fill(pixels, (byte)0); // 初始化为透明

            // 定义四个区域的位置
            var regions = new[]
            {
                new PictureRegion(0, 0, halfSize, halfSize), // 左上
                new PictureRegion(halfSize, 0, halfSize, halfSize), // 右上
                new PictureRegion(0, halfSize, halfSize, halfSize), // 左下
                new PictureRegion(halfSize, halfSize, halfSize, halfSize), // 右下
            };

            // 绘制所有处理好的区域
            foreach (var (regionPixels, regionIndex) in processedRegions)
            {
                if (regionIndex < regions.Length)
                {
                    DrawImageToRegion(pixels, regionPixels, regions[regionIndex], canvasSize);
                }
            }

            // 将像素数据写入 WriteableBitmap
            using (var pixelStream = buffer.AsStream())
            {
                await pixelStream.WriteAsync(pixels);
            }

            cover.Invalidate();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"播放列表{playlistName}创建组合封面失败");
        }
    }

    /// <summary>
    /// 从路径或 URL 加载图片字节数据
    /// </summary>
    /// <param name="coverPath"></param>
    /// <returns></returns>
    private static async Task<byte[]?> LoadImageBytesAsync(string coverPath)
    {
        if (coverPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            return await httpClient.GetByteArrayAsync(coverPath);
        }

        if (File.Exists(coverPath))
        {
            using var musicFile = TagLib.File.Create(coverPath);
            if (musicFile.Tag.Pictures.Length > 0)
            {
                return musicFile.Tag.Pictures[0].Data.Data;
            }
        }

        return null;
    }

    /// <summary>
    /// 将图片调整为适应指定区域大小（UniformToFill）
    /// </summary>
    /// <param name="imageBytes"></param>
    /// <param name="targetWidth"></param>
    /// <param name="targetHeight"></param>
    /// <returns></returns>
    public static async Task<byte[]?> ResizeImageToFitRegionAsync(
        byte[] imageBytes,
        int targetWidth,
        int targetHeight
    )
    {
        try
        {
            using var inputStream = new MemoryStream(imageBytes);
            var decoder = await BitmapDecoder.CreateAsync(inputStream.AsRandomAccessStream());

            var originalWidth = decoder.PixelWidth;
            var originalHeight = decoder.PixelHeight;

            // 计算UniformToFill的缩放和裁剪参数
            var scaleX = (double)targetWidth / originalWidth;
            var scaleY = (double)targetHeight / originalHeight;
            var scale = Math.Max(scaleX, scaleY); // 使用较大的缩放比例以填充整个区域

            var scaledWidth = (uint)(originalWidth * scale);
            var scaledHeight = (uint)(originalHeight * scale);

            // 计算居中裁剪的起始位置
            var cropX = (uint)Math.Max(0, (scaledWidth - targetWidth) / 2);
            var cropY = (uint)Math.Max(0, (scaledHeight - targetHeight) / 2);

            // 创建变换
            var transform = new BitmapTransform
            {
                ScaledWidth = scaledWidth,
                ScaledHeight = scaledHeight,
                Bounds = new BitmapBounds
                {
                    X = cropX,
                    Y = cropY,
                    Width = (uint)targetWidth,
                    Height = (uint)targetHeight,
                },
            };

            var pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage
            );

            return pixelData.DetachPixelData();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"调整图片大小失败");
            return null;
        }
    }

    /// <summary>
    /// 将图片像素绘制到指定区域
    /// </summary>
    /// <param name="canvasPixels"></param>
    /// <param name="imagePixels"></param>
    /// <param name="region"></param>
    /// <param name="canvasWidth"></param>
    private static void DrawImageToRegion(
        byte[] canvasPixels,
        byte[] imagePixels,
        PictureRegion region,
        int canvasWidth
    )
    {
        try
        {
            var regionWidth = region.Width;
            var regionHeight = region.Height;
            var regionX = region.X;
            var regionY = region.Y;

            for (var y = 0; y < regionHeight; y++)
            {
                for (var x = 0; x < regionWidth; x++)
                {
                    var imageIndex = (y * regionWidth + x) * 4; // BGRA
                    var canvasIndex = ((regionY + y) * canvasWidth + (regionX + x)) * 4;

                    if (
                        imageIndex < imagePixels.Length - 3
                        && canvasIndex < canvasPixels.Length - 3
                    )
                    {
                        canvasPixels[canvasIndex] = imagePixels[imageIndex]; // B
                        canvasPixels[canvasIndex + 1] = imagePixels[imageIndex + 1]; // G
                        canvasPixels[canvasIndex + 2] = imagePixels[imageIndex + 2]; // R
                        canvasPixels[canvasIndex + 3] = imagePixels[imageIndex + 3]; // A
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"绘制图片到区域失败");
        }
    }

    public static void ForceAlbumCoverRefresh(BriefLocalSongInfo info)
    {
        _songCovers.TryRemove(info.Path, out _);
    }

    public static void ForcePlaylistCoverRefresh(PlaylistInfo info)
    {
        _playlistCovers.TryRemove(info.Name, out _);
    }

    public static void ForceAllSongCoversRefresh()
    {
        _songCovers.Clear();
    }

    public static void ForceAllPlaylistCoversRefresh()
    {
        _playlistCovers.Clear();
    }

    /// <summary>
    /// 清理已被回收的图片引用，防止字典无限增长
    /// </summary>
    public static void CleanupDeadReferences()
    {
        foreach (var (key, weakRef) in _songCovers)
        {
            if (!weakRef.TryGetTarget(out _))
            {
                _songCovers.TryRemove(key, out _);
            }
        }
        foreach (var (key, weakRef) in _playlistCovers)
        {
            if (!weakRef.TryGetTarget(out _))
            {
                _playlistCovers.TryRemove(key, out _);
            }
        }
    }
}

public sealed record PictureRegion(int X, int Y, int Width, int Height);
