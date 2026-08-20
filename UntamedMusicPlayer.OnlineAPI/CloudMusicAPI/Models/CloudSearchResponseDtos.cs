using System.Text.Json.Serialization;

namespace UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;

public sealed class CloudSearchSongsResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("result")]
    public CloudSearchSongsResult? Result { get; set; }
}

public sealed class CloudSearchSongsResult
{
    [JsonPropertyName("songCount")]
    public int SongCount { get; set; }

    [JsonPropertyName("songs")]
    public List<CloudSearchSongDto>? Songs { get; set; }
}

public sealed class CloudSearchSongDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("album")]
    public CloudSearchAlbumReferenceDto? Album { get; set; }

    [JsonPropertyName("artists")]
    public List<CloudSearchArtistReferenceDto>? Artists { get; set; }

    [JsonPropertyName("duration")]
    public long Duration { get; set; }
}

public sealed class CloudSearchAlbumsResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("result")]
    public CloudSearchAlbumsResult? Result { get; set; }
}

public sealed class CloudSearchAlbumsResult
{
    [JsonPropertyName("albumCount")]
    public int AlbumCount { get; set; }

    [JsonPropertyName("albums")]
    public List<CloudSearchAlbumDto>? Albums { get; set; }
}

public sealed class CloudSearchAlbumDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("artists")]
    public List<CloudSearchArtistReferenceDto>? Artists { get; set; }

    [JsonPropertyName("publishTime")]
    public long PublishTime { get; set; }
}

public sealed class CloudSearchAlbumReferenceDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("publishTime")]
    public long PublishTime { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }
}

public sealed class CloudSearchArtistReferenceDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class CloudSearchArtistsResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("result")]
    public CloudSearchArtistsResult? Result { get; set; }
}

public sealed class CloudSearchArtistsResult
{
    [JsonPropertyName("artistCount")]
    public int ArtistCount { get; set; }

    [JsonPropertyName("artists")]
    public List<CloudSearchArtistDto>? Artists { get; set; }
}

public sealed class CloudSearchArtistDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }
}

public sealed class CloudSearchPlaylistsResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("result")]
    public CloudSearchPlaylistsResult? Result { get; set; }
}

public sealed class CloudSearchPlaylistsResult
{
    [JsonPropertyName("playlistCount")]
    public int PlaylistCount { get; set; }

    [JsonPropertyName("playlists")]
    public List<CloudSearchPlaylistDto>? Playlists { get; set; }
}

public sealed class CloudSearchPlaylistDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("coverImgUrl")]
    public string? CoverImgUrl { get; set; }

    [JsonPropertyName("trackCount")]
    public int TrackCount { get; set; }
}

public sealed class CloudSearchSuggestResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("result")]
    public CloudSearchSuggestResult? Result { get; set; }
}

public sealed class CloudSearchSuggestResult
{
    [JsonPropertyName("songs")]
    public List<CloudSearchSuggestionItemDto>? Songs { get; set; }

    [JsonPropertyName("albums")]
    public List<CloudSearchSuggestionItemDto>? Albums { get; set; }

    [JsonPropertyName("artists")]
    public List<CloudSearchSuggestionItemDto>? Artists { get; set; }

    [JsonPropertyName("playlists")]
    public List<CloudSearchSuggestionItemDto>? Playlists { get; set; }
}

public sealed class CloudSearchSuggestionItemDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class CloudSongUrlResponse
{
    [JsonPropertyName("data")]
    public List<CloudSongUrlItem>? Data { get; set; }
}

public sealed class CloudSongUrlItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("br")]
    public int BitRate { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class CloudSongDetailResponse
{
    [JsonPropertyName("songs")]
    public List<CloudSongTrackDto>? Songs { get; set; }
}

public sealed class CloudSongTrackDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("al")]
    public CloudSongAlbumDto? Album { get; set; }

    [JsonPropertyName("ar")]
    public List<CloudSearchArtistReferenceDto>? Artists { get; set; }

    [JsonPropertyName("dt")]
    public long Duration { get; set; }
}

public sealed class CloudSongAlbumDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("publishTime")]
    public long PublishTime { get; set; }

    [JsonPropertyName("artists")]
    public List<CloudSearchArtistReferenceDto>? Artists { get; set; }
}

public sealed class CloudAlbumResponse
{
    [JsonPropertyName("album")]
    public CloudAlbumDetailDto? Album { get; set; }

    [JsonPropertyName("songs")]
    public List<CloudSongTrackDto>? Songs { get; set; }
}

public sealed class CloudAlbumDetailDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("publishTime")]
    public long PublishTime { get; set; }

    [JsonPropertyName("artists")]
    public List<CloudSearchArtistReferenceDto>? Artists { get; set; }
}

public sealed class CloudLyricResponse
{
    [JsonPropertyName("lrc")]
    public CloudLyricDto? Lrc { get; set; }
}

public sealed class CloudLyricDto
{
    [JsonPropertyName("lyric")]
    public string? Lyric { get; set; }
}

public sealed class CloudArtistAlbumResponse
{
    [JsonPropertyName("artist")]
    public CloudArtistDto? Artist { get; set; }

    [JsonPropertyName("hotAlbums")]
    public List<CloudArtistAlbumDto>? Albums { get; set; }
}

public sealed class CloudArtistDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("albumSize")]
    public int AlbumSize { get; set; }

    [JsonPropertyName("musicSize")]
    public int MusicSize { get; set; }
}

public sealed class CloudArtistAlbumDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("publishTime")]
    public long PublishTime { get; set; }
}

public sealed class CloudArtistDescriptionResponse
{
    [JsonPropertyName("briefDesc")]
    public string? BriefDescription { get; set; }
}

public sealed class CloudPlaylistDetailResponse
{
    [JsonPropertyName("playlist")]
    public CloudPlaylistDetailDto? Playlist { get; set; }
}

public sealed class CloudPlaylistDetailDto
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("trackIds")]
    public List<CloudPlaylistTrackIdDto>? TrackIds { get; set; }
}

public sealed class CloudPlaylistTrackIdDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
