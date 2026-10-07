using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using UntamedMediaPlayer.Core.Enums;

namespace UntamedMediaPlayer.Core.Helpers;

public static class FileHelper
{
    public static ImmutableHashSet<string> SupportedAudioFormats { get; } = [".mp3", ".flac", ".ogg", ".m4a", ".wav", ".opus", ".dsf", ".dff", ".mid", ".midi", ".cda", ".ape", ".webm", ".wv", ".mp2", ".mp1", ".aif", ".aiff", ".m2a", ".m1a", ".mp3pro", ".bwf"];
    public static ImmutableHashSet<string> SupportedVideoFormats { get; } = [".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv", ".3gp", ".3g2", ".m4v", ".mpg", ".mpeg", ".webm", ".rm", ".rmvb", ".asf", ".wm", ".wtv", ".f4v", ".swf", ".vob", ".mxf", ".ogv", ".ogm"];
    public static ImmutableHashSet<string> SupportedPlaylistFormats { get; } = [".m3u8", ".m3u", ".ts", ".mts", ".m2ts", ".m2t"];
    public static ImmutableHashSet<string> SupportedMediaFormats { get; } = [.. SupportedAudioFormats, .. SupportedVideoFormats, .. SupportedPlaylistFormats];

    public static ImmutableHashSet<string> SupportedImageFormats { get; } = [".png", ".jpg", ".jpeg", ".jpe", ".jfif", ".bmp", ".dip", ".gif", ".tif", ".tiff"];
    public static ImmutableHashSet<string> SupportedSubtitleFormats { get; } = [".srt", ".vtt", ".ass", ".idx", ".sub"];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupportedAudio(string filePath)
    {
        return SupportedAudioFormats.Contains(Path.GetExtension(filePath).ToLowerInvariant());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupportedVideo(string filePath)
    {
        return SupportedVideoFormats.Contains(Path.GetExtension(filePath).ToLowerInvariant());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupportedPlaylist(string filePath)
    {
        return SupportedPlaylistFormats.Contains(Path.GetExtension(filePath).ToLowerInvariant());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupportedMedia(string filePath)
    {
        return SupportedMediaFormats.Contains(Path.GetExtension(filePath).ToLowerInvariant());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupportedImage(string filePath)
    {
        return SupportedImageFormats.Contains(Path.GetExtension(filePath).ToLowerInvariant());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupportedSubtitle(string filePath)
    {
        return SupportedSubtitleFormats.Contains(Path.GetExtension(filePath).ToLowerInvariant());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MediaType GetMediaTypeForFile(string filePath)
    {
        if (IsSupportedAudio(filePath))
        {
            return MediaType.Music;
        }
        if (IsSupportedVideo(filePath))
        {
            return MediaType.Video;
        }
        if (IsSupportedPlaylist(filePath))
        {
            return MediaType.Playlist;
        }
        return MediaType.Unknown;
    }
}
