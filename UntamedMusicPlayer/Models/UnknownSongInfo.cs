using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using UntamedMusicPlayer.Contracts.Models;

namespace UntamedMusicPlayer.Models;

[MemoryPackable]
public partial class BriefUnknownSongInfo : IBriefSongInfoBase
{
    public bool IsPlayAvailable { get; set; } = true;
    public bool IsOnline { get; set; }
    public string Path { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Album { get; set; } = null!;
    public string ArtistsStr { get; set; } = null!;
    public string DurationStr { get; set; } = null!;
    public string YearStr { get; set; } = null!;
    public string GenreStr { get; set; } = null!;

    [MemoryPackConstructor]
    public BriefUnknownSongInfo() { }

    public BriefUnknownSongInfo(Uri uri)
    {
        try
        {
            IsOnline = !uri.IsFile;
            Path = IsOnline ? $"{uri}" : uri.LocalPath;
            Title = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : "";
        }
        catch
        {
            IsPlayAvailable = false;
        }
    }
}

public class DetailedUnknownSongInfo : BriefUnknownSongInfo, IDetailedSongInfoBase
{
    public string ItemType { get; set; } = null!;
    public string AlbumArtistsStr { get; set; } = null!;
    public string ArtistAndAlbumStr { get; set; } = "";
    public BitmapImage? Cover { get; set; }
    public string BitRate { get; set; } = null!;
    public string TrackStr { get; set; } = null!;
    public string Lyric { get; set; } = null!;

    public DetailedUnknownSongInfo(BriefUnknownSongInfo info)
    {
        IsOnline = info.IsOnline;
        Path = info.Path;
        Title = info.Title;
    }
}
