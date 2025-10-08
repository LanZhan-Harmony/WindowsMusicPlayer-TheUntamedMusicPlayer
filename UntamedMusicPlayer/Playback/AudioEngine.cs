using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Wasapi;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Services;
using Windows.Media.Playback;
using ZLogger;

namespace UntamedMusicPlayer.Playback;

public partial class AudioEngine : IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<AudioEngine>();
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly SharedPlaybackState _state;
    private int _mainHandle = 0;
    private int _fxHandle = 0;
    private bool _isWasapiInitialized = false;
    private SyncProcedure? _syncEndCallback;
    private SyncProcedure? _syncFailCallback;
    private WasapiProcedure? _wasapiProc;

    // 专用播放线程相关
    private readonly Thread _playbackThread;
    private readonly BlockingCollection<Action> _taskQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private volatile bool _isDisposed = false;

    public event Action? PlaybackEnded;
    public event Action? PlaybackFailed;

    public AudioEngine(SharedPlaybackState state)
    {
        _state = state;

        // 初始化任务队列和取消令牌
        _taskQueue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
        _cancellationTokenSource = new CancellationTokenSource();

        // 创建并启动专用播放线程
        _playbackThread = new Thread(PlaybackThreadProc)
        {
            Name = "Bass Playback Thread",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal, // 提高线程优先级以确保播放流畅
        };
        _playbackThread.Start();

        ExecuteOnPlaybackThread(InitializeBass);

        _state.PropertyChanged += OnStateChanged;
    }

    /// <summary>
    /// 播放线程处理过程
    /// </summary>
    private void PlaybackThreadProc()
    {
        try
        {
            foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                try
                {
                    task.Invoke();
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"播放线程任务执行失败");
                }
            }
        }
        catch (OperationCanceledException) { } // 正常退出
    }

    /// <summary>
    /// 在播放线程上执行操作
    /// </summary>
    private void ExecuteOnPlaybackThread(Action action)
    {
        if (_isDisposed)
        {
            return;
        }
        if (Thread.CurrentThread == _playbackThread) // 已经在播放线程上，直接执行
        {
            action();
        }
        else // 将任务添加到队列
        {
            _taskQueue.Add(action);
        }
    }

    /// <summary>
    /// 在播放线程上执行操作并等待结果
    /// </summary>
    private T ExecuteOnPlaybackThread<T>(Func<T> func)
    {
        if (_isDisposed)
        {
            return default!;
        }
        if (Thread.CurrentThread == _playbackThread) // 已经在播放线程上，直接执行
        {
            return func();
        }
        var tcs = new TaskCompletionSource<T>();
        _taskQueue.Add(() =>
        {
            try
            {
                var result = func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task.Result;
    }

    private void OnStateChanged(object? _, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedPlaybackState.Volume):
                if (!_state.IsMute)
                {
                    ExecuteOnPlaybackThread(() => SetVolume(_state.Volume / 100.0));
                }
                break;
            case nameof(SharedPlaybackState.IsMute):
                ExecuteOnPlaybackThread(() =>
                    SetVolume(_state.IsMute ? 0.0 : _state.Volume / 100.0)
                );
                break;
            case nameof(SharedPlaybackState.Speed):
                ExecuteOnPlaybackThread(() => SetSpeed(_state.Speed));
                break;
        }
    }

    /// <summary>
    /// 初始化Bass音频库
    /// </summary>
    private bool InitializeBass()
    {
        if (!Bass.Init()) // 初始化Bass - 使用默认设备
        {
            if (Bass.LastError == Errors.Busy)
            {
                _logger.PlaybackDeviceBusy();
                return false;
            }
            _logger.ZLogInformation($"Bass初始化失败: {Bass.LastError}");
            return false;
        }

        LoadBassPlugins();

        // 设置同步回调
        _syncEndCallback += OnPlaybackEnded;
        _syncFailCallback += OnPlaybackFailed;

        // 设置WASAPI回调
        _wasapiProc = WasapiProc;

        return true;
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
    /// 播放结束回调
    /// </summary>
    private void OnPlaybackEnded(int _1, int _2, int _3, nint _4) =>
        _dispatcher.TryEnqueue(() => PlaybackEnded?.Invoke());

    /// <summary>
    /// 播放失败回调
    /// </summary>
    private void OnPlaybackFailed(int _1, int _2, int _3, nint _4) =>
        _dispatcher.TryEnqueue(() => PlaybackFailed?.Invoke());

    /// <summary>
    /// WASAPI回调
    /// </summary>
    private int WasapiProc(nint buffer, int length, nint user) =>
        _fxHandle == 0 ? 0 : Bass.ChannelGetData(_fxHandle, buffer, length);

    /// <summary>
    /// 载入要播放的歌曲
    /// </summary>
    public bool LoadSong() =>
        ExecuteOnPlaybackThread(() =>
        {
            FreeStreams();
            if (!CreateStreams())
            {
                return false;
            }
            SetVolume(_state.IsMute ? 0.0 : _state.Volume / 100.0);
            SetSpeed(_state.Speed);
            return true;
        });

    /// <summary>
    /// 创建音频流
    /// </summary>
    private bool CreateStreams()
    {
        const BassFlags flags =
            BassFlags.Unicode | BassFlags.Float | BassFlags.AsyncFile | BassFlags.Decode;

        var path = _state.CurrentSong!.Path;
        _mainHandle = _state.CurrentSong.IsOnline
            ? Bass.CreateStream(path, 0, flags, null)
            : Bass.CreateStream(path, 0, 0, flags);
        if (_mainHandle == 0)
        {
            if (Bass.LastError == Errors.Init)
            {
                if (InitializeBass())
                {
                    return CreateStreams();
                }
                return false;
            }
            _logger.ZLogInformation($"创建Bass流失败: {Bass.LastError}, 文件: {path}");
        }

        _fxHandle = _state.IsExclusiveMode
            ? BassFx.TempoCreate(_mainHandle, BassFlags.Decode)
            : BassFx.TempoCreate(_mainHandle, BassFlags.FxFreeSource);
        if (_fxHandle == 0)
        {
            var error = Bass.LastError;
            _logger.ZLogInformation($"创建Tempo流失败: {error}");
            Bass.StreamFree(_mainHandle);
            _mainHandle = 0;
            return false;
        }

        Bass.ChannelSetSync(_fxHandle, SyncFlags.End, 0, _syncEndCallback);
        Bass.ChannelSetSync(_fxHandle, SyncFlags.Stalled, 0, _syncFailCallback);

        var lengthBytes = Bass.ChannelGetLength(_fxHandle);
        var lengthSeconds = Bass.ChannelBytes2Seconds(_fxHandle, lengthBytes);
        _dispatcher.TryEnqueue(() => _state.TotalPlayingTime = TimeSpan.FromSeconds(lengthSeconds));

        return true;
    }

    /// <summary>
    /// 释放音频流
    /// </summary>
    private void FreeStreams()
    {
        if (BassWasapi.IsStarted)
        {
            BassWasapi.Stop(true);
        }
        if (_isWasapiInitialized)
        {
            BassWasapi.Free();
        }
        _isWasapiInitialized = false;
        if (_fxHandle != 0)
        {
            Bass.StreamFree(_fxHandle);
            _fxHandle = 0;
        }
        if (_mainHandle != 0)
        {
            Bass.StreamFree(_mainHandle);
            _mainHandle = 0;
        }
    }

    /// <summary>
    /// 播放
    /// </summary>
    /// <returns></returns>
    public bool Play() =>
        ExecuteOnPlaybackThread(() =>
        {
            if (_fxHandle == 0)
            {
                return false;
            }
            if (_state.IsExclusiveMode) // 独占模式
            {
                if (_isWasapiInitialized) // 从暂停恢复
                {
                    if (!BassWasapi.Start())
                    {
                        _logger.ZLogInformation($"独占从暂停恢复播放失败: {Bass.LastError}");
                        return false;
                    }
                    return true;
                }
                if (!Bass.ChannelGetInfo(_fxHandle, out var channelInfo))
                {
                    _logger.ZLogInformation($"无法获取流信息: {Bass.LastError}");
                    return false;
                }
                if (
                    !BassWasapi.Init(
                        -1,
                        channelInfo.Frequency,
                        channelInfo.Channels,
                        WasapiInitFlags.Exclusive | WasapiInitFlags.EventDriven,
                        0.05f,
                        0,
                        _wasapiProc,
                        nint.Zero
                    )
                )
                {
                    if (Bass.LastError == Errors.Busy)
                    {
                        _logger.PlaybackDeviceBusy();
                        return false;
                    }
                    _logger.SongPlaybackError(_state.CurrentSong!.Title);
                    _logger.ZLogInformation($"独占初始化失败: {Bass.LastError}");
                    return false;
                }
                _isWasapiInitialized = true;
                if (!BassWasapi.Start())
                {
                    _logger.ZLogInformation($"独占播放失败: {Bass.LastError}");
                    return false;
                }
                return true;
            }
            // 共享模式：先尝试直接播放，失败时在 Start() 后重试一次
            if (Bass.ChannelPlay(_fxHandle, false))
            {
                return true;
            }
            if (Bass.LastError == Errors.Start && Bass.Start())
            {
                return Bass.ChannelPlay(_fxHandle, false);
            }

            _logger.ZLogInformation($"共享播放失败: {Bass.LastError}");
            return false;
        });

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause() =>
        ExecuteOnPlaybackThread(() =>
        {
            if (_fxHandle != 0)
            {
                if (_state.IsExclusiveMode)
                {
                    if (BassWasapi.IsStarted)
                    {
                        BassWasapi.Stop(false);
                    }
                    return;
                }
                Bass.ChannelPause(_fxHandle);
            }
        });

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop() =>
        ExecuteOnPlaybackThread(() =>
        {
            if (_fxHandle != 0)
            {
                if (_state.IsExclusiveMode)
                {
                    if (BassWasapi.IsStarted)
                    {
                        BassWasapi.Stop(true);
                    }
                    BassWasapi.Free();
                    _isWasapiInitialized = false;
                    return;
                }
                Bass.ChannelStop(_fxHandle);
            }
        });

    /// <summary>
    /// 设置播放速度
    /// </summary>
    /// <param name="speed"></param>
    private void SetSpeed(double speed)
    {
        if (_fxHandle != 0)
        {
            var tempoPercent = (speed - 1.0) * 100.0;
            Bass.ChannelSetAttribute(_fxHandle, ChannelAttribute.Tempo, (float)tempoPercent);
        }
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    /// <param name="volume"></param>
    private void SetVolume(double volume)
    {
        if (_fxHandle != 0)
        {
            Bass.ChannelSetAttribute(_fxHandle, ChannelAttribute.Volume, volume);
        }
    }

    /// <summary>
    /// 设置独占模式
    /// </summary>
    /// <param name="isExclusive"></param>
    /// <param name="isPlaying"></param>
    public async Task SetExclusiveMode(bool isExclusive, bool isPlaying)
    {
        await ExecuteOnPlaybackThread(async () =>
        {
            _state.IsExclusiveMode = isExclusive;
            var position = GetPositionSeconds();
            Stop();
            LoadSong();
            if (isPlaying)
            {
                if (!Play())
                {
                    _dispatcher.TryEnqueue(() => _state.PlayState = MediaPlaybackState.Paused);
                }
            }
            await SetPositionInternal(position);
        });
    }

    /// <summary>
    /// 随着计时器更新播放进度
    /// </summary>
    public async Task UpdatePosition()
    {
        var position = ExecuteOnPlaybackThread(GetPositionSeconds);
        if (position >= 0)
        {
            var tcs = new TaskCompletionSource<bool>();
            _dispatcher.TryEnqueue(() =>
            {
                _state.CurrentPlayingTime = TimeSpan.FromSeconds(position);
                tcs.SetResult(true);
            });
            await tcs.Task;
        }
    }

    /// <summary>
    /// 快退10秒
    /// </summary>
    public void SkipBack10s() =>
        ExecuteOnPlaybackThread(() =>
        {
            var currentPosition = GetPositionSeconds();
            if (currentPosition >= 0)
            {
                var newPosition = Math.Max(0, currentPosition - 10);
                _ = SetPositionInternal(newPosition);
            }
        });

    /// <summary>
    /// 快进30秒
    /// </summary>
    public void SkipForward30s() =>
        ExecuteOnPlaybackThread(() =>
        {
            var currentPosition = GetPositionSeconds();
            if (currentPosition >= 0)
            {
                var newPosition = Math.Min(
                    _state.TotalPlayingTime.TotalSeconds,
                    currentPosition + 30
                );
                _ = SetPositionInternal(newPosition);
            }
        });

    /// <summary>
    /// 获取当前播放位置（秒）
    /// </summary>
    private double GetPositionSeconds()
    {
        if (_fxHandle != 0)
        {
            var positionBytes = Bass.ChannelGetPosition(_fxHandle);
            var positionSeconds = Bass.ChannelBytes2Seconds(_fxHandle, positionBytes);
            return positionSeconds;
        }
        return -1;
    }

    /// <summary>
    /// 设置播放位置（秒）
    /// </summary>
    /// <param name="targetSeconds"></param>
    public void SetPosition(double targetSeconds) =>
        ExecuteOnPlaybackThread(() => SetPositionInternal(targetSeconds));

    /// <summary>
    /// 设置播放位置（秒）- 内部方法
    /// </summary>
    /// <param name="targetSeconds"></param>
    private async Task SetPositionInternal(double targetSeconds)
    {
        if (_fxHandle != 0)
        {
            var targetBytes = Bass.ChannelSeconds2Bytes(_fxHandle, targetSeconds);
            var result = Bass.ChannelSetPosition(_fxHandle, targetBytes);
            if (!result)
            {
                var error = Bass.LastError;
                if (error == Errors.Position)
                {
                    var retryCount = 0;
                    while (!result && retryCount < 20) // 最多重试20次
                    {
                        await Task.Delay(100);
                        result = Bass.ChannelSetPosition(_fxHandle, targetBytes);
                        retryCount++;
                    }
                }
            }
            _dispatcher.TryEnqueue(() =>
                _state.CurrentPlayingTime = TimeSpan.FromSeconds(targetSeconds)
            );
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _state.PropertyChanged -= OnStateChanged;

        // 在播放线程上执行清理
        ExecuteOnPlaybackThread(() =>
        {
            FreeStreams();
            Bass.Free();
            _syncEndCallback -= OnPlaybackEnded;
            _syncFailCallback -= OnPlaybackFailed;
        });

        // 停止播放线程
        _cancellationTokenSource.Cancel();
        _taskQueue.CompleteAdding();

        // 等待线程完成（最多等待2秒）
        if (!_playbackThread.Join(TimeSpan.FromSeconds(2)))
        {
            _logger.ZLogWarning($"播放线程未能在2秒内完成");
        }

        _taskQueue.Dispose();
        _cancellationTokenSource.Dispose();

        GC.SuppressFinalize(this);
    }
}
