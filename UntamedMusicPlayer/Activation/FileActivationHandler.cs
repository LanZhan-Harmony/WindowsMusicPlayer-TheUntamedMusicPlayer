using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using UntamedMusicPlayer.Models;
using Windows.Storage;

namespace UntamedMusicPlayer.Activation;

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

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // 设置文件激活标志
        Data.IsFileActivationLaunch = true;

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
                        if (songInfo.IsPlayAvailable)
                        {
                            musicFiles.Add(songInfo);
                        }
                    }
                }
            });

            if (musicFiles.Count > 0)
            {
                PlayMusicFiles(musicFiles);
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
        while ((Data.MusicPlayer is null || !Data.MusicPlayer.HasLoaded) && DateTime.Now < timeout)
        {
            await Task.Delay(100);
        }

        // 再等待一小段时间确保UI完全加载
        await Task.Delay(500);
    }

    /// <summary>
    /// 播放音乐文件
    /// </summary>
    private static void PlayMusicFiles(List<BriefLocalSongInfo> musicFiles)
    {
        Data.MusicPlayer.SetPlayQueue("LocalSongs:Part", musicFiles);
        Data.MusicPlayer.PlaySongByInfo(musicFiles[0]);
        Data.RootPlayBarViewModel?.DetailModeUpdate();
    }
}
