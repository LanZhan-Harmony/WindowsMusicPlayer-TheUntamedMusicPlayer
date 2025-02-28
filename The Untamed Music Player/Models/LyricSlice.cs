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
        var lyricSlices = new List<LyricSlice>();

        await Task.Run(() =>
        {
            if (!string.IsNullOrEmpty(lyric))
            {
                var lines = lyric.Split('\n');
                double offset = 0; // 初始化时间补偿值
                double? lastTime = null;//上一行有歌词的行的时间
                double emptyStartTime = 0;//空白行开始时间
                var inEmptyBlock = false;
                var emptyLines = new List<LyricSlice>();
                foreach (var line in lines)
                {
                    if (line is not null)
                    {
                        if (line.StartsWith("[ti:") || line.StartsWith("[ar:") || line.StartsWith("[al:") || line.StartsWith("[by:"))//歌曲名、艺人名、专辑名、歌词制作人
                        {
                            continue;
                        }
                        else if (line.StartsWith("[offset:"))//歌词时间补偿值
                        {
                            var offsetMatch = RegexOffset().Match(line);
                            if (offsetMatch.Success)
                            {
                                offset = double.Parse(offsetMatch.Groups[1].Value);
                            }
                        }

                        try
                        {
                            var regexword = RegexWord();//获取歌词文本的正则表达式
                            var regextime = RegexTime();//获取时间戳的正则表达式
                            var WORD = regexword.Match(line).Groups[1].Value;//获取歌词文本
                            var TIME = regextime.Matches(line);//获取时间戳

                            if (string.IsNullOrWhiteSpace(WORD))//歌词文本为空
                            {
                                if (!inEmptyBlock)
                                {
                                    emptyStartTime = TIME.Count > 0 ? TimeSpan.Parse("00:" + TIME[0].Groups[1].Value).TotalMilliseconds + offset : lastTime ?? 0;//将时间戳转换为毫秒(只保留第一组, 防止一行歌词里面有多个时间)
                                    if (emptyStartTime == lastTime)
                                    {
                                        emptyStartTime += 1; // 如果空白行和上一行有歌词的行时间相同，空白行的时间加1毫秒
                                    }
                                    inEmptyBlock = true;
                                }
                            }
                            else
                            {
                                if (inEmptyBlock)
                                {
                                    var emptyEndTime = TIME.Count > 0 ? TimeSpan.Parse("00:" + TIME[0].Groups[1].Value).TotalMilliseconds + offset : lastTime ?? 0;
                                    if (emptyEndTime - emptyStartTime > 5000)
                                    {
                                        lyricSlices.Add(new LyricSlice(emptyStartTime, "•••"));
                                    }
                                    inEmptyBlock = false;
                                }

                                foreach (Match item in TIME)
                                {
                                    var time = TimeSpan.Parse("00:" + item.Groups[1].Value).TotalMilliseconds + offset;
                                    lyricSlices.Add(new LyricSlice(time, WORD));
                                    lastTime = time;
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                if (inEmptyBlock && emptyLines.Count > 0)
                {
                    var emptyEndTime = emptyLines.Last().Time;
                    if (emptyEndTime - emptyStartTime > 5000)
                    {
                        lyricSlices.Add(new LyricSlice(emptyStartTime, "•••"));
                    }
                }
            }
        });

        return [.. lyricSlices.OrderBy(t => t.Time)];//将 lyricSlices 列表按时间戳排序
    }
}
