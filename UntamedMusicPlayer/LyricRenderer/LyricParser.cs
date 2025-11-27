using System.Text.RegularExpressions;
using ZLinq;

namespace UntamedMusicPlayer.LyricRenderer;

public static partial class LyricParser
{
    [GeneratedRegex(@".*\](.*)", RegexOptions.Compiled)]
    private static partial Regex RegexWord();

    [GeneratedRegex(@"\[([0-9.:]*)\]", RegexOptions.Compiled)]
    private static partial Regex RegexTime();

    [GeneratedRegex(@"\[offset:\s*([+-]?\d+)\]", RegexOptions.Compiled)]
    private static partial Regex RegexOffset();

    /// <summary>
    /// 解析歌词文本并返回歌词片段列表
    /// </summary>
    /// <param name="lyric">LRC格式的歌词文本</param>
    /// <returns>按时间排序的歌词片段列表</returns>
    public static async Task<List<LyricSlice>> GetLyricSlices(string lyric)
    {
        if (string.IsNullOrWhiteSpace(lyric))
        {
            return [];
        }

        var lyricSlices = new List<LyricSlice>();
        await Task.Run(() =>
        {
            var lines = lyric.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            var offset = 0.0; // 时间偏移量（毫秒）
            double? lastTime = null; // 上一条歌词的时间
            var emptyStartTime = 0.0; // 空白歌词块的开始时间
            var inEmptyBlock = false; // 当前是否在空白歌词块中
            var timeGroupedLyrics = new Dictionary<double, List<string>>(); // 用于存储时间相同的歌词内容(例如翻译), Dictionary<时间, 歌词内容列表>

            foreach (var line in lines) // 解析所有歌词行并按时间分组
            {
                if (line.StartsWith("[offset:")) // 处理偏移量标签 [offset:±毫秒数]
                {
                    var offsetMatch = RegexOffset().Match(line);
                    if (offsetMatch.Success)
                    {
                        offset = double.Parse(offsetMatch.Groups[1].Value);
                    }
                    continue;
                }

                if (
                    line.StartsWith("[ti:") // 标题
                    || line.StartsWith("[ar:") // 艺术家
                    || line.StartsWith("[al:") // 专辑
                    || line.StartsWith("[by:") // 制作者
                ) // 跳过元信息标签
                {
                    continue;
                }

                try
                {
                    // 提取歌词内容
                    var wordMatch = RegexWord().Match(line);
                    var word = wordMatch.Groups[1].Value;
                    var isEmptyWord = string.IsNullOrWhiteSpace(word);

                    // 提取时间标签
                    var timeMatches = RegexTime().Matches(line);
                    if (timeMatches.Count == 0)
                    {
                        continue;
                    }

                    foreach (Match timeMatch in timeMatches) // 处理该行的所有时间标签
                    {
                        var time =
                            TimeSpan.Parse("00:" + timeMatch.Groups[1].Value).TotalMilliseconds
                            + offset; // 解析时间并加上偏移量

                        if (isEmptyWord) // 是空白歌词
                        {
                            if (!inEmptyBlock) // 是空白块中第一个空白行
                            {
                                emptyStartTime = time != lastTime ? time : (lastTime ?? 0) + 1; // 如果时间与上一条歌词相同, 则将空白块开始时间设为上一条歌词时间+1毫秒
                                inEmptyBlock = true;
                            }
                        }
                        else // 不是空白歌词
                        {
                            if (inEmptyBlock) // 之前在空白块中, 现在遇到非空白歌词, 结束空白块
                            {
                                if (time - emptyStartTime > 5000) // 如果空白块持续时间超过5秒，添加省略号
                                {
                                    if (
                                        !timeGroupedLyrics.TryGetValue(
                                            emptyStartTime,
                                            out var emptyBlockList
                                        )
                                    )
                                    {
                                        timeGroupedLyrics[emptyStartTime] = emptyBlockList = [];
                                    }
                                    emptyBlockList.Add("•••");
                                }
                                inEmptyBlock = false;
                            }

                            if (!timeGroupedLyrics.TryGetValue(time, out var lyricList)) // 将歌词按时间分组
                            {
                                timeGroupedLyrics[time] = lyricList = [];
                            }
                            lyricList.Add(word);
                            lastTime = time;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (inEmptyBlock && lastTime.HasValue && lastTime.Value - emptyStartTime > 5000) // 处理最后一个空白块
            {
                if (!timeGroupedLyrics.TryGetValue(emptyStartTime, out var emptyBlockList))
                {
                    timeGroupedLyrics[emptyStartTime] = emptyBlockList = [];
                }
                emptyBlockList.Add("•••");
            }

            foreach (var kvp in timeGroupedLyrics) // 将分组的歌词转换为LyricSlice对象
            {
                var time = kvp.Key;
                var contents = kvp.Value;
                var mergedContent = string.Join("\n", contents); // 将同一时间的多行歌词用换行符连接
                lyricSlices.Add(new LyricSlice(time, mergedContent));
            }
        });

        // 按时间排序并返回
        return [.. lyricSlices.AsValueEnumerable().OrderBy(t => t.Time)];
    }
}
