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

        // 如果动态压力极大，强行降级
        if (isUnderHeavyPressure)
        {
            var period = ((float)gcPauseMs + 50) / 1000;
            return (period * 4, period); // 极度保守模式
        }

        return _staticTier switch
        {
            DevicePerformanceTier.Pro => (0.16f, 0.04f),
            DevicePerformanceTier.Balanced => (0.25f, 0.0625f),
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
        var info = GC.GetGCMemoryInfo();

        // 获取最后一次 GC 的总停顿时间
        var totalPause = 0d;
        foreach (var pause in info.PauseDurations)
        {
            totalPause += pause.TotalMilliseconds;
        }

        // 计算内存压力百分比
        var loadPercentage = (float)info.MemoryLoadBytes / info.TotalAvailableMemoryBytes * 100;

        // 3. 判定标准
        // - 停顿 > 100ms (音频断流风险)
        // - 或者内存占用 > 95% (系统交换压力)
        // - 或者全进程 GC 时间占比过高 (info.PauseTimePercentage > 5)
        var isHeavy = totalPause > 100 || loadPercentage > 95 || info.PauseTimePercentage > 5;

        return (isHeavy, totalPause);
    }

    private static long GetTotalMemoryGb() =>
        GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024;
}
