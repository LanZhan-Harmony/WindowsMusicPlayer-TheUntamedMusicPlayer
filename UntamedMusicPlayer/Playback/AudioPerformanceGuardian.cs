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

    /// <summary>
    /// 获取音频配置建议
    /// </summary>
    public static (float Buffer, float Period) GetRecommendedSettings()
    {
        var (isUnderHeavyPressure, gcPauseMs) = GetRuntimePressure();

        // 如果动态压力极大（正在频繁 GC 或 CPU 爆满），强行降级
        if (isUnderHeavyPressure)
        {
            var period = ((float)gcPauseMs + 50) / 1000;
            return (period * 4, period); // 极度保守模式
        }

        return _staticTier switch
        {
            DevicePerformanceTier.Pro => (0.12f, 0.03f),
            DevicePerformanceTier.Balanced => (0.2f, 0.05f),
            _ => (0.4f, 0.1f),
        };
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
        var gcInfo = GC.GetGCMemoryInfo();

        // 获取最后一次 GC 的暂停时间 (毫秒)
        var lastPause = gcInfo.PauseDurations[0].TotalMilliseconds;

        // 压力判断逻辑：
        // 1. 如果上次 GC 暂停超过 100ms (对音频是致命的)
        // 2. 或者当前进程内存占用过载
        var isHeavy = lastPause > 100 || gcInfo.MemoryLoadBytes > 85;

        return (isHeavy, lastPause);
    }

    private static long GetTotalMemoryGb()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            return gcInfo.TotalAvailableMemoryBytes / 1024 / 1024 / 1024;
        }
        catch
        {
            return 8; // 默认保守值
        }
    }
}
