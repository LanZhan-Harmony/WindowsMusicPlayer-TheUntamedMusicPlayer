using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using The_Untamed_Music_Player.Models;
using Windows.Storage;

namespace The_Untamed_Music_Player.Activation;

/// <summary>
/// 处理通过文件关联启动应用程序的激活器
/// </summary>
public class FileActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // 检查是否是通过文件激活
        return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind
            == ExtendedActivationKind.File;
    }

    protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // 获取文件激活参数
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (
            activatedArgs?.Data
                is Windows.ApplicationModel.Activation.IFileActivatedEventArgs fileArgs
            && fileArgs.Files.Count > 0
        )
        {
            // 等待主窗口和数据初始化完成
            await WaitForInitializationAsync();

            // 处理传入的音频文件
            var musicFiles = new List<BriefLocalSongInfo>();

            await Task.Run(() =>
            {
                foreach (var file in fileArgs.Files.OfType<StorageFile>())
                {
                    if (Data.SupportedAudioTypes.Contains(file.FileType.ToLowerInvariant()))
                    {
                        var songInfo = new BriefLocalSongInfo(
                            file.Path,
                            Path.GetDirectoryName(file.Path) ?? ""
                        );
                        musicFiles.Add(songInfo);
                    }
                }
            });

            if (musicFiles.Count > 0)
            {
                await PlayMusicFiles(musicFiles);
            }
        }
    }

    /// <summary>
    /// 等待应用程序初始化完成
    /// </summary>
    private static async Task WaitForInitializationAsync()
    {
        // 等待数据对象初始化
        var timeout = DateTime.Now.AddSeconds(10); // 10秒超时
        while (Data.MusicPlayer is null && DateTime.Now < timeout)
        {
            await Task.Delay(100);
        }

        // 再等待一小段时间确保UI完全加载
        await Task.Delay(500);
    }

    /// <summary>
    /// 播放音乐文件
    /// </summary>
    private static async Task PlayMusicFiles(List<BriefLocalSongInfo> musicFiles)
    {
        try
        {
            if (Data.MusicPlayer is null)
            {
                return;
            }
            Data.MusicPlayer.Reset();
            await Task.Delay(200);
            Data.MusicPlayer.SetPlayQueue("LocalSongs:Part", musicFiles);
            Data.MusicPlayer.PlaySongByInfo(musicFiles[0]);
            Data.RootPlayBarViewModel?.DetailModeUpdate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"播放文件时出错: {ex.Message}");
        }
    }
}
