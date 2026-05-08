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

    [GeneratedRegex(@"\[offset:\s*([+-]?\d+)\]", RegexOptions.Compiled)]
    private static partial Regex OffsetRegex();

    [GeneratedRegex(@"<\s*([0-9.:]+)\s*>", RegexOptions.Compiled)]
    private static partial Regex EnhancedTimeRegex();

    [GeneratedRegex(@"(?:\[\s*[0-9.:]+\s*\]|<\s*[0-9.:]+\s*>)", RegexOptions.Compiled)]
    private static partial Regex AllTimesRegex();

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
            var enhanced = lines.Any(line => line.Contains('<'));
            var timeGroupedLyrics = new Dictionary<double, LyricSliceGroup>();

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

    private static void ParseNormalLrcLines(
        IEnumerable<string> lines,
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
                var wordMatch = WordRegex().Match(line);
                var word = wordMatch.Groups[1].Value;
                var isEmptyWord = string.IsNullOrWhiteSpace(word);

                var timeMatches = TimeRegex().Matches(line);
                if (timeMatches.Count == 0)
                {
                    continue;
                }

                foreach (Match timeMatch in timeMatches)
                {
                    if (!TryParseTime(timeMatch.Groups[1].Value, offset, out var time))
                    {
                        continue;
                    }

                    if (isEmptyWord)
                    {
                        if (!inEmptyBlock)
                        {
                            emptyStartTime = time != lastTime ? time : (lastTime ?? 0) + 1;
                            inEmptyBlock = true;
                        }
                    }
                    else
                    {
                        if (inEmptyBlock)
                        {
                            if (time - emptyStartTime > 5000)
                            {
                                AddSliceContent(timeGroupedLyrics, emptyStartTime, "•••");
                            }

                            inEmptyBlock = false;
                        }

                        AddSliceContent(timeGroupedLyrics, time, word);
                        lastTime = time;
                    }
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

    private static void ParseEnhancedLrcLines(
        IEnumerable<string> lines,
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
                var timeMatches = TimeRegex().Matches(line);
                var enhancedTimeMatches = EnhancedTimeRegex().Matches(line);
                if (timeMatches.Count == 0 && enhancedTimeMatches.Count == 0)
                {
                    continue;
                }

                var allTimes = new List<double>();
                foreach (Match timeMatch in timeMatches)
                {
                    if (TryParseTime(timeMatch.Groups[1].Value, offset, out var time))
                    {
                        allTimes.Add(time);
                    }
                }

                foreach (Match timeMatch in enhancedTimeMatches)
                {
                    if (TryParseTime(timeMatch.Groups[1].Value, offset, out var time))
                    {
                        allTimes.Add(time);
                    }
                }

                if (allTimes.Count == 0)
                {
                    continue;
                }

                var startTime = allTimes[0];
                double? explicitEndTime = allTimes.Count > 1 ? allTimes[^1] : null;
                var content = AllTimesRegex().Replace(line, string.Empty).Trim();
                var isEmptyWord = string.IsNullOrWhiteSpace(content);

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

    private static bool TryHandleSpecialLine(string line, ref double offset)
    {
        if (line.StartsWith("[offset:", StringComparison.OrdinalIgnoreCase))
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

    private static bool TryParseTime(string value, double offset, out double milliseconds)
    {
        if (TimeSpan.TryParse($"00:{value}", CultureInfo.InvariantCulture, out var timeSpan))
        {
            milliseconds = timeSpan.TotalMilliseconds + offset;
            return true;
        }

        milliseconds = 0;
        return false;
    }

    private static void AddSliceContent(
        Dictionary<double, LyricSliceGroup> groupedLyrics,
        double startTime,
        string content,
        double? endTime = null
    )
    {
        if (!groupedLyrics.TryGetValue(startTime, out var group))
        {
            groupedLyrics[startTime] = group = new LyricSliceGroup();
        }

        group.Contents.Add(content);

        if (endTime.HasValue)
        {
            group.EndTime = group.EndTime.HasValue
                ? Math.Max(group.EndTime.Value, endTime.Value)
                : endTime.Value;
        }
    }

    private static List<LyricSlice> BuildLyricSlices(
        Dictionary<double, LyricSliceGroup> groupedLyrics,
        TimeSpan duration
    )
    {
        var sortedGroups = groupedLyrics.AsValueEnumerable().OrderBy(t => t.Key).ToList();
        var sortedSlices = new List<LyricSlice>(sortedGroups.Count);

        for (var i = 0; i < sortedGroups.Count; i++)
        {
            var startTime = sortedGroups[i].Key;
            var group = sortedGroups[i].Value;
            var slice = new LyricSlice(startTime, string.Join("\n", group.Contents))
            {
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
