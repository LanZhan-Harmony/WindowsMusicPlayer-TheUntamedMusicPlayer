namespace UntamedMusicPlayer.Playback;

public enum DevicePerformanceTier
{
    Entry,
    Balanced,
    Pro,
}

public static class AudioPerformanceGuardian
{
    private static readonly int _processorCount = Environment.ProcessorCount;
    private static readonly long _totalMemoryGb = GetTotalMemoryGb();
    private static readonly DevicePerformanceTier _staticTier = AssessStaticTier();

    private static long _lastGcIndex = -1;
    private static DateTime _lastGcTimestamp = DateTime.MinValue;
    private static double _lastGcPauseMs = 0;

    private static float _recommendedBuffer;
    private static float _recommendedPeriod;
    private static readonly Timer _timer;

    static AudioPerformanceGuardian()
    {
        RefreshSettings();
        _timer = new Timer(
            _ => RefreshSettings(),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10)
        );
    }

    /// <summary>
    /// 获取音频配置建议
    /// </summary>
    public static (float Buffer, float Period) GetRecommendedSettings() =>
        (_recommendedBuffer, _recommendedPeriod);

    private static void RefreshSettings()
    {
        var (isUnderHeavyPressure, gcPauseMs) = GetRuntimePressure();

        // 如果动态压力极大，强行降级
        if (isUnderHeavyPressure)
        {
            var period = ((float)gcPauseMs + 50) / 1000;
            _recommendedBuffer = period * 4;
            _recommendedPeriod = period;
        }
        else
        {
            (_recommendedBuffer, _recommendedPeriod) = _staticTier switch
            {
                DevicePerformanceTier.Pro => (0.16f, 0.04f),
                DevicePerformanceTier.Balanced => (0.25f, 0.0625f),
                _ => (0.4f, 0.1f),
            };
        }
    }

    private static DevicePerformanceTier AssessStaticTier()
    {
        if (_processorCount >= 12 && _totalMemoryGb >= 24)
        {
            return DevicePerformanceTier.Pro;
        }
        if (_processorCount >= 6 && _totalMemoryGb >= 8)
        {
            return DevicePerformanceTier.Balanced;
        }
        return DevicePerformanceTier.Entry;
    }

    private static (bool IsUnderHeavyPressure, double GcPauseMs) GetRuntimePressure()
    {
        var info = GC.GetGCMemoryInfo();
        var now = DateTime.UtcNow;

        // 1. 获取最后一次 GC 的总停顿时间
        var currentPause = 0d;
        foreach (var pause in info.PauseDurations)
        {
            currentPause += pause.TotalMilliseconds;
        }

        // 2. 更新 GC 追踪状态
        if (info.Index > _lastGcIndex)
        {
            _lastGcIndex = info.Index;
            _lastGcPauseMs = currentPause;
            _lastGcTimestamp = now;
        }

        // 3. 判定该停顿是否依然“有效”（例如 20 秒内）
        var isPauseRecent = (now - _lastGcTimestamp).TotalSeconds < 20;
        var effectivePause = isPauseRecent ? _lastGcPauseMs : 0;

        // 4. 计算内存压力百分比
        var loadPercentage = (float)info.MemoryLoadBytes / info.TotalAvailableMemoryBytes * 100;

        // 5. 判定标准
        // - 最近发生的停顿 > 100ms (音频断流风险)
        // - 或者内存占用 > 95% (系统交换压力)
        // - 或者全进程 GC 时间占比过高 (info.PauseTimePercentage > 5)
        var isHeavy = effectivePause > 100 || loadPercentage > 95 || info.PauseTimePercentage > 5;

        return (isHeavy, effectivePause);
    }

    private static long GetTotalMemoryGb() =>
        GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024;
}
