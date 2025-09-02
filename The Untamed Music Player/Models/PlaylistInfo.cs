using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using MemoryPack;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using Windows.Graphics.Imaging;
using ZLinq;

namespace The_Untamed_Music_Player.Models;

[MemoryPackable]
public partial class PlaylistInfo
{
    public string Name { get; set; } = null!;
    public string TotalSongNumStr { get; set; } = null!;
    public long ModifiedDate { get; set; }
    public ObservableCollection<IndexedPlaylistSong> SongList { get; set; } = [];
    public bool IsCoverEdited { get; set; } = false;
    public List<string> CoverPaths { get; set; } = new(4);

    [MemoryPackIgnore]
    public WriteableBitmap? Cover { get; set; }

    [MemoryPackConstructor]
    public PlaylistInfo() { }

    public PlaylistInfo(string name, string? coverPath = null)
    {
        Name = name;
        TotalSongNumStr = GetTotalSongNumStr(0);
        IsCoverEdited = coverPath is not null;
        if (IsCoverEdited)
        {
            CoverPaths.Add(coverPath!);
        }
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 仅在创建播放列表时使用
    /// </summary>
    /// <param name="songs"></param>
    /// <returns></returns>
    public async Task AddSongs(List<IBriefSongInfoBase> songs)
    {
        foreach (var song in songs)
        {
            var coverPathIndex = IsCoverEdited ? -1 : await TryAddCoverPath(song);
            var indexedSong = new IndexedPlaylistSong(SongList.Count, song, coverPathIndex);
            SongList.Add(indexedSong);
        }
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        InitializeCover();
        GetCover();
    }

    public PlaylistInfo(string name, PlaylistInfo info)
    {
        Name = name;
        TotalSongNumStr = info.TotalSongNumStr;
        ModifiedDate = info.ModifiedDate;
        SongList = info.SongList;
        IsCoverEdited = info.IsCoverEdited;
        CoverPaths = info.CoverPaths;
        Cover = info.Cover;
    }

    /// <summary>
    /// 重新索引所有歌曲，确保Index对应真实位置
    /// </summary>
    public void ReindexSongs()
    {
        for (var i = 0; i < SongList.Count; i++)
        {
            SongList[i].Index = i;
        }
    }

    /// <summary>
    /// 向播放列表中添加一首歌曲
    /// </summary>
    public async Task Add(IBriefSongInfoBase song)
    {
        var coverPathIndex = IsCoverEdited ? -1 : await TryAddCoverPath(song);
        var indexedSong = new IndexedPlaylistSong(SongList.Count, song, coverPathIndex);
        SongList.Add(indexedSong);
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

        if (coverPathIndex >= 0)
        {
            InitializeCover();
            GetCover();
        }
    }

    /// <summary>
    /// 向播放列表中添加多首歌曲
    /// </summary>
    public async Task AddRange(IEnumerable<IBriefSongInfoBase> songs)
    {
        var coverUpdated = false;
        foreach (var song in songs)
        {
            var coverPathIndex = IsCoverEdited ? -1 : await TryAddCoverPath(song);
            var indexedSong = new IndexedPlaylistSong(SongList.Count, song, coverPathIndex);
            SongList.Add(indexedSong);

            if (coverPathIndex >= 0)
            {
                coverUpdated = true;
            }
        }
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        if (coverUpdated)
        {
            InitializeCover();
            GetCover();
        }
    }

    /// <summary>
    /// 从播放列表中删除一首歌曲
    /// </summary>
    public async Task Delete(IndexedPlaylistSong song)
    {
        if (song.Index < 0 || song.Index >= SongList.Count)
        {
            return;
        }

        var needRefillCover = false;

        if (song.CoverPathIndex >= 0 && song.CoverPathIndex < CoverPaths.Count)
        {
            CoverPaths.RemoveAt(song.CoverPathIndex); // 删除对应的封面路径
            needRefillCover = true;

            // 更新其他歌曲的封面路径索引
            for (var i = 0; i < SongList.Count; i++)
            {
                if (SongList[i].CoverPathIndex > song.CoverPathIndex)
                {
                    SongList[i].CoverPathIndex--;
                }
            }
        }
        SongList.RemoveAt(song.Index);
        ReindexSongs(); // 重新索引以确保Index对应真实位置
        TotalSongNumStr = GetTotalSongNumStr(SongList.Count);
        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        if (needRefillCover)
        {
            if (CoverPaths.Count < 4)
            {
                await RefillCoverPaths();
            }
            InitializeCover();
            GetCover();
        }
    }

    /// <summary>
    /// 将一首歌曲上移
    /// </summary>
    public void MoveUp(IndexedPlaylistSong song)
    {
        var currentPosition = song.Index;
        if (currentPosition <= 0 || currentPosition >= SongList.Count)
        {
            return;
        }

        // 交换位置
        (SongList[currentPosition], SongList[currentPosition - 1]) = (
            SongList[currentPosition - 1],
            SongList[currentPosition]
        );

        // 更新索引
        SongList[currentPosition].Index = currentPosition;
        SongList[currentPosition - 1].Index = currentPosition - 1;

        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 将一首歌曲下移
    /// </summary>
    public void MoveDown(IndexedPlaylistSong song)
    {
        var currentPosition = song.Index;
        if (currentPosition < 0 || currentPosition >= SongList.Count - 1)
        {
            return;
        }

        // 交换位置
        (SongList[currentPosition], SongList[currentPosition + 1]) = (
            SongList[currentPosition + 1],
            SongList[currentPosition]
        );

        // 更新索引
        SongList[currentPosition].Index = currentPosition;
        SongList[currentPosition + 1].Index = currentPosition + 1;

        ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 获取所有歌曲
    /// </summary>
    public IBriefSongInfoBase[] GetAllSongs()
    {
        return [.. SongList.AsValueEnumerable().Select(indexedSong => indexedSong.Song)];
    }

    /// <summary>
    /// 尝试添加封面路径，返回封面路径索引
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    private async Task<int> TryAddCoverPath(IBriefSongInfoBase song)
    {
        if (CoverPaths.Count >= 4)
        {
            return -1;
        }
        var coverPath = await IBriefSongInfoBase.GetCoverPathAsync(song);
        if (!string.IsNullOrWhiteSpace(coverPath))
        {
            CoverPaths.Add(coverPath);
            return CoverPaths.Count - 1;
        }
        return -1;
    }

    /// <summary>
    /// 重新填充封面路径，从剩余歌曲中寻找新的封面
    /// </summary>
    private async Task RefillCoverPaths()
    {
        foreach (var indexedSong in SongList)
        {
            if (CoverPaths.Count >= 4)
            {
                break;
            }

            // 如果这首歌还没有封面路径索引，尝试添加它的封面
            if (indexedSong.CoverPathIndex == -1)
            {
                var coverPath = await IBriefSongInfoBase.GetCoverPathAsync(indexedSong.Song);
                if (!string.IsNullOrWhiteSpace(coverPath))
                {
                    CoverPaths.Add(coverPath);
                    indexedSong.CoverPathIndex = CoverPaths.Count - 1;
                }
            }
        }
    }

    public void InitializeCover()
    {
        if (CoverPaths.Count == 0)
        {
            Cover = null;
        }
        else if (Cover is null)
        {
            const int canvasSize = 256;
            Cover = new WriteableBitmap(canvasSize, canvasSize);
        }
    }

    public async void GetCover()
    {
        if (Cover is null)
        {
            return;
        }
        if (IsCoverEdited) // 用户自定义封面，直接从CoverPaths[0]加载图片
        {
            await Task.Run(async () =>
            {
                try
                {
                    var imagePath = CoverPaths[0];
                    if (!File.Exists(imagePath))
                    {
                        return;
                    }
                    const int canvasSize = 256;

                    // 读取图片文件为字节数组
                    var imageBytes = await File.ReadAllBytesAsync(imagePath);

                    // 使用ResizeImageToFitRegionAsync来正确调整图片尺寸
                    var resizedImageBytes = await ResizeImageToFitRegionAsync(
                        imageBytes,
                        canvasSize,
                        canvasSize
                    );

                    if (resizedImageBytes is null)
                    {
                        return;
                    }

                    // 切换到UI线程更新WriteableBitmap
                    App.MainWindow?.DispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.Low,
                        async () =>
                        {
                            try
                            {
                                // 将像素数据写入WriteableBitmap
                                using var pixelStream = Cover.PixelBuffer.AsStream();
                                await pixelStream.WriteAsync(resizedImageBytes);
                                Cover.Invalidate();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"更新自定义封面失败: {ex.Message}");
                            }
                        }
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载自定义封面失败: {ex.Message}");
                }
            });
            return;
        }

        // 自动组合封面
        await Task.Run(async () =>
        {
            try
            {
                const int canvasSize = 256;
                const int halfSize = canvasSize / 2;

                // 在后台线程中处理所有图像数据
                var processedRegions = new List<(byte[] pixels, int regionIndex)>();

                for (var i = 0; i < CoverPaths.Count; i++)
                {
                    var coverPath = CoverPaths[i];
                    try
                    {
                        byte[]? imageBytes = null;

                        if (coverPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            using var httpClient = new HttpClient();
                            httpClient.Timeout = TimeSpan.FromSeconds(5); // 设置超时
                            imageBytes = await httpClient.GetByteArrayAsync(coverPath);
                        }
                        else if (File.Exists(coverPath))
                        {
                            using var musicFile = TagLib.File.Create(coverPath);
                            if (musicFile.Tag.Pictures.Length > 0)
                            {
                                imageBytes = musicFile.Tag.Pictures[0].Data.Data;
                            }
                        }

                        if (imageBytes is null)
                        {
                            continue;
                        }

                        // 处理图像并立即释放原始数据
                        var resizedImageBytes = await ResizeImageToFitRegionAsync(
                            imageBytes,
                            halfSize,
                            halfSize
                        );

                        // 释放原始图像数据
                        imageBytes = null;

                        if (resizedImageBytes is not null)
                        {
                            processedRegions.Add((resizedImageBytes, i));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"处理封面失败: {coverPath}, 错误: {ex.Message}");
                    }
                }

                // 切换回UI线程进行最终的绘制
                App.MainWindow?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    async () =>
                    {
                        try
                        {
                            var buffer = Cover.PixelBuffer;
                            var pixels = new byte[buffer.Length];

                            // 初始化为透明
                            Array.Fill(pixels, (byte)0);

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
                                    DrawImageToRegion(
                                        pixels,
                                        regionPixels,
                                        regions[regionIndex],
                                        canvasSize
                                    );
                                }
                            }

                            // 将像素数据写入WriteableBitmap
                            using (var pixelStream = buffer.AsStream())
                            {
                                await pixelStream.WriteAsync(pixels);
                            }

                            // 使WriteableBitmap失效以更新显示
                            Cover.Invalidate();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"UI线程绘制失败: {ex.Message}");
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建WriteableBitmap失败: {ex.Message}");
            }
        });
    }

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
            Debug.WriteLine($"调整图片大小失败: {ex.Message}");
            return null;
        }
    }

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
            Debug.WriteLine($"绘制图片到区域失败: {ex.Message}");
        }
    }

    public void ClearCover()
    {
        if (
            CoverPaths.Count > 0
            && Data.SupportedCoverTypes.Contains(Path.GetExtension(CoverPaths[0]).ToLower())
        )
        {
            try
            {
                File.Delete(CoverPaths[0]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除封面图片失败: {ex.Message}");
            }
        }
        CoverPaths.Clear();
        IsCoverEdited = true;
        Cover = null;
        SongList.AsValueEnumerable().Select(i => i.CoverPathIndex = -1);
    }

    private static string GetTotalSongNumStr(int totalSongNum)
    {
        return totalSongNum == 1
            ? $"{totalSongNum} {"PlaylistInfo_Item".GetLocalized()}"
            : $"{totalSongNum} {"PlaylistInfo_Items".GetLocalized()}";
    }
}

public record PictureRegion(int X, int Y, int Width, int Height);

[MemoryPackable]
public partial class IndexedPlaylistSong
{
    /// <summary>
    /// 在播放列表中的位置索引 (0-based)
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 歌曲信息
    /// </summary>
    public IBriefSongInfoBase Song { get; set; } = null!;

    /// <summary>
    /// 封面路径索引，用于追踪该歌曲在CoverPaths中的位置 (-1表示没有封面)
    /// </summary>
    public int CoverPathIndex { get; set; } = -1;

    [MemoryPackConstructor]
    public IndexedPlaylistSong() { }

    public IndexedPlaylistSong(int index, IBriefSongInfoBase song, int coverPathIndex = -1)
    {
        Index = index;
        Song = song;
        CoverPathIndex = coverPathIndex;
    }
}
