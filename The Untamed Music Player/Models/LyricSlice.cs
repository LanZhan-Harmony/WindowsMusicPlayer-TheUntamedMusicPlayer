using System.Text.RegularExpressions;

namespace The_Untamed_Music_Player.Models;

public partial class LyricSlice(double time, string content)
{
    public string Content { get; set; } = content;

    public double Time { get; set; } = time;

    [GeneratedRegex(@".*\](.*)")]
    private static partial Regex RegexWord();

    [GeneratedRegex(@"\[([0-9.:]*)\]", RegexOptions.Compiled)]
    private static partial Regex RegexTime();

    [GeneratedRegex(@"\[offset:\s*([+-]?\d+)\]", RegexOptions.Compiled)]
    private static partial Regex RegexOffset();

    public static async Task<List<LyricSlice>> GetLyricSlices(string lyric)
    {
        if (string.IsNullOrWhiteSpace(lyric))
        {
            return [];
        }

        var lyricSlices = new List<LyricSlice>();
        await Task.Run(() =>
        {
            var lines = lyric.Split('\n');
            var offset = 0.0;
            double? lastTime = null;
            var emptyStartTime = 0.0;
            var inEmptyBlock = false;

            // 预编译的正则表达式已经在类级别定义，直接使用
            var regexword = RegexWord();
            var regextime = RegexTime();
            var regexOffset = RegexOffset();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // 处理元信息行
                if (line.StartsWith("[offset:"))
                {
                    var offsetMatch = regexOffset.Match(line);
                    if (offsetMatch.Success)
                    {
                        offset = double.Parse(offsetMatch.Groups[1].Value);
                    }
                    continue;
                }
                if (
                    line.StartsWith("[ti:")
                    || line.StartsWith("[ar:")
                    || line.StartsWith("[al:")
                    || line.StartsWith("[by:")
                )
                {
                    continue;
                }

                try
                {
                    var wordMatch = regexword.Match(line);
                    var timeMatches = regextime.Matches(line);

                    if (timeMatches.Count == 0)
                    {
                        continue;
                    }

                    var word = wordMatch.Groups[1].Value;
                    var isEmptyWord = string.IsNullOrWhiteSpace(word);

                    var currentTime =
                        TimeSpan.Parse("00:" + timeMatches[0].Groups[1].Value).TotalMilliseconds
                        + offset;

                    if (isEmptyWord)
                    {
                        if (!inEmptyBlock)
                        {
                            emptyStartTime =
                                currentTime != lastTime ? currentTime : (lastTime ?? 0) + 1;
                            inEmptyBlock = true;
                        }
                    }
                    else
                    {
                        if (inEmptyBlock)
                        {
                            if (currentTime - emptyStartTime > 5000)
                            {
                                lyricSlices.Add(new LyricSlice(emptyStartTime, "•••"));
                            }
                            inEmptyBlock = false;
                        }

                        foreach (Match timeMatch in timeMatches)
                        {
                            var time =
                                TimeSpan.Parse("00:" + timeMatch.Groups[1].Value).TotalMilliseconds
                                + offset;
                            lyricSlices.Add(new LyricSlice(time, word));
                            lastTime = time;
                        }
                    }
                }
                catch (Exception)
                {
                    // 考虑添加日志记录，而不是静默忽略
                    continue;
                }
            }

            // 处理最后一个空白块
            if (inEmptyBlock && lastTime.HasValue && lastTime.Value - emptyStartTime > 5000)
            {
                lyricSlices.Add(new LyricSlice(emptyStartTime, "•••"));
            }
        });
        return [.. lyricSlices.OrderBy(t => t.Time)];
    }
}
