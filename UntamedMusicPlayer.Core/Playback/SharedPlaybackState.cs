using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Contracts.Services;
using Windows.Media.Playback;

namespace UntamedMusicPlayer.Core.Playback;

public sealed partial class SharedPlaybackState : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;

    public SharedPlaybackState(ILocalSettingsService localSettingsService)
    {
        _localSettingsService =
            localSettingsService ?? throw new ArgumentNullException(nameof(localSettingsService));
    }

    [ObservableProperty]
    public partial ShuffleState ShuffleMode { get; set; } = ShuffleState.Normal;

    [ObservableProperty]
    public partial RepeatState RepeatMode { get; set; } = RepeatState.NoRepeat;

    [ObservableProperty]
    public partial MediaPlaybackState PlayState { get; set; } = MediaPlaybackState.Paused;

    [ObservableProperty]
    public partial double Volume { get; set; } = 100;

    [ObservableProperty]
    public partial bool IsMute { get; set; } = false;

    [ObservableProperty]
    public partial double Speed { get; set; } = 1.0;

    [ObservableProperty]
    public partial TimeSpan CurrentPlayingTime { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial TimeSpan TotalPlayingTime { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial int PlayQueueIndex { get; set; } = -1;

    public int PlayQueueCount { get; set; } = 0;

    [ObservableProperty]
    public partial IBriefSongInfoBase? CurrentBriefSong { get; set; }

    [ObservableProperty]
    public partial IDetailedSongInfoBase? CurrentSong { get; set; }

    public bool IsExclusiveMode { get; set; } = false;

    public async Task LoadStateAsync()
    {
        try
        {
            var shuffleModeName = await _localSettingsService.ReadSettingAsync<string>(
                nameof(ShuffleMode)
            );
            ShuffleMode = Enum.TryParse<ShuffleState>(shuffleModeName, out var cacheShuffleMode)
                ? cacheShuffleMode
                : ShuffleState.Normal;
            var repeatModeName = await _localSettingsService.ReadSettingAsync<string>(
                nameof(RepeatMode)
            );
            RepeatMode = Enum.TryParse<RepeatState>(repeatModeName, out var cacheRepeatMode)
                ? cacheRepeatMode
                : RepeatState.NoRepeat;
            IsMute = await _localSettingsService.ReadSettingAsync<bool>(nameof(IsMute));
            IsExclusiveMode = await _localSettingsService.ReadSettingAsync<bool>(
                nameof(IsExclusiveMode)
            );
            var volume = await _localSettingsService.ReadSettingAsync<double>(nameof(Volume));
            Volume = volume > 0 ? volume : 100;
            var speed = await _localSettingsService.ReadSettingAsync<double>(nameof(Speed));
            Speed = speed > 0 ? speed : 1.0;
        }
        catch { }
    }

    public async Task SaveStateAsync()
    {
        try
        {
            await _localSettingsService.SaveSettingAsync(nameof(ShuffleMode), $"{ShuffleMode}");
            await _localSettingsService.SaveSettingAsync(nameof(RepeatMode), $"{RepeatMode}");
            await _localSettingsService.SaveSettingAsync(nameof(Volume), Volume);
            await _localSettingsService.SaveSettingAsync(nameof(IsMute), IsMute);
            await _localSettingsService.SaveSettingAsync(nameof(Speed), Speed);
        }
        catch { }
    }
}

public enum ShuffleState
{
    /// <summary>
    /// 正常模式
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 随机模式
    /// </summary>
    Shuffled = 1,
}

public enum RepeatState
{
    /// <summary>
    /// 不循环
    /// </summary>
    NoRepeat = 0,

    /// <summary>
    /// 列表循环
    /// </summary>
    RepeatAll = 1,

    /// <summary>
    /// 单曲循环
    /// </summary>
    RepeatOne = 2,
}
