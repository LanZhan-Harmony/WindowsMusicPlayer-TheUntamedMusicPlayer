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
    private int _currentStream = 0;
    private int _fxStream = 0;
    private int _wasapiDevice = -1;
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
        if (_fxStream == 0)
        {
            return 0;
        }
        var bytesRead = Bass.ChannelGetData(_fxStream, buffer, length);
        if (bytesRead == -1 || bytesRead == 0)
        {
            return 0;
        }
        return bytesRead;
    }

    public void LoadSong()
    {
        FreeStreams();
        CreateStream();
        SetVolume(_state.IsMute ? 0.0 : _state.Volume / 100.0);
    }

    /// <summary>
    /// 从文件路径创建音频流
    /// </summary>
    /// <param name="path">文件路径</param>
    private void CreateStream()
    {
        FreeStreams();
        const BassFlags flags =
            BassFlags.Unicode | BassFlags.Float | BassFlags.AsyncFile | BassFlags.Decode;

        var path = _state.CurrentSong!.Path;
        _currentStream = _state.CurrentSong.IsOnline
            ? Bass.CreateStream(path, 0, flags, null)
            : Bass.CreateStream(path, 0, 0, flags);
        if (_currentStream == 0)
        {
            var error = Bass.LastError;
            _logger.ZLogInformation($"创建Bass流失败: {error}, 文件: {path}");
        }

        _fxStream = _state.IsExclusiveMode
            ? BassFx.TempoCreate(_currentStream, BassFlags.Decode)
            : BassFx.TempoCreate(_currentStream, BassFlags.FxFreeSource);
        if (_fxStream == 0)
        {
            var error = Bass.LastError;
            _logger.ZLogInformation($"创建Tempo流失败: {error}");
            Bass.StreamFree(_currentStream);
            _currentStream = 0;
        }

        SetSpeed(_state.Speed);

        Bass.ChannelSetSync(_fxStream, SyncFlags.End, 0, _syncEndCallback);
        Bass.ChannelSetSync(_fxStream, SyncFlags.Stalled, 0, _syncFailCallback);

        var lengthBytes = Bass.ChannelGetLength(_fxStream);
        var lengthSeconds = Bass.ChannelBytes2Seconds(_fxStream, lengthBytes);
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
        if (_wasapiDevice != -1)
        {
            BassWasapi.Free();
            _wasapiDevice = -1;
        }
        if (_fxStream != 0)
        {
            Bass.StreamFree(_fxStream);
            _fxStream = 0;
        }
        if (_currentStream != 0)
        {
            Bass.StreamFree(_currentStream);
            _currentStream = 0;
        }
    }

    public bool Play()
    {
        if (_fxStream == 0)
        {
            return false;
        }

        return Bass.ChannelPlay(_fxStream, false);
    }

    public bool Pause()
    {
        if (_fxStream == 0)
        {
            return false;
        }

        return Bass.ChannelPause(_fxStream);
    }

    public bool Stop()
    {
        if (_fxStream == 0)
        {
            return false;
        }

        return Bass.ChannelStop(_fxStream);
    }

    /// <summary>
    /// 设置播放速度
    /// </summary>
    /// <param name="speed"></param>
    private void SetSpeed(double speed)
    {
        if (_fxStream != 0)
        {
            var tempoPercent = (speed - 1.0) * 100.0;
            Bass.ChannelSetAttribute(_fxStream, ChannelAttribute.Tempo, (float)tempoPercent);
        }
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    /// <param name="volume"></param>
    private void SetVolume(double volume)
    {
        if (_fxStream != 0)
        {
            Bass.ChannelSetAttribute(_fxStream, ChannelAttribute.Volume, (float)volume);
        }
    }

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
        if (_fxStream != 0)
        {
            var positionBytes = Bass.ChannelGetPosition(_fxStream);
            var positionSeconds = Bass.ChannelBytes2Seconds(_fxStream, positionBytes);
            return positionSeconds;
        }
        return -1;
    }

    public async void SetPosition(double targetSeconds)
    {
        if (_fxStream != 0)
        {
            var targetBytes = Bass.ChannelSeconds2Bytes(_fxStream, targetSeconds);
            var result = Bass.ChannelSetPosition(_fxStream, targetBytes);
            if (!result)
            {
                var error = Bass.LastError;
                if (error == Errors.Position)
                {
                    var retryCount = 0;
                    while (!result && retryCount < 20) // 最多重试20次
                    {
                        await Task.Delay(100);
                        result = Bass.ChannelSetPosition(_fxStream, targetBytes);
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
