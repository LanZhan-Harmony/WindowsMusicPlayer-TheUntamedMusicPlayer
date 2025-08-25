using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using ManagedBass;
using ManagedBass.Fx;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.Services;
using Windows.Media;
using Windows.Storage.Streams;
using Windows.System.Threading;
using ZLinq;

namespace The_Untamed_Music_Player.Models;

public partial class MusicPlayer : ObservableObject, IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private static readonly ILogger _logger = LoggingService.CreateLogger<MusicPlayer>();

    /// <summary>
    /// 用于获取SMTC的临时播放器
    /// </summary>
    private readonly Windows.Media.Playback.MediaPlayer _tempPlayer = new();

    /// <summary>
    /// 用于SMTC显示封面图片的流
    /// </summary>
    private static InMemoryRandomAccessStream? _currentCoverStream = null!;

    /// <summary>
    /// SMTC控件
    /// </summary>
    private SystemMediaTransportControls _systemControls = null!;

    /// <summary>
    /// SMTC显示内容更新器
    /// </summary>
    private SystemMediaTransportControlsDisplayUpdater _displayUpdater = null!;

    /// <summary>
    /// SMTC时间线属性
    /// </summary>
    private readonly SystemMediaTransportControlsTimelineProperties _timelineProperties = new();

    /// <summary>
    /// 线程锁开启状态, true为开启, false为关闭
    /// </summary>
    private bool _lockable = false;

    /// <summary>
    /// 播放失败次数
    /// </summary>
    private byte _failedCount = 0;

    /// <summary>
    /// 播放队列歌曲数量
    /// </summary>
    private int _playQueueLength = 0;

    /// <summary>
    /// 当前歌词切片在集合中的索引
    /// </summary>
    private int _currentLyricIndex = 0;

    /// <summary>
    /// Bass音频流句柄
    /// </summary>
    private int _currentStream = 0;

    /// <summary>
    /// 用于变速不变调的Tempo流句柄
    /// </summary>
    private int _tempoStream = 0;

    /// <summary>
    /// 播放结束事件回调
    /// </summary>
    private SyncProcedure? _syncEndCallback;

    /// <summary>
    /// 播放失败事件回调
    /// </summary>
    private SyncProcedure? _syncFailCallback;

    /// <summary>
    /// 播放速度
    /// </summary>
    public double PlaySpeed
    {
        get;
        set
        {
            field = value;
            SetPlaybackSpeed(value);
        }
    } = 1.0;

    /// <summary>
    /// 线程计时器
    /// </summary>
    public ThreadPoolTimer? PositionUpdateTimer250ms { get; set; }

    /// <summary>
    /// 播放队列集合
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> PlayQueue { get; set; } = [];

    /// <summary>
    /// 随机播放队列集合
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> ShuffledPlayQueue { get; set; } = [];

    /// <summary>
    /// 随机播放模式, true为开启, false为关闭.
    /// </summary>
    [ObservableProperty]
    public partial bool ShuffleMode { get; set; } = false;

    /// <summary>
    /// 播放队列名
    /// </summary>
    [ObservableProperty]
    public partial string PlayQueueName { get; set; } = "";

    /// <summary>
    /// 当前歌曲在播放队列中的索引
    /// </summary>
    [ObservableProperty]
    public partial int PlayQueueIndex { get; set; }

    partial void OnPlayQueueIndexChanged(int value)
    {
        var repeatOffOrSingle = RepeatMode == 0 || RepeatMode == 2;
        _systemControls.IsPreviousEnabled = !(value == 0 && repeatOffOrSingle);
        _systemControls.IsNextEnabled = !(value == _playQueueLength - 1 && repeatOffOrSingle);
    }

    /// <summary>
    /// 循环播放模式, 0为不循环, 1为列表循环, 2为单曲循环
    /// </summary>
    [ObservableProperty]
    public partial byte RepeatMode { get; set; } = 0;

    partial void OnRepeatModeChanged(byte value)
    {
        var isFirstSong = PlayQueueIndex == 0;
        var isLastSong = PlayQueueIndex == _playQueueLength - 1;
        var isRepeatOffOrSingle = value == 0 || value == 2;
        _systemControls.IsPreviousEnabled = !(isFirstSong && isRepeatOffOrSingle);
        _systemControls.IsNextEnabled = !(isLastSong && isRepeatOffOrSingle);
    }

    /// <summary>
    /// 播放状态, 0为暂停, 1为播放, 2为加载中
    /// </summary>
    [ObservableProperty]
    public partial byte PlayState { get; set; } = 0;

    /// <summary>
    /// 当前播放的歌曲简要版
    /// </summary>
    [ObservableProperty]
    public partial IBriefSongInfoBase? CurrentBriefSong { get; set; }

    partial void OnCurrentBriefSongChanged(IBriefSongInfoBase? value)
    {
        _ = SaveCurrentBriefSongAsync();
    }

    /// <summary>
    /// 当前播放歌曲
    /// </summary>
    [ObservableProperty]
    public partial IDetailedSongInfoBase? CurrentSong { get; set; }

    /// <summary>
    /// 当前播放时间
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan CurrentPlayingTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// 当前歌曲总时长
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan TotalPlayingTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// 当前播放进度(百分比)
    /// </summary>
    [ObservableProperty]
    public partial double CurrentPosition { get; set; } = 0;

    /// <summary>
    /// 当前音量
    /// </summary>
    [ObservableProperty]
    public partial double CurrentVolume { get; set; } = 100;

    partial void OnCurrentVolumeChanged(double value)
    {
        if (!IsMute && _tempoStream != 0)
        {
            Bass.ChannelSetAttribute(_tempoStream, ChannelAttribute.Volume, value / 100.0);
        }
    }

    /// <summary>
    /// 是否静音, true为静音, false为非静音
    /// </summary>
    [ObservableProperty]
    public partial bool IsMute { get; set; } = false;

    partial void OnIsMuteChanged(bool value)
    {
        if (_tempoStream != 0)
        {
            Bass.ChannelSetAttribute(
                _tempoStream,
                ChannelAttribute.Volume,
                value ? 0 : CurrentVolume / 100.0
            );
        }
    }

    /// <summary>
    /// 当前歌词内容
    /// </summary>
    [ObservableProperty]
    public partial string CurrentLyricContent { get; set; } = "";

    /// <summary>
    /// 当前歌词切片集合
    /// </summary>
    [ObservableProperty]
    public partial List<LyricSlice> CurrentLyric { get; set; } = [];

    public MusicPlayer()
    {
        InitializeBass();
        InitializeSystemMediaTransportControls();
        LoadCurrentStateAsync();
    }

    /// <summary>
    /// 初始化Bass音频库
    /// </summary>
    private void InitializeBass()
    {
        // 初始化Bass - 使用默认设备
        if (!Bass.Init())
        {
            Debug.WriteLine($"Bass初始化失败: {Bass.LastError}");
            return;
        }

        // 加载Bass插件
        LoadBassPlugins();

        // 设置同步回调
        _syncEndCallback = OnPlayBackEnded;
        _syncFailCallback = OnPlaybackFailed;
    }

    /// <summary>
    /// 加载Bass插件
    /// </summary>
    private static void LoadBassPlugins()
    {
        var appPath = AppContext.BaseDirectory;
        var pluginPaths = new[]
        {
            "bassape.dll",
            "basscd.dll",
            "bassdsd.dll",
            "bassflac.dll",
            "basshls.dll",
            "bassmidi.dll",
            "bassopus.dll",
            "basswebm.dll",
            "basswv.dll",
        };

        foreach (var pluginPath in pluginPaths)
        {
            var fullPath = Path.Combine(appPath, pluginPath);
            Bass.PluginLoad(fullPath);
        }
    }

    /// <summary>
    /// 初始化系统媒体传输控件
    /// </summary>
    private void InitializeSystemMediaTransportControls()
    {
        _systemControls = _tempPlayer.SystemMediaTransportControls;
        _displayUpdater = _systemControls.DisplayUpdater;
        _displayUpdater.Type = MediaPlaybackType.Music;
        _displayUpdater.AppMediaId = "AppDisplayName".GetLocalized();
        _systemControls.IsEnabled = true;
        _systemControls.ButtonPressed += SystemControls_ButtonPressed;
        _timelineProperties.StartTime = TimeSpan.Zero;
        _timelineProperties.MinSeekTime = TimeSpan.Zero;
    }

    public void Reset()
    {
        Stop();
        CurrentBriefSong = null;
        CurrentSong = null;
        PlayQueueIndex = 0;
        _playQueueLength = 0;
        PlayQueue.Clear();
        ShuffledPlayQueue.Clear();
    }

    /// <summary>
    /// Bass同步回调 - 播放结束
    /// </summary>
    private void OnPlayBackEnded(int handle, int channel, int data, IntPtr user)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_lockable)
            {
                if (RepeatMode == 2)
                {
                    PlaySongByInfo(CurrentBriefSong!);
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    /// <summary>
    /// Bass同步回调 - 播放失败
    /// </summary>
    private void OnPlaybackFailed(int handle, int channel, int data, IntPtr user)
    {
        _logger.SongPlaybackError(CurrentSong!.Title);
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || CurrentSong.IsOnline)
            {
                Stop();
            }
            else
            {
                CurrentBriefSong!.IsPlayAvailable = false;
                _failedCount++;
                if (_failedCount > 2)
                {
                    _failedCount = 0;
                    Stop();
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    private void HandleSongNotAvailable()
    {
        _logger.SongPlaybackError(CurrentSong!.Title);
        if (PlayQueue.Count == 0)
        {
            return;
        }
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || CurrentSong.IsOnline)
            {
                Stop();
            }
            else
            {
                _failedCount++;
                if (_failedCount > 2)
                {
                    _failedCount = 0;
                    Stop();
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    /// <summary>
    /// 按路径播放歌曲
    /// </summary>
    /// <param name="info"></param>
    public void PlaySongByInfo(IBriefSongInfoBase info)
    {
        var index =
            PlayQueue.AsValueEnumerable().FirstOrDefault(song => song.Song == info)?.Index ?? 0;
        PlaySongByIndex(index);
    }

    public void PlaySongByIndexedInfo(IndexedPlayQueueSong info)
    {
        PlaySongByIndex(info.Index);
    }

    /// <summary>
    /// 按索引播放歌曲
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isLast"></param>
    private async void PlaySongByIndex(int index, bool isLast = false)
    {
        Stop();
        PlayState = 2;
        var songToPlay = ShuffleMode ? ShuffledPlayQueue[index] : PlayQueue[index];
        CurrentBriefSong = songToPlay.Song;
        CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay.Song);
        PlayQueueIndex = isLast ? 0 : index;
        if (CurrentSong!.IsPlayAvailable)
        {
            await SetSource(CurrentSong!.Path);
            _ = UpdateLyric(CurrentSong!.Lyric);
            _systemControls.IsPlayEnabled = true;
            _systemControls.IsPauseEnabled = true;
            if (!isLast)
            {
                Play();
            }
        }
        else
        {
            HandleSongNotAvailable();
        }
    }

    /// <summary>
    /// 将歌曲添加到下一首播放
    /// </summary>
    /// <param name="info"></param>
    public void AddSongToNextPlay(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var insertIndex = PlayQueueIndex + 1;
        queue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, info));
        _playQueueLength++;
        // 仅更新从插入位置之后的索引
        for (var i = insertIndex + 1; i < queue.Count; i++)
        {
            queue[i].Index = i;
        }
        if (ShuffleMode)
        {
            PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, info));
        }
    }

    /// <summary>
    /// 将歌曲列表添加到下一首播放
    /// </summary>
    /// <param name="songs"></param>
    public void AddSongsToNextPlay(IEnumerable<IBriefSongInfoBase> songs)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var insertIndex = PlayQueueIndex + 1;
        foreach (var song in songs)
        {
            queue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, song));
            insertIndex++;
        }
        _playQueueLength += songs.AsValueEnumerable().Count();
        // 仅更新从插入位置之后的索引
        for (var i = insertIndex; i < queue.Count; i++)
        {
            queue[i].Index = i;
        }
        if (ShuffleMode)
        {
            foreach (var song in songs)
            {
                PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 将歌曲添加到播放队列
    /// </summary>
    /// <param name="info"></param>
    public void AddSongToPlayQueue(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        queue.Add(new IndexedPlayQueueSong(queue.Count, info));
        _playQueueLength++;
        if (ShuffleMode)
        {
            PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, info));
        }
    }

    /// <summary>
    /// 将歌曲列表添加到播放队列
    /// </summary>
    /// <param name="songs"></param>
    public void AddSongsToPlayQueue(IEnumerable<IBriefSongInfoBase> songs)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        foreach (var song in songs)
        {
            queue.Add(new IndexedPlayQueueSong(queue.Count, song));
        }
        _playQueueLength += songs.AsValueEnumerable().Count();
        if (ShuffleMode)
        {
            foreach (var song in songs)
            {
                PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 从播放队列中移除歌曲
    /// </summary>
    /// <param name="info"></param>
    public async Task RemoveSong(IndexedPlayQueueSong info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.Index;
        int newIndex;
        if (index == PlayQueueIndex) // 如果删除的歌曲正好是当前播放歌曲
        {
            var playState = PlayState;
            Stop();
            newIndex = PlayQueueIndex < _playQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var songToPlay = ShuffleMode ? ShuffledPlayQueue[newIndex] : PlayQueue[newIndex];
            CurrentBriefSong = songToPlay.Song;
            CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay.Song);
            PlayQueueIndex = newIndex == 0 ? 0 : newIndex - 1;
            if (playState != 0)
            {
                if (CurrentSong!.IsPlayAvailable)
                {
                    await SetSource(CurrentSong!.Path);
                    _ = UpdateLyric(CurrentSong!.Lyric);
                    _systemControls.IsPauseEnabled = true;
                    _systemControls.IsPlayEnabled = true;
                    Play();
                }
                else
                {
                    HandleSongNotAvailable();
                }
            }
        }
        else if (index < PlayQueueIndex) // 删除歌曲在当前播放歌曲之前时，当前索引前移1位
        {
            PlayQueueIndex--;
        }

        // 从队列中移除歌曲并更新后续歌曲的索引
        queue.RemoveAt(index);
        _playQueueLength--;
        for (var i = index; i < queue.Count; i++)
        {
            queue[i].Index = i;
        }
        OnPropertyChanged(nameof(PlayQueueIndex));

        if (queue.Count == 0)
        {
            ClearPlayQueue();
        }
    }

    /// <summary>
    /// 将歌曲上移
    /// </summary>
    /// <param name="info"></param>
    public void MoveUpSong(IndexedPlayQueueSong info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.Index;
        if (index > 0)
        {
            (queue[index - 1], queue[index]) = (queue[index], queue[index - 1]);
            queue[index].Index = index;
            queue[index - 1].Index = index - 1;
            if (index == PlayQueueIndex)
            {
                PlayQueueIndex--;
            }
        }
    }

    /// <summary>
    /// 将歌曲下移
    /// </summary>
    /// <param name="info"></param>
    public void MoveDownSong(IndexedPlayQueueSong info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.Index;
        if (index < queue.Count - 1)
        {
            (queue[index + 1], queue[index]) = (queue[index], queue[index + 1]);
            queue[index].Index = index;
            queue[index + 1].Index = index + 1;
            if (index == PlayQueueIndex)
            {
                PlayQueueIndex++;
            }
        }
    }

    /// <summary>
    /// 为播放器设置音乐源
    /// </summary>
    /// <param name="path"></param>
    private async Task SetSource(string path)
    {
        try
        {
            Data.RootPlayBarViewModel!.ButtonVisibility = Visibility.Visible;
            Data.RootPlayBarViewModel!.Availability = true;

            // 释放之前的流
            if (_tempoStream != 0)
            {
                Bass.StreamFree(_tempoStream);
                _tempoStream = 0;
            }
            if (_currentStream != 0)
            {
                Bass.StreamFree(_currentStream);
                _currentStream = 0;
            }

            if (CurrentSong!.IsOnline) // 在线流
            {
                _currentStream = Bass.CreateStream(path, 0, BassFlags.Decode, null);
            }
            else // 本地文件
            {
                _currentStream = Bass.CreateStream(path, 0, 0, BassFlags.Decode);
            }
            if (_currentStream == 0)
            {
                Debug.WriteLine($"创建Bass流失败: {Bass.LastError}");
                return;
            }

            // 使用BassFx创建Tempo流用于变速不变调
            _tempoStream = BassFx.TempoCreate(_currentStream, BassFlags.FxFreeSource);
            if (_tempoStream == 0)
            {
                Debug.WriteLine($"创建Tempo流失败: {Bass.LastError}");
                return;
            }

            // 设置初始播放速度
            var tempoPercent = (PlaySpeed - 1.0) * 100.0;
            var result = Bass.ChannelSetAttribute(
                _tempoStream,
                ChannelAttribute.Tempo,
                (float)tempoPercent
            );
            if (!result)
            {
                Debug.WriteLine($"设置Tempo失败: {Bass.LastError}");
            }

            Bass.ChannelSetSync(_tempoStream, SyncFlags.End, 0, _syncEndCallback); // 设置播放结束回调
            Bass.ChannelSetSync(_tempoStream, SyncFlags.Stalled, 0, _syncFailCallback); // 设置播放失败回调

            // 设置音量
            Bass.ChannelSetAttribute(
                _tempoStream,
                ChannelAttribute.Volume,
                IsMute ? 0 : CurrentVolume / 100.0
            );

            // 获取歌曲时长
            var lengthBytes = Bass.ChannelGetLength(_tempoStream);
            var lengthSeconds = Bass.ChannelBytes2Seconds(_tempoStream, lengthBytes);
            TotalPlayingTime = TimeSpan.FromSeconds(lengthSeconds);

            _displayUpdater.MusicProperties.Title = CurrentSong!.Title;
            _displayUpdater.MusicProperties.Artist =
                CurrentSong.ArtistsStr == "SongInfo_UnknownArtist".GetLocalized()
                    ? ""
                    : CurrentSong.ArtistsStr;
            _timelineProperties.MaxSeekTime = TotalPlayingTime;
            _timelineProperties.EndTime = TotalPlayingTime;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SetSource异常: {ex.Message}");
        }

        // 设置封面图片
        await SetCoverImage();
        _displayUpdater.Update();
    }

    /// <summary>
    /// 设置封面图片
    /// </summary>
    private async Task SetCoverImage()
    {
        if (CurrentSong!.Cover is not null)
        {
            if (CurrentSong.IsOnline)
            {
                try
                {
                    var info = (IDetailedOnlineSongInfo)CurrentSong;
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
                    var info = (DetailedLocalSongInfo)CurrentSong;
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
        else
        {
            _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                new Uri("ms-appx:///Assets/NoCover.png")
            );
        }
    }

    /// <summary>
    /// 设置播放队列
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    public void SetPlayQueue(string name, IEnumerable<IBriefSongInfoBase> list)
    {
        if (PlayQueue.Count != list.AsValueEnumerable().Count() || PlayQueueName != name)
        {
            PlayQueueName = name;
            PlayQueue =
            [
                .. list.AsValueEnumerable()
                    .Select((song, index) => new IndexedPlayQueueSong(index, song)),
            ];

            _playQueueLength = list.AsValueEnumerable().Count();

            if (ShuffleMode)
            {
                UpdateShufflePlayQueue();
                // 更新随机播放队列中每首歌曲的 Index 为实际位置
                for (var i = 0; i < ShuffledPlayQueue.Count; i++)
                {
                    ShuffledPlayQueue[i].Index = i;
                }
            }
        }
        _ = FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    public void SetShuffledPlayQueue(string name, IEnumerable<IBriefSongInfoBase> list)
    {
        if (
            ShuffleMode == false
            || PlayQueue.Count != list.AsValueEnumerable().Count()
            || PlayQueueName != name
        )
        {
            ShuffleMode = true;
            PlayQueueName = name;
            PlayQueue =
            [
                .. list.AsValueEnumerable()
                    .Select((song, index) => new IndexedPlayQueueSong(index, song)),
            ];
            _playQueueLength = list.AsValueEnumerable().Count();

            UpdateShufflePlayQueue();
            for (var i = 0; i < ShuffledPlayQueue.Count; i++)
            {
                ShuffledPlayQueue[i].Index = i;
            }
        }
        _ = FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 计时器更新事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerHandler250ms(ThreadPoolTimer timer)
    {
        try
        {
            if (_tempoStream == 0 || _lockable || PlayState != 1)
            {
                return;
            }

            var positionBytes = Bass.ChannelGetPosition(_tempoStream);
            var positionSeconds = Bass.ChannelBytes2Seconds(_tempoStream, positionBytes);
            var playingTime = TimeSpan.FromSeconds(positionSeconds);

            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                CurrentPlayingTime = playingTime;
                if (TotalPlayingTime.TotalMilliseconds > 0)
                {
                    CurrentPosition =
                        100 * (playingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
                }
            });

            var dispatcherQueue =
                Data.LyricPage?.DispatcherQueue ?? Data.DesktopLyricWindow?.DispatcherQueue;
            if (CurrentLyric.Count > 0)
            {
                dispatcherQueue?.TryEnqueue(() => UpdateCurrentLyricIndex(positionSeconds * 1000));
            }

            _timelineProperties.Position = CurrentPlayingTime;
            _systemControls.UpdateTimelineProperties(_timelineProperties);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"计时器更新异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前歌词切片索引
    /// </summary>
    /// <param name="currentTime"></param>
    /// <returns></returns>
    public int GetCurrentLyricIndex(double currentTime)
    {
        for (var i = 0; i < CurrentLyric.Count; i++)
        {
            if (CurrentLyric[i].Time > currentTime)
            {
                return i > 0 ? i - 1 : 0;
            }
        }
        return CurrentLyric.Count - 1;
    }

    /// <summary>
    /// 更新当前歌词索引和状态
    /// </summary>
    /// <param name="currentTime">当前播放时间（毫秒）</param>
    private void UpdateCurrentLyricIndex(double currentTime)
    {
        if (CurrentLyric.Count == 0)
        {
            return;
        }

        var newIndex = GetCurrentLyricIndex(currentTime);

        if (newIndex != _currentLyricIndex)
        {
            // 更新旧歌词状态
            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyric.Count)
            {
                CurrentLyric[_currentLyricIndex].IsCurrent = false;
            }

            _currentLyricIndex = newIndex;

            // 更新新歌词状态
            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyric.Count)
            {
                CurrentLyric[_currentLyricIndex].IsCurrent = true;
                CurrentLyricContent = CurrentLyric[_currentLyricIndex].Content;
            }
        }
    }

    private void SystemControls_ButtonPressed(
        SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args
    )
    {
        switch (args.Button)
        {
            case SystemMediaTransportControlsButton.Play:
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    PlayPauseUpdate
                );
                break;
            case SystemMediaTransportControlsButton.Pause:
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    PlayPauseUpdate
                );
                break;
            case SystemMediaTransportControlsButton.Previous: // 注意: 必须在UI线程中调用
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    PlayPreviousSong
                );
                break;
            case SystemMediaTransportControlsButton.Next:
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    PlayNextSong
                );
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 静音按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        IsMute = !IsMute;
    }

    /// <summary>
    /// 播放
    /// </summary>
    public void Play()
    {
        PositionUpdateTimer250ms = ThreadPoolTimer.CreatePeriodicTimer(
            UpdateTimerHandler250ms,
            TimeSpan.FromMilliseconds(250)
        );
        if (_tempoStream != 0)
        {
            Bass.ChannelPlay(_tempoStream, false);
        }
        PlayState = 1;
        _systemControls.PlaybackStatus = MediaPlaybackStatus.Playing;
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        if (_tempoStream != 0)
        {
            Bass.ChannelPause(_tempoStream);
        }
        PlayState = 0;
        _systemControls.PlaybackStatus = MediaPlaybackStatus.Paused;
        PositionUpdateTimer250ms?.Cancel();
        PositionUpdateTimer250ms = null;
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        if (_tempoStream != 0)
        {
            Bass.ChannelStop(_tempoStream);
        }
        PlayState = 0;
        CurrentPlayingTime = TimeSpan.Zero;
        CurrentPosition = 0;
        _currentLyricIndex = 0;
        CurrentLyricContent = "";
        PositionUpdateTimer250ms?.Cancel();
        PositionUpdateTimer250ms = null;
    }

    /// <summary>
    /// 播放上一曲
    /// </summary>
    public void PlayPreviousSong()
    {
        try
        {
            var newIndex = PlayQueueIndex > 0 ? PlayQueueIndex - 1 : PlayQueueIndex;

            if (RepeatMode == 1) // 列表循环
            {
                newIndex = (PlayQueueIndex + _playQueueLength - 1) % _playQueueLength;
            }

            PlaySongByIndex(newIndex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 播放下一曲
    /// </summary>
    public void PlayNextSong()
    {
        try
        {
            var newIndex = PlayQueueIndex < _playQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var isLast = PlayQueueIndex >= _playQueueLength - 1;

            if (RepeatMode == 1) // 列表循环
            {
                newIndex = (PlayQueueIndex + 1) % _playQueueLength;
                isLast = false;
            }

            PlaySongByIndex(newIndex, isLast);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 播放按钮更新
    /// </summary>
    public void PlayPauseUpdate()
    {
        if (PlayState == 0)
        {
            Play();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// 随机播放模式更新
    /// </summary>
    public void ShuffleModeUpdate()
    {
        ShuffleMode = !ShuffleMode;
        if (ShuffleMode)
        {
            UpdateShufflePlayQueue();
            for (var i = 0; i < ShuffledPlayQueue.Count; i++)
            {
                ShuffledPlayQueue[i].Index = i;
            }
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    ShuffledPlayQueue
                        .AsValueEnumerable()
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info.Song).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Song.Path == CurrentSong.Path
                        )
                        ?.Index ?? 0;
            }
        }
        else
        {
            ShuffledPlayQueue.Clear();
            for (var i = 0; i < PlayQueue.Count; i++)
            {
                PlayQueue[i].Index = i;
            }
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    PlayQueue
                        .AsValueEnumerable()
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info.Song).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Song.Path == CurrentSong.Path
                        )
                        ?.Index ?? 0;
            }
        }
    }

    /// <summary>
    /// 循环播放模式更新
    /// </summary>
    public void RepeatModeUpdate()
    {
        RepeatMode = (byte)((RepeatMode + 1) % 3);
    }

    /// <summary>
    /// 设置播放速度
    /// </summary>
    /// <param name="speed"></param>
    private void SetPlaybackSpeed(double speed)
    {
        if (_tempoStream != 0)
        {
            // 使用Tempo属性来改变播放速度而不改变音调, Tempo属性的值以百分比表示: 0=正常速度, 100=2倍速度, -50=0.5倍速度
            var tempoPercent = (speed - 1.0) * 100.0;
            Bass.ChannelSetAttribute(_tempoStream, ChannelAttribute.Tempo, (float)tempoPercent);
        }
    }

    /// <summary>
    /// 随机播放队列更新
    /// </summary>
    /// <returns></returns>
    public void UpdateShufflePlayQueue()
    {
        ShuffledPlayQueue = [.. PlayQueue.AsValueEnumerable().OrderBy(x => Guid.NewGuid())];
    }

    /// <summary>
    /// 清空播放队列
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
        CurrentBriefSong = null;
        CurrentSong = null;
        CurrentPlayingTime = TimeSpan.Zero;
        TotalPlayingTime = TimeSpan.Zero;
        PlayQueue.Clear();
        ShuffledPlayQueue.Clear();
        OnPropertyChanged(nameof(PlayQueue)); // 不能删,用于通知按钮是否禁用
        CurrentLyric.Clear();
        PlayQueueName = "";
        PlayQueueIndex = 0;
        _playQueueLength = 0;
        Data.RootPlayBarViewModel!.ButtonVisibility = Visibility.Collapsed;
        Data.RootPlayBarViewModel!.Availability = false;
        _systemControls.IsPlayEnabled = false;
        _systemControls.IsPauseEnabled = false;
        _systemControls.IsPreviousEnabled = false;
        _systemControls.IsNextEnabled = false;
        _ = FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 按下滑动条事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressLock(object sender, PointerRoutedEventArgs e)
    {
        _lockable = true;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(
            ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100
        );
    }

    /// <summary>
    /// 滑动滑动条事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void SliderUpdate(object sender, PointerRoutedEventArgs e)
    {
        CurrentPlayingTime = TimeSpan.FromMilliseconds(
            ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100
        );
        UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
    }

    /// <summary>
    /// 松开滑动条更新播放进度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressUpdate(object sender, PointerRoutedEventArgs e)
    {
        if (_tempoStream != 0) // 使用_tempoStream而不是_currentStream
        {
            var targetTimeSeconds = ((Slider)sender).Value * TotalPlayingTime.TotalSeconds / 100;
            var targetBytes = Bass.ChannelSeconds2Bytes(_tempoStream, targetTimeSeconds);

            // 对原始流设置位置，tempo流会自动跟随
            var result = Bass.ChannelSetPosition(_tempoStream, targetBytes);
            if (!result)
            {
                Debug.WriteLine($"设置播放位置失败: {Bass.LastError}");
            }

            CurrentPlayingTime = TimeSpan.FromSeconds(targetTimeSeconds);
            if (TotalPlayingTime.TotalMilliseconds > 0)
            {
                CurrentPosition =
                    100
                    * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
            }
            UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
        }
        _lockable = false;
    }

    /// <summary>
    /// 点击歌词更新播放进度
    /// </summary>
    /// <param name="time"></param>
    public void LyricProgressUpdate(double time)
    {
        _lockable = true;
        if (_tempoStream != 0) // 使用_tempoStream而不是_currentStream
        {
            var targetTimeSeconds = time / 1000.0;
            var targetBytes = Bass.ChannelSeconds2Bytes(_tempoStream, targetTimeSeconds);

            // 对原始流设置位置，tempo流会自动跟随
            var result = Bass.ChannelSetPosition(_tempoStream, targetBytes);
            if (!result)
            {
                Debug.WriteLine($"设置播放位置失败: {Bass.LastError}");
            }

            CurrentPlayingTime = TimeSpan.FromMilliseconds(time);
            if (TotalPlayingTime.TotalMilliseconds > 0)
            {
                CurrentPosition =
                    100
                    * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
            }
            UpdateCurrentLyricIndex(time);
        }
        _lockable = false;
    }

    public void SkipBack10sButton_Click(object sender, RoutedEventArgs e)
    {
        _lockable = true;
        if (_tempoStream != 0) // 使用_tempoStream而不是_currentStream
        {
            var currentPositionBytes = Bass.ChannelGetPosition(_tempoStream);
            var currentPositionSeconds = Bass.ChannelBytes2Seconds(
                _tempoStream,
                currentPositionBytes
            );
            var newPositionSeconds = Math.Max(0, currentPositionSeconds - 10);
            var newPositionBytes = Bass.ChannelSeconds2Bytes(_tempoStream, newPositionSeconds);

            var result = Bass.ChannelSetPosition(_tempoStream, newPositionBytes);
            if (!result)
            {
                Debug.WriteLine($"设置播放位置失败: {Bass.LastError}");
            }

            CurrentPlayingTime = TimeSpan.FromSeconds(newPositionSeconds);

            if (TotalPlayingTime.TotalMilliseconds > 0)
            {
                CurrentPosition =
                    100
                    * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
            }
            UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
        }
        _lockable = false;
    }

    public void SkipForw30sButton_Click(object sender, RoutedEventArgs e)
    {
        _lockable = true;
        if (_tempoStream != 0) // 使用_tempoStream而不是_currentStream
        {
            var currentPositionBytes = Bass.ChannelGetPosition(_tempoStream);
            var currentPositionSeconds = Bass.ChannelBytes2Seconds(
                _tempoStream,
                currentPositionBytes
            );
            var newPositionSeconds = Math.Min(
                TotalPlayingTime.TotalSeconds,
                currentPositionSeconds + 30
            );
            var newPositionBytes = Bass.ChannelSeconds2Bytes(_tempoStream, newPositionSeconds);

            var result = Bass.ChannelSetPosition(_tempoStream, newPositionBytes);
            if (!result)
            {
                Debug.WriteLine($"设置播放位置失败: {Bass.LastError}");
            }

            CurrentPlayingTime = TimeSpan.FromSeconds(newPositionSeconds);

            if (TotalPlayingTime.TotalMilliseconds > 0)
            {
                CurrentPosition =
                    100
                    * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
            }
            UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
        }
        _lockable = false;
    }

    public void SpeedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listview && listview.SelectedIndex is int selectedIndex)
        {
            PlaySpeed = selectedIndex switch
            {
                0 => 0.25,
                1 => 0.5,
                2 => 1,
                3 => 1.5,
                4 => 2,
                _ => 1,
            };
        }
    }

    public void SpeedListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listview)
        {
            listview.SelectedIndex = PlaySpeed switch
            {
                0.25 => 0,
                0.5 => 1,
                1 => 2,
                1.5 => 3,
                2 => 4,
                _ => 2,
            };
        }
    }

    public async Task UpdateLyric(string lyric)
    {
        CurrentLyric = await LyricSlice.GetLyricSlices(lyric);
        _currentLyricIndex = 0;
        CurrentLyricContent = "";

        if (CurrentLyric.Count > 0)
        {
            CurrentLyric[0].IsCurrent = true;
            CurrentLyricContent = CurrentLyric[0].Content;
        }
    }

    public async Task SaveCurrentBriefSongAsync()
    {
        await Task.Run(async () =>
        {
            if (CurrentBriefSong is not null)
            {
                var sourceMode = IBriefSongInfoBase.GetSourceMode(CurrentBriefSong);
                await _localSettingsService.SaveSettingAsync("CurrentBriefSong", CurrentBriefSong);
                await _localSettingsService.SaveSettingAsync("SourceMode", sourceMode);
            }
        });
    }

    /// <summary>
    /// 保存当前播放状态至设置存储
    /// </summary>
    public async void SaveCurrentStateAsync()
    {
        await FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
        await _localSettingsService.SaveSettingAsync("PlayQueueIndex", PlayQueueIndex);
        await _localSettingsService.SaveSettingAsync("ShuffleMode", ShuffleMode);
        await _localSettingsService.SaveSettingAsync("RepeatMode", RepeatMode);
        await _localSettingsService.SaveSettingAsync("IsMute", IsMute);
        await _localSettingsService.SaveSettingAsync("CurrentVolume", CurrentVolume);
        await _localSettingsService.SaveSettingAsync("PlaySpeed", PlaySpeed);
        await _localSettingsService.SaveSettingAsync("CurrentBriefSong", CurrentBriefSong);
        await SaveCurrentBriefSongAsync();
    }

    /// <summary>
    /// 从设置存储中读取当前播放状态
    /// </summary>
    public async void LoadCurrentStateAsync()
    {
        try
        {
            PlayQueueIndex = await _localSettingsService.ReadSettingAsync<int>("PlayQueueIndex");
            ShuffleMode = await _localSettingsService.ReadSettingAsync<bool>("ShuffleMode");
            RepeatMode = await _localSettingsService.ReadSettingAsync<byte>("RepeatMode");
            IsMute = await _localSettingsService.ReadSettingAsync<bool>("IsMute");
            var sourceMode = await _localSettingsService.ReadSettingAsync<short>("SourceMode");
            CurrentBriefSong = sourceMode switch
            {
                0 => await _localSettingsService.ReadSettingAsync<BriefLocalSongInfo>(
                    "CurrentBriefSong"
                ),
                1 => await _localSettingsService.ReadSettingAsync<BriefCloudOnlineSongInfo>(
                    "CurrentBriefSong"
                ),
                _ => null,
            };
            (PlayQueue, ShuffledPlayQueue) = await FileManager.LoadPlayQueueDataAsync();
            _playQueueLength = PlayQueue.Count;
            if (CurrentBriefSong is not null)
            {
                CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
                    CurrentBriefSong
                );
                await SetSource(CurrentSong!.Path);
                _ = UpdateLyric(CurrentSong!.Lyric);
                _systemControls.IsPlayEnabled = true;
                _systemControls.IsPauseEnabled = true;
            }
            if (Data.NotFirstUsed)
            {
                CurrentVolume = await _localSettingsService.ReadSettingAsync<double>(
                    "CurrentVolume"
                );
                PlaySpeed = await _localSettingsService.ReadSettingAsync<double>("PlaySpeed");
            }
            else
            {
                CurrentVolume = 100;
                PlaySpeed = 1;
            }
            Data.RootPlayBarViewModel?.ButtonVisibility =
                CurrentSong is not null && PlayQueue.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability =
                CurrentSong is not null && PlayQueue.Count > 0;
        }
        catch (Exception ex)
        {
            CurrentBriefSong = null;
            CurrentSong = null;
            PlayQueue = [];
            ShuffledPlayQueue = [];
            Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability = false;
            Debug.WriteLine(ex.StackTrace);
        }
    }

    public void Dispose()
    {
        Stop();
        if (_tempoStream != 0)
        {
            Bass.StreamFree(_tempoStream);
            _tempoStream = 0;
        }
        if (_currentStream != 0)
        {
            Bass.StreamFree(_currentStream);
            _currentStream = 0;
        }
        Bass.Free();
        _currentCoverStream?.Dispose();
        _tempPlayer?.Dispose();
    }
}

[MemoryPackable]
public partial class IndexedPlayQueueSong
{
    public int Index { get; set; }
    public IBriefSongInfoBase Song { get; set; } = null!;

    [MemoryPackConstructor]
    public IndexedPlayQueueSong() { }

    public IndexedPlayQueueSong(int index, IBriefSongInfoBase song)
    {
        Index = index;
        Song = song;
    }
}
