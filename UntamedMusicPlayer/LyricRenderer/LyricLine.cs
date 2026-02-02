namespace UntamedMusicPlayer.LyricRenderer;

/// <summary>
/// 表示一行歌词，包含主歌词和翻译
/// </summary>
public sealed class LyricLine
{
    /// <summary>
    /// 主歌词文本（第一行）
    /// </summary>
    public string MainText { get; set; } = "";

    /// <summary>
    /// 翻译文本（其余行）
    /// </summary>
    public string TranslationText { get; set; } = "";

    /// <summary>
    /// 歌词开始时间（毫秒）
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// 歌词结束时间（毫秒），即下一行的开始时间
    /// </summary>
    public double EndTime { get; set; }

    /// <summary>
    /// 主歌词字符数
    /// </summary>
    public int CharacterCount => MainText.Length;

    /// <summary>
    /// 每个字符的持续时间（毫秒）
    /// </summary>
    public double TimePerCharacter =>
        CharacterCount > 0 ? (EndTime - StartTime) / CharacterCount : 0;

    /// <summary>
    /// 计算当前时间下的字符进度（0.0 到 1.0）
    /// </summary>
    /// <param name="currentTime">当前播放时间（毫秒）</param>
    /// <returns>已完成的字符比例</returns>
    public float GetCharacterProgress(double currentTime)
    {
        if (currentTime <= StartTime)
        {
            return 0f;
        }

        if (currentTime >= EndTime)
        {
            return 1f;
        }

        if (CharacterCount == 0)
        {
            return 1f;
        }

        var elapsed = currentTime - StartTime;
        var duration = EndTime - StartTime;
        return (float)(elapsed / duration);
    }

    /// <summary>
    /// 获取当前正在高亮的字符索引
    /// </summary>
    /// <param name="currentTime">当前播放时间（毫秒）</param>
    /// <returns>当前字符索引</returns>
    public int GetCurrentCharacterIndex(double currentTime)
    {
        if (CharacterCount == 0)
        {
            return 0;
        }

        var progress = GetCharacterProgress(currentTime);
        return Math.Min((int)(progress * CharacterCount), CharacterCount - 1);
    }

    /// <summary>
    /// 原始的LyricSlice引用，用于点击事件
    /// </summary>
    public LyricSlice? SourceSlice { get; set; }

    /// <summary>
    /// 从LyricSlice创建LyricLine
    /// </summary>
    public static LyricLine FromSlice(LyricSlice slice)
    {
        var content = slice.Content;
        var lines = content.Split('\n', StringSplitOptions.None);

        return new LyricLine
        {
            MainText = lines.Length > 0 ? lines[0] : "",
            TranslationText = lines.Length > 1 ? string.Join("\n", lines.Skip(1)) : "",
            StartTime = slice.StartTime,
            EndTime = slice.EndTime,
            SourceSlice = slice,
        };
    }
}
