using System.ComponentModel;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Wasapi;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Services;
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

    public event Action? PlaybackEnded;
    public event Action? PlaybackFailed;

    public AudioEngine(SharedPlaybackState state)
    {
        _state = state;
        InitializeBass();
        _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? _, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedPlaybackState.Volume):
                if (!_state.IsMute)
                {
                    SetVolume(_state.Volume / 100.0);
                }
                break;
            case nameof(SharedPlaybackState.IsMute):
                SetVolume(_state.IsMute ? 0.0 : _state.Volume / 100.0);
                break;
            case nameof(SharedPlaybackState.Speed):
                SetSpeed(_state.Speed);
                break;
        }
    }

    /// <summary>
    /// 初始化Bass音频库
    /// </summary>
    private void InitializeBass()
    {
        if (!Bass.Init()) // 初始化Bass - 使用默认设备
        {
            _logger.ZLogInformation($"Bass初始化失败: {Bass.LastError}");
            return;
        }

        LoadBassPlugins();

        // 设置同步回调
        _syncEndCallback += OnPlaybackEnded;
        _syncFailCallback += OnPlaybackFailed;

        // 设置WASAPI回调
        _wasapiProc = WasapiProc;
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

    private void OnPlaybackEnded(int _1, int _2, int _3, nint _4) =>
        _dispatcher.TryEnqueue(() => PlaybackEnded?.Invoke());

    private void OnPlaybackFailed(int _1, int _2, int _3, nint _4) =>
        _dispatcher.TryEnqueue(() => PlaybackFailed?.Invoke());

    /// <summary>
    /// WASAPI回调处理程序
    /// </summary>
    private int WasapiProc(nint buffer, int length, nint user)
    {
        if (_fxHandle != 0)
        {
            return Bass.ChannelGetData(_fxHandle, buffer, length);
        }
        return 0;
    }

    /// <summary>
    /// 载入要播放的歌曲
    /// </summary>
    public void LoadSong()
    {
        FreeStreams();
        CreateStreams();
        SetVolume(_state.IsMute ? 0.0 : _state.Volume / 100.0);
        SetSpeed(_state.Speed);
    }

    /// <summary>
    /// 创建音频流
    /// </summary>
    private void CreateStreams()
    {
        const BassFlags flags =
            BassFlags.Unicode | BassFlags.Float | BassFlags.AsyncFile | BassFlags.Decode;

        var path = _state.CurrentSong!.Path;
        _mainHandle = _state.CurrentSong.IsOnline
            ? Bass.CreateStream(path, 0, flags, null)
            : Bass.CreateStream(path, 0, 0, flags);
        if (_mainHandle == 0)
        {
            var error = Bass.LastError;
            _logger.ZLogInformation($"创建Bass流失败: {error}, 文件: {path}");
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
        }

        Bass.ChannelSetSync(_fxHandle, SyncFlags.End, 0, _syncEndCallback);
        Bass.ChannelSetSync(_fxHandle, SyncFlags.Stalled, 0, _syncFailCallback);

        var lengthBytes = Bass.ChannelGetLength(_fxHandle);
        var lengthSeconds = Bass.ChannelBytes2Seconds(_fxHandle, lengthBytes);
        _state.TotalPlayingTime = TimeSpan.FromSeconds(lengthSeconds);
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

    public bool Play()
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
        if (!Bass.ChannelPlay(_fxHandle, false)) // 共享模式
        {
            _logger.ZLogInformation($"共享播放失败: {Bass.LastError}");
            return false;
        }
        return true;
    }

    public void Pause()
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
    }

    public void Stop()
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
    }

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
    public void SetExclusiveMode(bool isExclusive, bool isPlaying)
    {
        _state.IsExclusiveMode = isExclusive;
        var position = GetPositionSeconds();
        Stop();
        Play();
        SetPosition(position);
    }

    /// <summary>
    /// 随着计时器更新播放进度
    /// </summary>
    public async Task UpdatePosition()
    {
        var position = GetPositionSeconds();
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

    public void SkipBack10s()
    {
        var currentPosition = GetPositionSeconds();
        if (currentPosition >= 0)
        {
            var newPosition = Math.Max(0, currentPosition - 10);
            SetPosition(newPosition);
        }
    }

    public void SkipForward30s()
    {
        var currentPosition = GetPositionSeconds();
        if (currentPosition >= 0)
        {
            var newPosition = Math.Min(_state.TotalPlayingTime.TotalSeconds, currentPosition + 30);
            SetPosition(newPosition);
        }
    }

    /// <summary>
    /// 获取当前播放位置（秒）
    /// </summary>
    /// <returns></returns>
    public double GetPositionSeconds()
    {
        if (_fxHandle != 0)
        {
            var positionBytes = Bass.ChannelGetPosition(_fxHandle);
            var positionSeconds = Bass.ChannelBytes2Seconds(_fxHandle, positionBytes);
            return positionSeconds;
        }
        return -1;
    }

    public async void SetPosition(double targetSeconds)
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
            _state.CurrentPlayingTime = TimeSpan.FromSeconds(targetSeconds);
        }
    }

    public void Dispose()
    {
        FreeStreams();
        Bass.Free();
        _syncEndCallback -= OnPlaybackEnded;
        _syncFailCallback -= OnPlaybackFailed;
        _state.PropertyChanged -= OnStateChanged;
        GC.SuppressFinalize(this);
    }
}
