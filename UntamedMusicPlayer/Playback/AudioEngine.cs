using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Services;
using Windows.Media.Playback;
using ZLogger;

namespace UntamedMusicPlayer.Playback;

public sealed partial class AudioEngine : IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<AudioEngine>();
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly SharedPlaybackState _state;
    private NativePlaybackCallback? _playbackEndedNativeCallback;
    private NativePlaybackCallback? _playbackFailedNativeCallback;

    // 专用播放线程相关
    private readonly Thread _playbackThread;
    private readonly BlockingCollection<Action> _taskQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private volatile bool _isDisposed = false;
    private bool _hasLoadedSong = false;

    public event Action? PlaybackEnded;
    public event Action? PlaybackFailed;

    public AudioEngine(SharedPlaybackState state)
    {
        _state = state;

        // 初始化任务队列和取消令牌
        _taskQueue = [with(new ConcurrentQueue<Action>())];
        _cancellationTokenSource = new CancellationTokenSource();

        // 创建并启动专用播放线程
        _playbackThread = new Thread(PlaybackThreadProc)
        {
            Name = "Bass Playback Thread",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
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
        catch (OperationCanceledException) { }
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
        if (Thread.CurrentThread == _playbackThread)
        {
            action();
        }
        else
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
        if (Thread.CurrentThread == _playbackThread)
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
        _playbackEndedNativeCallback = OnPlaybackEnded;
        _playbackFailedNativeCallback = OnPlaybackFailed;
        NativeMethods.SetCallbacks(_playbackEndedNativeCallback, _playbackFailedNativeCallback);

        if (!NativeMethods.Initialize())
        {
            if (NativeMethods.IsLastErrorBusy())
            {
                _logger.PlaybackDeviceBusy();
                return false;
            }
            _logger.ZLogInformation($"Bass初始化失败, 错误码: {NativeMethods.GetLastError()}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 播放结束回调
    /// </summary>
    private void OnPlaybackEnded() => _dispatcher.TryEnqueue(() => PlaybackEnded?.Invoke());

    /// <summary>
    /// 播放失败回调
    /// </summary>
    private void OnPlaybackFailed() => _dispatcher.TryEnqueue(() => PlaybackFailed?.Invoke());

    /// <summary>
    /// 载入要播放的歌曲
    /// </summary>
    public bool LoadSong() =>
        ExecuteOnPlaybackThread(() =>
        {
            _hasLoadedSong = false;
            if (_state.CurrentSong is null)
            {
                return false;
            }

            var success = NativeMethods.LoadSong(
                _state.CurrentSong.Path,
                _state.CurrentSong.IsOnline,
                _state.IsExclusiveMode,
                _state.IsMute ? 0.0 : _state.Volume / 100.0,
                _state.Speed,
                out var totalSeconds
            );

            if (!success)
            {
                _logger.ZLogInformation(
                    $"创建Bass流失败, 错误码: {NativeMethods.GetLastError()}, 文件: {_state.CurrentSong.Path}"
                );
                return false;
            }

            _hasLoadedSong = true;
            _dispatcher.TryEnqueue(() =>
                _state.TotalPlayingTime = TimeSpan.FromSeconds(totalSeconds)
            );
            return true;
        });

    /// <summary>
    /// 释放音频流
    /// </summary>
    private void FreeStreams()
    {
        NativeMethods.Stop();
        _hasLoadedSong = false;
    }

    /// <summary>
    /// 播放
    /// </summary>
    public bool Play() =>
        ExecuteOnPlaybackThread(() =>
        {
            if (!_hasLoadedSong)
            {
                return false;
            }

            if (NativeMethods.Play(_state.IsExclusiveMode))
            {
                return true;
            }

            if (NativeMethods.IsLastErrorBusy())
            {
                _logger.PlaybackDeviceBusy();
                return false;
            }

            if (_state.IsExclusiveMode)
            {
                _logger.SongPlaybackError(_state.CurrentSong!.Title);
                _logger.ZLogInformation(
                    $"独占播放失败, 错误码: {NativeMethods.GetLastError()}, 文件: {_state.CurrentSong.Path}"
                );
                return false;
            }

            _logger.ZLogInformation(
                $"共享播放失败, 错误码: {NativeMethods.GetLastError()}, 文件: {_state.CurrentSong!.Path}"
            );
            return false;
        });

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause() =>
        ExecuteOnPlaybackThread(() => NativeMethods.Pause(_state.IsExclusiveMode));

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop() => ExecuteOnPlaybackThread(FreeStreams);

    /// <summary>
    /// 设置播放速度
    /// </summary>
    private void SetSpeed(double speed)
    {
        if (_hasLoadedSong)
        {
            NativeMethods.SetSpeed(speed);
        }
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    private void SetVolume(double volume)
    {
        if (_hasLoadedSong)
        {
            NativeMethods.SetVolume(volume);
        }
    }

    /// <summary>
    /// 设置独占模式
    /// </summary>
    public async Task SetExclusiveMode(bool isExclusive, bool isPlaying)
    {
        await ExecuteOnPlaybackThread(async () =>
        {
            _state.IsExclusiveMode = isExclusive;
            if (!_hasLoadedSong)
            {
                return;
            }
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
            SetPositionInternal(position);
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
                SetPositionInternal(newPosition);
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
                SetPositionInternal(newPosition);
            }
        });

    /// <summary>
    /// 获取当前播放位置（秒）
    /// </summary>
    private double GetPositionSeconds() => _hasLoadedSong ? NativeMethods.GetPositionSeconds() : -1;

    /// <summary>
    /// 设置播放位置（秒）
    /// </summary>
    public void SetPosition(double targetSeconds) =>
        ExecuteOnPlaybackThread(() => SetPositionInternal(targetSeconds));

    /// <summary>
    /// 设置播放位置（秒）- 内部方法
    /// </summary>
    private void SetPositionInternal(double targetSeconds)
    {
        if (_hasLoadedSong)
        {
            NativeMethods.SetPositionSeconds(targetSeconds);
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

        _state.PropertyChanged -= OnStateChanged;

        ExecuteOnPlaybackThread(() =>
        {
            FreeStreams();
            NativeMethods.SetCallbacks(null, null);
            NativeMethods.Shutdown();
            _playbackEndedNativeCallback = null;
            _playbackFailedNativeCallback = null;
        });

        _isDisposed = true;

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
