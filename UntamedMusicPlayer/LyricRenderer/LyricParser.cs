using System.Globalization;
using System.Text.RegularExpressions;
using ZLinq;

namespace UntamedMusicPlayer.LyricRenderer;

public static partial class LyricParser
{
    [GeneratedRegex(@".*\](.*)", RegexOptions.Compiled)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\[\s*([0-9.:]+)\s*\]", RegexOptions.Compiled)]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"<\s*([0-9.:]+)\s*>", RegexOptions.Compiled)]
    private static partial Regex EnhancedTimeRegex();

    [GeneratedRegex(@"(?:\[\s*[0-9.:]+\s*\]|<\s*[0-9.:]+\s*>)", RegexOptions.Compiled)]
    private static partial Regex AllTimesRegex();

    [GeneratedRegex(@"\[offset:\s*([+-]?\d+)\]", RegexOptions.Compiled)]
    private static partial Regex OffsetRegex();

    /// <summary>
    /// 歌词片段组，包含一个或多个同时出现的歌词内容，以及一个可选的结束时间
    /// </summary>
    private sealed class LyricSliceGroup
    {
        public List<string> Contents { get; } = [];
        public double? EndTime { get; set; }
    }

    /// <summary>
    /// 解析歌词文本并返回歌词片段列表
    /// </summary>
    /// <param name="lyric">LRC格式的歌词文本</param>
    /// <param name="duration">歌曲时长</param>
    /// <returns>按时间排序的歌词片段列表</returns>
    public static async Task<List<LyricSlice>> GetLyricSlices(string lyric, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(lyric))
        {
            return [];
        }

        return await Task.Run(() =>
        {
            var lines = lyric.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            var offset = 0.0;
            var enhanced = lines.Any(line => line.Contains('<')); // 包含尖括号的行表示是增强LRC
            var timeGroupedLyrics = new Dictionary<double, LyricSliceGroup>(); // 歌词片段组字典，键为开始时间，值为歌词片段组

            if (enhanced)
            {
                ParseEnhancedLrcLines(lines, ref offset, timeGroupedLyrics);
            }
            else
            {
                ParseNormalLrcLines(lines, ref offset, timeGroupedLyrics);
            }

            return BuildLyricSlices(timeGroupedLyrics, duration);
        });
    }

    /// <summary>
    /// 解析普通LRC格式歌词行集合
    /// </summary>
    /// <param name="lines">普通LRC格式歌词行</param>
    /// <param name="offset">时间偏移量</param>
    /// <param name="timeGroupedLyrics">按时间分组的歌词字典</param>
    private static void ParseNormalLrcLines(
        string[] lines,
        ref double offset,
        Dictionary<double, LyricSliceGroup> timeGroupedLyrics
    )
    {
        double? lastTime = null; // 上一条歌词的时间
        var emptyStartTime = 0.0; // 空白歌词块的开始时间
        var inEmptyBlock = false; // 当前是否在空白歌词块中

        foreach (var line in lines)
        {
            if (TryHandleSpecialLine(line, ref offset))
            {
                continue;
            }

            try
            {
                // 提取歌词内容
                var wordMatch = WordRegex().Match(line); // 例如[01:02.97]Lorem ipsum dolor sit amet
                var word = wordMatch.Groups[1].Value; // 提取的歌词内容，例如Lorem ipsum dolor sit amet
                var isEmptyWord = string.IsNullOrWhiteSpace(word);

                // 提取这一行中的全部时间标签，一行可能包含多个时间标签，例如[01:02.97][01:53.50][02:20.60]Lorem ipsum dolor sit amet
                var timeMatches = TimeRegex().Matches(line);
                if (timeMatches.Count == 0)
                {
                    continue;
                }

                foreach (Match timeMatch in timeMatches) // 处理该行的所有时间标签
                {
                    if (!TryParseTime(timeMatch.Groups[1].ValueSpan, offset, out var time))
                    {
                        continue;
                    }

                    if (isEmptyWord) // 是空白歌词行
                    {
                        if (!inEmptyBlock) // 是空白块中第一个空白行
                        {
                            // 如果时间与上一条歌词相同, 则将空白块开始时间设为上一条歌词时间+1毫秒。例如:
                            // [01:02.97]Lorem ipsum dolor sit amet
                            // [01:02.97]
                            emptyStartTime = time != lastTime ? time : (lastTime ?? 0) + 1;
                            inEmptyBlock = true;
                        }
                    }
                    else // 不是空白歌词行
                    {
                        // 之前在空白块中, 现在遇到非空白歌词, 结束空白块。例如：
                        // [01:02.97]Lorem ipsum dolor sit amet
                        // [01:03.50]
                        // [01:07.00]
                        // [01:15.00]Consectetur adipiscing elit
                        if (inEmptyBlock)
                        {
                            if (time - emptyStartTime > 5000) // 如果空白块持续时间超过5秒，添加•••
                            {
                                AddSliceContent(timeGroupedLyrics, emptyStartTime, "•••");
                            }
                            inEmptyBlock = false;
                        }

                        AddSliceContent(timeGroupedLyrics, time, word); // 添加当前歌词内容到对应时间的片段组
                        lastTime = time;
                    }
                }
            }
            catch
            {
                continue; // 忽略该行继续解析下一行
            }
        }

        if (inEmptyBlock && lastTime.HasValue && lastTime.Value - emptyStartTime > 5000) // 如果最后一个块是空白块且持续时间超过5秒，添加•••
        {
            AddSliceContent(timeGroupedLyrics, emptyStartTime, "•••");
        }
    }

    /// <summary>
    /// 解析增强LRC格式歌词行集合，支持尖括号时间标签和多时间标签
    /// </summary>
    /// <param name="lines">增强LRC格式的歌词行</param>
    /// <param name="offset">时间偏移量</param>
    /// <param name="timeGroupedLyrics">按时间分组的歌词字典</param>
    private static void ParseEnhancedLrcLines(
        string[] lines,
        ref double offset,
        Dictionary<double, LyricSliceGroup> timeGroupedLyrics
    )
    {
        double? lastTime = null;
        var emptyStartTime = 0.0;
        var inEmptyBlock = false;

        foreach (var line in lines)
        {
            if (TryHandleSpecialLine(line, ref offset))
            {
                continue;
            }

            try
            {
                // 增强LRC同时支持两类时间标签：
                // 1. 普通方括号时间标签：[01:02.97]，但增强LRC不存在一行可能包含多个行开始时间标签的情况
                // 2. 增强尖括号时间标签：<01:02.97>
                // 例如[02:20.60]<02:20.60>Lorem<02:21.00>ipsum<02:22.14>dolor<02:23.30>sit<02:24.00>amet
                // 先分别提取两种标签，再按它们在原文中的位置合并，确保时间顺序与原文一致
                var timeMatches = TimeRegex().Matches(line);
                var enhancedTimeMatches = EnhancedTimeRegex().Matches(line);
                if (timeMatches.Count == 0 && enhancedTimeMatches.Count == 0)
                {
                    continue;
                }

                var allTimeMatches = new List<Match>(timeMatches.Count + enhancedTimeMatches.Count);
                foreach (Match timeMatch in timeMatches)
                {
                    allTimeMatches.Add(timeMatch);
                }
                foreach (Match timeMatch in enhancedTimeMatches)
                {
                    allTimeMatches.Add(timeMatch);
                }
                allTimeMatches.Sort((left, right) => left.Index.CompareTo(right.Index));

                // 统一解析这一行里的所有时间标签
                var allTimes = new List<double>(allTimeMatches.Count); // 这一行的全部时间标签解析结果，单位为毫秒
                foreach (var timeMatch in allTimeMatches)
                {
                    if (TryParseTime(timeMatch.Groups[1].ValueSpan, offset, out var time))
                    {
                        allTimes.Add(time);
                    }
                }

                if (allTimes.Count == 0)
                {
                    continue;
                }

                var startTime = allTimes[0]; // 以第一个时间标签作为该行开始时间
                double? explicitEndTime = null;
                var trimmedLine = line.TrimEnd();
                var lastTimeMatch = allTimeMatches[^1]; // 最后一个时间标签，例如<02:25.00>

                // 只有最后一个时间标签真的出现在行尾时，才把它当成显式结束时间
                // 例如[02:20.60]<02:20.60>Lorem<02:21.00>ipsum<02:22.14>dolor<02:23.30>sit<02:24.00>amet<02:25.00>，这里explicitEndTime会被设为 02:25.00
                // 而[02:20.60]<02:20.60>Lorem<02:21.00>ipsum<02:22.14>dolor<02:23.30>sit<02:24.00>amet不会设置explicitEndTime
                if (lastTimeMatch.Index + lastTimeMatch.Length == trimmedLine.Length) // 最后一个标签的起始位置加上它的长度正好等于整行文本的长度
                {
                    explicitEndTime = allTimes[^1];
                }

                var content = AllTimesRegex().Replace(line, "").Trim(); //去掉所有时间标签后，剩下的就是歌词正文，例如[02:20.60]<02:20.60>Lorem<02:20.63> <02:21.00>ipsum<02:22.14> <02:23.14>dolor<02:23.30>
                var isEmptyWord = string.IsNullOrWhiteSpace(content); // 例如Lorem ipsum dolor

                if (isEmptyWord)
                {
                    if (!inEmptyBlock)
                    {
                        emptyStartTime = startTime != lastTime ? startTime : (lastTime ?? 0) + 1;
                        inEmptyBlock = true;
                    }
                }
                else
                {
                    if (inEmptyBlock)
                    {
                        if (startTime - emptyStartTime > 5000)
                        {
                            AddSliceContent(timeGroupedLyrics, emptyStartTime, "•••");
                        }
                        inEmptyBlock = false;
                    }

                    AddSliceContent(timeGroupedLyrics, startTime, content, explicitEndTime);
                    lastTime = explicitEndTime ?? startTime;
                }
            }
            catch
            {
                continue;
            }
        }

        if (inEmptyBlock && lastTime.HasValue && lastTime.Value - emptyStartTime > 5000)
        {
            AddSliceContent(timeGroupedLyrics, emptyStartTime, "•••");
        }
    }

    /// <summary>
    /// 解析特殊行，如offset行和元信息行，并更新偏移量
    /// </summary>
    /// <param name="line">要解析的行</param>
    /// <param name="offset">时间偏移量</param>
    /// <returns>如果是特殊行则返回true，否则返回false</returns>
    private static bool TryHandleSpecialLine(string line, ref double offset)
    {
        if (line.StartsWith("[offset:", StringComparison.OrdinalIgnoreCase)) // 处理偏移量标签 [offset:±毫秒数]
        {
            var offsetMatch = OffsetRegex().Match(line);
            if (offsetMatch.Success)
            {
                offset = double.Parse(offsetMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            return true;
        }

        if (
            line.StartsWith("[ti:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[ar:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[al:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[by:", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 解析时间字符串并加上偏移量
    /// </summary>
    /// <param name="value">时间字符串，格式为mm:ss.xx</param>
    /// <param name="offset">时间偏移量，单位为毫秒</param>
    /// <param name="milliseconds">解析后的时间，单位为毫秒</param>
    /// <returns>如果解析成功则返回true，否则返回false</returns>
    private static bool TryParseTime(
        ReadOnlySpan<char> value,
        double offset,
        out double milliseconds
    )
    {
        milliseconds = 0;
        var separator1 = value.IndexOf(':');
        if (separator1 == -1)
        {
            return false;
        }

        // 解析分钟
        if (!double.TryParse(value[..separator1], CultureInfo.InvariantCulture, out var minutes))
        {
            return false;
        }

        // 解析秒（以及百分/千分秒）
        if (
            !double.TryParse(
                value[(separator1 + 1)..],
                CultureInfo.InvariantCulture,
                out var seconds
            )
        )
        {
            return false;
        }

        milliseconds = (minutes * 60 + seconds) * 1000 + offset;
        return true;
    }

    /// <summary>
    /// 向指定时间点的歌词片段组中添加歌词内容，并更新结束时间（如果提供了结束时间）
    /// </summary>
    /// <param name="groupedLyrics">歌词片段组字典</param>
    /// <param name="startTime">歌词片段的开始时间，单位为毫秒</param>
    /// <param name="content">歌词内容</param>
    /// <param name="endTime">歌词片段的结束时间，单位为毫秒，可选</param>
    private static void AddSliceContent(
        Dictionary<double, LyricSliceGroup> groupedLyrics,
        double startTime,
        string content,
        double? endTime = null
    )
    {
        // 尝试获取指定开始时间的歌词片段组，如果不存在则创建一个新的片段组并添加到字典中
        if (!groupedLyrics.TryGetValue(startTime, out var group))
        {
            groupedLyrics[startTime] = group = new LyricSliceGroup();
        }
        group.Contents.Add(content);

        // 如果提供了结束时间，则更新片段组的结束时间为当前结束时间和提供的结束时间中的较大值
        if (endTime.HasValue)
        {
            group.EndTime = group.EndTime.HasValue
                ? Math.Max(group.EndTime.Value, endTime.Value)
                : endTime.Value;
        }
    }

    /// <summary>
    /// 根据按时间分组的歌词片段组字典构建歌词片段列表，并根据每个片段的结束时间或下一片段的开始时间设置片段的结束时间
    /// </summary>
    /// <param name="groupedLyrics">按开始时间分组的歌词片段组字典</param>
    /// <param name="duration">歌曲总时长</param>
    /// <returns>构建的歌词片段列表</returns>
    private static List<LyricSlice> BuildLyricSlices(
        Dictionary<double, LyricSliceGroup> groupedLyrics,
        TimeSpan duration
    )
    {
        var sortedGroups = groupedLyrics.AsValueEnumerable().OrderBy(t => t.Key).ToList(); // 按开始时间排序
        var sortedSlices = new List<LyricSlice>(sortedGroups.Count);

        for (var i = 0; i < sortedGroups.Count; i++)
        {
            var startTime = sortedGroups[i].Key;
            var group = sortedGroups[i].Value;
            // 一个时间点下可能有多段内容，例如翻译，用换行把它们拼成一个切片内容
            var slice = new LyricSlice(startTime, string.Join("\n", group.Contents))
            {
                // 如果解析阶段已经明确给了结束时间，就直接使用它；否则用下一条歌词的开始时间作为当前歌词的结束时间；如果这是最后一条歌词，就让它持续到整首歌结束
                EndTime =
                    group.EndTime
                    ?? (
                        i < sortedGroups.Count - 1
                            ? sortedGroups[i + 1].Key
                            : duration.TotalMilliseconds
                    ),
            };
            sortedSlices.Add(slice);
        }

        return sortedSlices;
    }
}
