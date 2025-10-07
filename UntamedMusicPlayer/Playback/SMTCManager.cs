using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace UntamedMusicPlayer.Playback;

/// <summary>
/// 系统媒体传输控件管理器
/// </summary>
public partial class SMTCManager : IDisposable
{
    private readonly SharedPlaybackState _state;

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
    /// 播放状态变化事件
    /// </summary>
    public event Action<SystemMediaTransportControlsButton>? ButtonPressed;

    public SMTCManager(SharedPlaybackState state)
    {
        _state = state;
        _systemControls = _tempPlayer.SystemMediaTransportControls;
        _displayUpdater = _systemControls.DisplayUpdater;
        _displayUpdater.Type = MediaPlaybackType.Music;
        _displayUpdater.AppMediaId = "AppDisplayName".GetLocalized();
        _systemControls.IsEnabled = true;
        _systemControls.ButtonPressed += OnSystemControlsButtonPressed;
        _timelineProperties.StartTime = TimeSpan.Zero;
        _timelineProperties.MinSeekTime = TimeSpan.Zero;

        _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName
            is nameof(SharedPlaybackState.RepeatMode)
                or nameof(SharedPlaybackState.PlayQueueIndex)
        )
        {
            UpdateButtonState();
        }
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
    public void UpdatePlaybackStatus(MediaPlaybackStatus playbackStatus) =>
        _systemControls.PlaybackStatus = playbackStatus;

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
    public void UpdateButtonState()
    {
        var isFirstSong = _state.PlayQueueIndex == 0;
        var isLastSong = _state.PlayQueueIndex == _state.PlayQueueCount - 1;
        var isRepeatOffOrSingle =
            _state.RepeatMode == RepeatState.NoRepeat || _state.RepeatMode == RepeatState.RepeatOne;

        _systemControls.IsPreviousEnabled = !(isFirstSong && isRepeatOffOrSingle);
        _systemControls.IsNextEnabled = !(isLastSong && isRepeatOffOrSingle);
    }

    /// <summary>
    /// 更新媒体信息
    /// </summary>
    public void UpdateMediaInfo()
    {
        _displayUpdater.MusicProperties.Title = _state.CurrentSong!.Title;
        _displayUpdater.MusicProperties.Artist =
            _state.CurrentSong.ArtistsStr == "SongInfo_UnknownArtist".GetLocalized()
                ? ""
                : _state.CurrentSong.ArtistsStr;
        _timelineProperties.MaxSeekTime = _state.TotalPlayingTime;
        _timelineProperties.EndTime = _state.TotalPlayingTime;
    }

    /// <summary>
    /// 设置封面图片
    /// </summary>
    /// <param name="song">当前歌曲</param>
    public async Task SetCoverImageAndUpdateAsync()
    {
        var song = _state.CurrentSong!;
        var defaultCoverUri = new Uri("ms-appx:///Assets/NoCover.png");

        if (song.Cover is null) // 没有封面时使用默认图片
        {
            _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(defaultCoverUri);
            _displayUpdater.Update();
            return;
        }

        try
        {
            if (song.IsOnline) // 在线歌曲:直接使用 URL
            {
                var coverPath = (song as IDetailedOnlineSongInfo)?.CoverPath;
                _displayUpdater.Thumbnail = coverPath is not null
                    ? RandomAccessStreamReference.CreateFromUri(new Uri(coverPath))
                    : RandomAccessStreamReference.CreateFromUri(defaultCoverUri);
            }
            else // 本地歌曲:从byte[]加载
            {
                var localSong = song as DetailedLocalSongInfo;
                if (localSong?.CoverBuffer is not null)
                {
                    _currentCoverStream?.Dispose();
                    _currentCoverStream = new InMemoryRandomAccessStream();
                    await _currentCoverStream.WriteAsync(localSong.CoverBuffer.AsBuffer());
                    _currentCoverStream.Seek(0);
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromStream(
                        _currentCoverStream
                    );
                }
                else
                {
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                        defaultCoverUri
                    );
                }
            }
        }
        catch
        {
            _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(defaultCoverUri);
        }
        finally
        {
            _displayUpdater.Update();
        }
    }

    /// <summary>
    /// 更新时间轴属性
    /// </summary>
    public void UpdateTimelinePosition()
    {
        _timelineProperties.Position = _state.CurrentPlayingTime;
        _systemControls.UpdateTimelineProperties(_timelineProperties);
    }

    public void Dispose()
    {
        _systemControls.ButtonPressed -= OnSystemControlsButtonPressed;
        _currentCoverStream?.Dispose();
        _tempPlayer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
