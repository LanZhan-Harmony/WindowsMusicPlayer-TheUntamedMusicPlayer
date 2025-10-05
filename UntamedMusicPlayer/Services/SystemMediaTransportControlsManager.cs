using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace UntamedMusicPlayer.Services;

/// <summary>
/// 系统媒体传输控件管理器
/// </summary>
public partial class SystemMediaTransportControlsManager : IDisposable
{
    /// <summary>
    /// 用于获取SMTC的临时播放器
    /// </summary>
    private readonly MediaPlayer _tempPlayer = new();

    /// <summary>
    /// 用于SMTC显示封面图片的流
    /// </summary>
    private static InMemoryRandomAccessStream? _currentCoverStream = null!;

    /// <summary>
    /// SMTC控件
    /// </summary>
    private readonly SystemMediaTransportControls _systemControls;

    /// <summary>
    /// SMTC显示内容更新器
    /// </summary>
    private readonly SystemMediaTransportControlsDisplayUpdater _displayUpdater;

    /// <summary>
    /// SMTC时间线属性
    /// </summary>
    private readonly SystemMediaTransportControlsTimelineProperties _timelineProperties = new();

    /// <summary>
    /// 播放队列歌曲数量
    /// </summary>
    private int _playQueueLength = 0;

    /// <summary>
    /// 当前歌曲在播放队列中的索引
    /// </summary>
    private int _playQueueIndex = 0;

    /// <summary>
    /// 循环播放模式
    /// </summary>
    private byte _repeatMode = 0;

    /// <summary>
    /// 播放状态变化事件
    /// </summary>
    public event Action<SystemMediaTransportControlsButton>? ButtonPressed;

    public SystemMediaTransportControlsManager()
    {
        _systemControls = _tempPlayer.SystemMediaTransportControls;
        _displayUpdater = _systemControls.DisplayUpdater;
        _displayUpdater.Type = MediaPlaybackType.Music;
        _displayUpdater.AppMediaId = "AppDisplayName".GetLocalized();
        _systemControls.IsEnabled = true;
        _systemControls.ButtonPressed += OnSystemControlsButtonPressed;
        _timelineProperties.StartTime = TimeSpan.Zero;
        _timelineProperties.MinSeekTime = TimeSpan.Zero;
    }

    /// <summary>
    /// 系统媒体控制按钮按下事件处理
    /// </summary>
    private void OnSystemControlsButtonPressed(
        SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args
    )
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => ButtonPressed?.Invoke(args.Button)
        );
    }

    /// <summary>
    /// 更新播放状态
    /// </summary>
    /// <param name="playbackStatus">播放状态</param>
    public void UpdatePlaybackStatus(MediaPlaybackStatus playbackStatus)
    {
        _systemControls.PlaybackStatus = playbackStatus;
    }

    /// <summary>
    /// 设置按钮是否可用
    /// </summary>
    /// <param name="isPlayEnabled">播放按钮是否可用</param>
    /// <param name="isPauseEnabled">暂停按钮是否可用</param>
    /// <param name="isPreviousEnabled">上一首按钮是否可用</param>
    /// <param name="isNextEnabled">下一首按钮是否可用</param>
    public void SetButtonsEnabled(
        bool isPlayEnabled,
        bool isPauseEnabled,
        bool isPreviousEnabled,
        bool isNextEnabled
    )
    {
        _systemControls.IsPlayEnabled = isPlayEnabled;
        _systemControls.IsPauseEnabled = isPauseEnabled;
        _systemControls.IsPreviousEnabled = isPreviousEnabled;
        _systemControls.IsNextEnabled = isNextEnabled;
    }

    /// <summary>
    /// 更新播放队列信息以计算按钮状态
    /// </summary>
    /// <param name="playQueueIndex">当前播放索引</param>
    /// <param name="playQueueLength">播放队列长度</param>
    /// <param name="repeatMode">循环模式</param>
    public void UpdatePlayQueueInfo(int playQueueIndex, int playQueueLength, byte repeatMode)
    {
        _playQueueIndex = playQueueIndex;
        _playQueueLength = playQueueLength;
        _repeatMode = repeatMode;

        UpdateNavigationButtonsState();
    }

    /// <summary>
    /// 更新导航按钮状态
    /// </summary>
    private void UpdateNavigationButtonsState()
    {
        var isFirstSong = _playQueueIndex == 0;
        var isLastSong = _playQueueIndex == _playQueueLength - 1;
        var isRepeatOffOrSingle = _repeatMode == 0 || _repeatMode == 2;

        _systemControls.IsPreviousEnabled = !(isFirstSong && isRepeatOffOrSingle);
        _systemControls.IsNextEnabled = !(isLastSong && isRepeatOffOrSingle);
    }

    /// <summary>
    /// 更新媒体信息
    /// </summary>
    /// <param name="title">歌曲标题</param>
    /// <param name="artist">艺术家</param>
    /// <param name="totalDuration">总时长</param>
    public void UpdateMediaInfo(string title, string artist, TimeSpan totalDuration)
    {
        _displayUpdater.MusicProperties.Title = title;
        _displayUpdater.MusicProperties.Artist =
            artist == "SongInfo_UnknownArtist".GetLocalized() ? "" : artist;
        _timelineProperties.MaxSeekTime = totalDuration;
        _timelineProperties.EndTime = totalDuration;
    }

    /// <summary>
    /// 设置封面图片
    /// </summary>
    /// <param name="song">当前歌曲</param>
    public async Task SetCoverImageAsync(IDetailedSongInfoBase song)
    {
        if (song.Cover is null)
        {
            _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                new Uri("ms-appx:///Assets/NoCover.png")
            );
            return;
        }

        if (song.IsOnline)
        {
            try
            {
                var info = (IDetailedOnlineSongInfo)song;
                _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                    new Uri(info.CoverPath!)
                );
            }
            catch { }
        }
        else
        {
            try
            {
                var info = (DetailedLocalSongInfo)song;
                _currentCoverStream?.Dispose();
                _currentCoverStream = new InMemoryRandomAccessStream();
                await _currentCoverStream.WriteAsync(info.CoverBuffer.AsBuffer());
                _currentCoverStream.Seek(0);
                _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromStream(
                    _currentCoverStream
                );
            }
            catch
            {
                _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                    new Uri("ms-appx:///Assets/NoCover.png")
                );
            }
        }
    }

    /// <summary>
    /// 更新时间轴属性
    /// </summary>
    /// <param name="currentTime">当前播放时间</param>
    public void UpdateTimelinePosition(TimeSpan currentTime)
    {
        _timelineProperties.Position = currentTime;
        _systemControls.UpdateTimelineProperties(_timelineProperties);
    }

    /// <summary>
    /// 应用所有更改
    /// </summary>
    public void Update()
    {
        _displayUpdater.Update();
    }

    public void Dispose()
    {
        _systemControls.ButtonPressed -= OnSystemControlsButtonPressed;
        _currentCoverStream?.Dispose();
        _tempPlayer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
