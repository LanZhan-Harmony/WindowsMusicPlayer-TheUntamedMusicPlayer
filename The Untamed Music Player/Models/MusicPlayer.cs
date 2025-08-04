using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Wasapi;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using Windows.Media;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace The_Untamed_Music_Player.Models;

public partial class MusicPlayer : ObservableRecipient, IDisposable
{
    private readonly ILocalSettingsService _localSettingsService;

    private readonly Windows.Media.Playback.MediaPlayer _tempPlayer = new();

    /// <summary>
    /// 用于SMTC显示封面图片的流
    /// </summary>
    private static InMemoryRandomAccessStream? _currentCoverStream = null!;

    /// <summary>
    /// 当前播放的歌曲简要版
    /// </summary>
    private IBriefSongInfoBase? _currentBriefSong;

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
    /// 排序方式
    /// </summary>
    private byte _sortMode = 0;

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
    /// 原始音频流的频率
    /// </summary>
    private double _originalFrequency = 44100;

    /// <summary>
    /// 播放状态同步事件回调
    /// </summary>
    private SyncProcedure? _syncEndCallback;

    /// <summary>
    /// Bass下载流同步事件回调
    /// </summary>
    private readonly DownloadProcedure? _downloadCallback = null;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

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
    public partial ObservableCollection<IBriefSongInfoBase> PlayQueue { get; set; } = [];

    /// <summary>
    /// 随机播放队列集合
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IBriefSongInfoBase> ShuffledPlayQueue { get; set; } = [];

    /// <summary>
    /// 歌曲来源模式, 0为本地, 1为网易
    /// </summary>
    public byte SourceMode { get; set; } = 0;

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
        if (!IsMute && _currentStream != 0)
        {
            Bass.ChannelSetAttribute(_currentStream, ChannelAttribute.Volume, value / 100.0);
        }
    }

    /// <summary>
    /// 是否静音, true为静音, false为非静音
    /// </summary>
    [ObservableProperty]
    public partial bool IsMute { get; set; } = false;

    partial void OnIsMuteChanged(bool value)
    {
        if (_currentStream != 0)
        {
            Bass.ChannelSetAttribute(
                _currentStream,
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
        _localSettingsService = App.GetService<ILocalSettingsService>();

        // 初始化Bass音频库
        InitializeBass();

        // 初始化SMTC
        InitializeSystemMediaTransportControls();

        LoadCurrentStateAsync();
    }

    /// <summary>
    /// 初始化Bass音频库
    /// </summary>
    private void InitializeBass()
    {
        try
        {
            // 初始化Bass - 使用默认设备
            if (!Bass.Init(-1, 44100, DeviceInitFlags.Default, IntPtr.Zero))
            {
                Debug.WriteLine($"Bass初始化失败: {Bass.LastError}");
                return;
            }

            // 加载Bass插件
            LoadBassPlugins();

            // 设置同步回调
            _syncEndCallback = OnSyncEnd;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Bass初始化异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载Bass插件
    /// </summary>
    private static void LoadBassPlugins()
    {
        try
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
                if (File.Exists(fullPath))
                {
                    var plugin = Bass.PluginLoad(fullPath);
                    if (plugin == 0)
                    {
                        Debug.WriteLine($"加载Bass插件失败: {pluginPath} - {Bass.LastError}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载Bass插件时发生异常: {ex.Message}");
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
        _systemControls.IsEnabled = true;
        _systemControls.ButtonPressed += SystemControls_ButtonPressed;
        _timelineProperties.StartTime = TimeSpan.Zero;
        _timelineProperties.MinSeekTime = TimeSpan.Zero;
    }

    /// <summary>
    /// Bass同步回调 - 播放结束
    /// </summary>
    private void OnSyncEnd(int handle, int channel, int data, IntPtr user)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_lockable)
            {
                if (RepeatMode == 2)
                {
                    PlaySongByInfo(_currentBriefSong!);
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
    public async void PlaySongByInfo(IBriefSongInfoBase info)
    {
        Stop();
        PlayState = 2; // 设置为加载中状态
        _currentBriefSong = info;
        CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);
        PlayQueueIndex = info.PlayQueueIndex;
        if (CurrentSong!.IsPlayAvailable)
        {
            await SetSource(CurrentSong!.Path);
            _ = UpdateLyric(CurrentSong!.Lyric);
            _systemControls.IsPlayEnabled = true;
            _systemControls.IsPauseEnabled = true;
            Play();
        }
        else
        {
            HandleSongNotAvailable();
        }
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
        _currentBriefSong = songToPlay;
        CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay);
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
        queue.Insert(insertIndex, (IBriefSongInfoBase)info.Clone());
        _playQueueLength++;
        // 仅更新从插入位置之后的索引
        for (var i = insertIndex; i < queue.Count; i++)
        {
            queue[i].PlayQueueIndex = i;
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
            queue.Insert(insertIndex, (IBriefSongInfoBase)song.Clone());
            insertIndex++;
        }
        _playQueueLength += songs.Count();
        // 仅更新从插入位置之后的索引
        for (var i = insertIndex - songs.Count(); i < queue.Count; i++)
        {
            queue[i].PlayQueueIndex = i;
        }
    }

    /// <summary>
    /// 从播放队列中移除歌曲
    /// </summary>
    /// <param name="info"></param>
    public async Task RemoveSong(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.PlayQueueIndex;
        int newIndex;
        if (index == PlayQueueIndex) // 如果删除的歌曲正好是当前播放歌曲
        {
            Stop();
            newIndex = PlayQueueIndex < _playQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var songToPlay = ShuffleMode ? ShuffledPlayQueue[newIndex] : PlayQueue[newIndex];
            _currentBriefSong = songToPlay;
            CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay);
            PlayQueueIndex = newIndex == 0 ? 0 : newIndex - 1;
            if (PlayState != 0)
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
            queue[i].PlayQueueIndex = i;
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
    public void MoveUpSong(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.PlayQueueIndex;
        if (index > 0)
        {
            (queue[index - 1], queue[index]) = (queue[index], queue[index - 1]);
            queue[index].PlayQueueIndex = index;
            queue[index - 1].PlayQueueIndex = index - 1;
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
    public void MoveDownSong(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.PlayQueueIndex;
        if (index < queue.Count - 1)
        {
            (queue[index + 1], queue[index]) = (queue[index], queue[index + 1]);
            queue[index].PlayQueueIndex = index;
            queue[index + 1].PlayQueueIndex = index + 1;
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
            if (_tempoStream != 0 && _tempoStream != _currentStream)
            {
                Bass.StreamFree(_tempoStream);
                _tempoStream = 0;
            }
            if (_currentStream != 0)
            {
                Bass.StreamFree(_currentStream);
                _currentStream = 0;
            }

            // 根据来源模式创建Bass流
            if (SourceMode == 0) // 本地文件
            {
                _currentStream = Bass.CreateStream(path, 0, 0, BassFlags.Decode);
            }
            else // 在线流
            {
                _currentStream = Bass.CreateStream(
                    path,
                    0,
                    BassFlags.Decode,
                    _downloadCallback,
                    IntPtr.Zero
                );
            }
            if (_currentStream == 0)
            {
                Debug.WriteLine($"创建Bass流失败: {Bass.LastError}");
                PlayState = 0; // 恢复为暂停状态
                return;
            }

            // 获取原始频率
            var info = new ChannelInfo();
            Bass.ChannelGetInfo(_currentStream, out info);
            _originalFrequency = info.Frequency;
            Debug.WriteLine($"原始流频率: {_originalFrequency} Hz");

            // 使用BassFx创建Tempo流用于变速不变调
            _tempoStream = BassFx.TempoCreate(_currentStream, BassFlags.FxFreeSource);
            if (_tempoStream != 0)
            {
                Debug.WriteLine($"成功创建Tempo流: {_tempoStream}");

                // 设置初始播放速度
                var tempoPercent = (PlaySpeed - 1.0) * 100.0;
                var result = Bass.ChannelSetAttribute(
                    _tempoStream,
                    ChannelAttribute.Tempo,
                    (float)tempoPercent
                );
                Debug.WriteLine($"设置Tempo属性: {tempoPercent}%, 结果: {result}");
                if (!result)
                {
                    Debug.WriteLine($"设置Tempo失败: {Bass.LastError}");
                }

                // 设置播放结束回调
                Bass.ChannelSetSync(_tempoStream, SyncFlags.End, 0, _syncEndCallback, IntPtr.Zero);
            }
            else
            {
                Debug.WriteLine($"创建Tempo流失败: {Bass.LastError}，使用原始流");
                // 如果创建Tempo流失败，则使用原始流
                _tempoStream = _currentStream;
                Bass.ChannelSetSync(
                    _currentStream,
                    SyncFlags.End,
                    0,
                    _syncEndCallback,
                    IntPtr.Zero
                );
            }

            // 设置音量
            Bass.ChannelSetAttribute(
                _tempoStream,
                ChannelAttribute.Volume,
                IsMute ? 0 : CurrentVolume / 100.0
            );

            // 获取歌曲时长
            var lengthBytes = Bass.ChannelGetLength(_currentStream);
            var lengthSeconds = Bass.ChannelBytes2Seconds(_currentStream, lengthBytes);
            TotalPlayingTime = TimeSpan.FromSeconds(lengthSeconds);

            _displayUpdater.MusicProperties.Title = CurrentSong!.Title;
            _displayUpdater.MusicProperties.Artist =
                CurrentSong.ArtistsStr == "SongInfo_UnknownArtist".GetLocalized()
                    ? ""
                    : CurrentSong.ArtistsStr;
            _timelineProperties.MaxSeekTime = TotalPlayingTime;
            _timelineProperties.EndTime = TotalPlayingTime;

            PositionUpdateTimer250ms = ThreadPoolTimer.CreatePeriodicTimer(
                UpdateTimerHandler250ms,
                TimeSpan.FromMilliseconds(250)
            );

            if (ShuffleMode)
            {
                // 更新当前歌曲在随机播放队列中的索引
                PlayQueueIndex =
                    ShuffledPlayQueue
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Path == CurrentSong.Path
                        )
                        ?.PlayQueueIndex ?? 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
            PlayState = 0; // 恢复为暂停状态
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
            if (SourceMode == 0)
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
            else
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
    public void SetPlayList(
        string name,
        IEnumerable<IBriefSongInfoBase> list,
        byte sourceMode = 0,
        byte sortMode = 0
    )
    {
        if (
            PlayQueue.Count != list.Count()
            || PlayQueueName != name
            || _sortMode != sortMode
            || SourceMode != sourceMode
        )
        {
            _sortMode = sortMode;
            SourceMode = sourceMode;
            PlayQueueName = name;
            PlayQueue = [.. list];
            _playQueueLength = list.Count();

            if (!ShuffleMode)
            {
                // 更新播放队列中每首歌曲的 PlayQueueIndex 为实际位置
                for (var i = 0; i < PlayQueue.Count; i++)
                {
                    PlayQueue[i].PlayQueueIndex = i;
                }
            }
            else
            {
                UpdateShufflePlayQueue();
                // 更新随机播放队列中每首歌曲的 PlayQueueIndex 为实际位置
                for (var i = 0; i < ShuffledPlayQueue.Count; i++)
                {
                    ShuffledPlayQueue[i].PlayQueueIndex = i;
                }
            }
            FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
        }
    }

    public void SetShuffledPlayList(
        string name,
        IEnumerable<IBriefSongInfoBase> list,
        byte sourceMode = 0,
        byte sortMode = 0
    )
    {
        if (
            ShuffleMode == false
            || PlayQueue.Count != list.Count()
            || PlayQueueName != name
            || _sortMode != sortMode
            || SourceMode != sourceMode
        )
        {
            _sortMode = sortMode;
            SourceMode = sourceMode;
            ShuffleMode = true;
            PlayQueueName = name;
            PlayQueue = [.. list];
            _playQueueLength = list.Count();

            UpdateShufflePlayQueue();
            for (var i = 0; i < ShuffledPlayQueue.Count; i++)
            {
                ShuffledPlayQueue[i].PlayQueueIndex = i;
            }
        }
        FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 计时器更新事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerHandler250ms(ThreadPoolTimer timer)
    {
        try
        {
            if (_currentStream == 0 || _lockable || PlayState != 1)
            {
                return;
            }

            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                var positionBytes = Bass.ChannelGetPosition(_currentStream);
                var positionSeconds = Bass.ChannelBytes2Seconds(_currentStream, positionBytes);
                CurrentPlayingTime = TimeSpan.FromSeconds(positionSeconds);

                if (TotalPlayingTime.TotalMilliseconds > 0)
                {
                    CurrentPosition =
                        100
                        * (
                            CurrentPlayingTime.TotalMilliseconds
                            / TotalPlayingTime.TotalMilliseconds
                        );
                }
            });

            var dispatcherQueue =
                Data.LyricPage?.DispatcherQueue ?? Data.DesktopLyricWindow?.DispatcherQueue;
            if (CurrentLyric.Count > 0)
            {
                dispatcherQueue?.TryEnqueue(() =>
                {
                    var positionBytes = Bass.ChannelGetPosition(_currentStream);
                    var positionSeconds = Bass.ChannelBytes2Seconds(_currentStream, positionBytes);
                    UpdateCurrentLyricIndex(positionSeconds * 1000);
                });
            }

            _timelineProperties.Position = CurrentPlayingTime;
            _systemControls.UpdateTimelineProperties(_timelineProperties);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
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

    private void HandleSongNotAvailable()
    {
        if (PlayQueue.Count == 0)
        {
            return;
        }
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || SourceMode != 0)
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
        var streamToPlay = _tempoStream != 0 ? _tempoStream : _currentStream;
        if (streamToPlay != 0)
        {
            Bass.ChannelPlay(streamToPlay, false);
            PlayState = 1;
            _systemControls.PlaybackStatus = MediaPlaybackStatus.Playing;
        }
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        var streamToPlay = _tempoStream != 0 ? _tempoStream : _currentStream;
        if (streamToPlay != 0)
        {
            Bass.ChannelPause(streamToPlay);
            PlayState = 0;
            _systemControls.PlaybackStatus = MediaPlaybackStatus.Paused;
        }
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        var streamToPlay = _tempoStream != 0 ? _tempoStream : _currentStream;
        if (streamToPlay != 0)
        {
            Bass.ChannelStop(streamToPlay);
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
        if (PlayState == 1 || PlayState == 2)
        {
            Pause();
        }
        else
        {
            Play();
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
                ShuffledPlayQueue[i].PlayQueueIndex = i;
            }
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    ShuffledPlayQueue
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Path == CurrentSong.Path
                        )
                        ?.PlayQueueIndex ?? 0;
            }
        }
        else
        {
            ShuffledPlayQueue.Clear();
            for (var i = 0; i < PlayQueue.Count; i++)
            {
                PlayQueue[i].PlayQueueIndex = i;
            }
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    PlayQueue
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Path == CurrentSong.Path
                        )
                        ?.PlayQueueIndex ?? 0;
            }
        }
        FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 循环播放模式更新
    /// </summary>
    public void RepeatModeUpdate()
    {
        RepeatMode = (byte)((RepeatMode + 1) % 3);
    }

    /// <summary>
    /// 随机播放队列更新
    /// </summary>
    /// <returns></returns>
    public void UpdateShufflePlayQueue()
    {
        ShuffledPlayQueue = new ObservableCollection<IBriefSongInfoBase>(
            [.. PlayQueue.OrderBy(x => Guid.NewGuid())]
        );
    }

    /// <summary>
    /// 清空播放队列
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
        _currentBriefSong = null;
        CurrentSong = null;
        CurrentPlayingTime = TimeSpan.Zero;
        TotalPlayingTime = TimeSpan.Zero;
        PlayQueue.Clear();
        ShuffledPlayQueue.Clear();
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
        FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
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
        if (_currentStream != 0)
        {
            var targetTimeSeconds = ((Slider)sender).Value * TotalPlayingTime.TotalSeconds / 100;
            var targetBytes = Bass.ChannelSeconds2Bytes(_currentStream, targetTimeSeconds);
            Bass.ChannelSetPosition(_currentStream, targetBytes);

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
        if (_currentStream != 0)
        {
            var targetTimeSeconds = time / 1000.0;
            var targetBytes = Bass.ChannelSeconds2Bytes(_currentStream, targetTimeSeconds);
            Bass.ChannelSetPosition(_currentStream, targetBytes);

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
        if (_currentStream != 0)
        {
            var currentPositionBytes = Bass.ChannelGetPosition(_currentStream);
            var currentPositionSeconds = Bass.ChannelBytes2Seconds(
                _currentStream,
                currentPositionBytes
            );
            var newPositionSeconds = Math.Max(0, currentPositionSeconds - 10);
            var newPositionBytes = Bass.ChannelSeconds2Bytes(_currentStream, newPositionSeconds);

            Bass.ChannelSetPosition(_currentStream, newPositionBytes);
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
        if (_currentStream != 0)
        {
            var currentPositionBytes = Bass.ChannelGetPosition(_currentStream);
            var currentPositionSeconds = Bass.ChannelBytes2Seconds(
                _currentStream,
                currentPositionBytes
            );
            var newPositionSeconds = Math.Min(
                TotalPlayingTime.TotalSeconds,
                currentPositionSeconds + 30
            );
            var newPositionBytes = Bass.ChannelSeconds2Bytes(_currentStream, newPositionSeconds);

            Bass.ChannelSetPosition(_currentStream, newPositionBytes);
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

    /// <summary>
    /// 保存当前播放状态至设置存储
    /// </summary>
    public async void SaveCurrentStateAsync()
    {
        await _localSettingsService.SaveSettingAsync("PlayQueueIndex", PlayQueueIndex);
        await _localSettingsService.SaveSettingAsync("SourceMode", SourceMode);
        await _localSettingsService.SaveSettingAsync("ShuffleMode", ShuffleMode);
        await _localSettingsService.SaveSettingAsync("RepeatMode", RepeatMode);
        await _localSettingsService.SaveSettingAsync("IsMute", IsMute);
        await _localSettingsService.SaveSettingAsync("CurrentVolume", CurrentVolume);
        await _localSettingsService.SaveSettingAsync("PlaySpeed", PlaySpeed);
        await _localSettingsService.SaveSettingAsync("CurrentBriefSong", _currentBriefSong);
    }

    /// <summary>
    /// 从设置存储中读取当前播放状态
    /// </summary>
    public async void LoadCurrentStateAsync()
    {
        try
        {
            PlayQueueIndex = await _localSettingsService.ReadSettingAsync<int>("PlayQueueIndex");
            SourceMode = await _localSettingsService.ReadSettingAsync<byte>("SourceMode");
            ShuffleMode = await _localSettingsService.ReadSettingAsync<bool>("ShuffleMode");
            RepeatMode = await _localSettingsService.ReadSettingAsync<byte>("RepeatMode");
            IsMute = await _localSettingsService.ReadSettingAsync<bool>("IsMute");
            _currentBriefSong = SourceMode switch
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
            if (_currentBriefSong is not null)
            {
                CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
                    _currentBriefSong
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
                CurrentSong is not null && PlayQueue.Any()
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability = CurrentSong is not null && PlayQueue.Any();
        }
        catch (Exception ex)
        {
            _currentBriefSong = null;
            CurrentSong = null;
            PlayQueue = [];
            ShuffledPlayQueue = [];
            Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability = false;
            Debug.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的具体实现
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    PositionUpdateTimer250ms?.Cancel();
                    PositionUpdateTimer250ms = null;

                    if (_tempoStream != 0 && _tempoStream != _currentStream)
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
                catch (Exception ex)
                {
                    Debug.WriteLine($"释放Bass资源时出错: {ex.Message}");
                }
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// 设置播放速度
    /// </summary>
    /// <param name="speed"></param>
    private void SetPlaybackSpeed(double speed)
    {
        Debug.WriteLine($"设置播放速度: {speed}");

        if (_tempoStream != 0 && _tempoStream != _currentStream)
        {
            try
            {
                // 使用Tempo属性来改变播放速度而不改变音调
                // Tempo属性的值以百分比表示: 0=正常速度, 100=2倍速度, -50=0.5倍速度
                var tempoPercent = (speed - 1.0) * 100.0;
                Debug.WriteLine($"计算的Tempo百分比: {tempoPercent}%");

                var result = Bass.ChannelSetAttribute(
                    _tempoStream,
                    ChannelAttribute.Tempo,
                    (float)tempoPercent
                );
                Debug.WriteLine($"设置Tempo结果: {result}");

                if (!result)
                {
                    Debug.WriteLine($"设置Tempo失败: {Bass.LastError}");
                    // 作为备选方案，尝试设置频率
                    Bass.ChannelSetAttribute(
                        _currentStream,
                        ChannelAttribute.Frequency,
                        (float)(_originalFrequency * speed)
                    );
                }
                else
                {
                    Debug.WriteLine($"成功设置Tempo: {tempoPercent}%");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置Tempo流速度异常: {ex.Message}");
                // 回退方案：使用频率调节
                Bass.ChannelSetAttribute(
                    _currentStream,
                    ChannelAttribute.Frequency,
                    (float)(_originalFrequency * speed)
                );
            }
        }
        else if (_currentStream != 0)
        {
            Debug.WriteLine("使用频率调节播放速度（回退方案）");
            // 回退方案：如果没有tempo流，使用频率调节（但会改变音调）
            Bass.ChannelSetAttribute(
                _currentStream,
                ChannelAttribute.Frequency,
                (float)(_originalFrequency * speed)
            );
        }
        else
        {
            Debug.WriteLine("没有可用的音频流");
        }
    }
}
