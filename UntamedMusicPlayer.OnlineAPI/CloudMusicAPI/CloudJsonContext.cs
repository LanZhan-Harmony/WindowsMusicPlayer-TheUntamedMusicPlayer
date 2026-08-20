using System.Text.Json.Serialization;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;

namespace UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CloudSearchSongsResponse))]
[JsonSerializable(typeof(CloudSearchAlbumsResponse))]
[JsonSerializable(typeof(CloudSearchArtistsResponse))]
[JsonSerializable(typeof(CloudSearchPlaylistsResponse))]
[JsonSerializable(typeof(CloudSearchSuggestResponse))]
[JsonSerializable(typeof(CloudSongUrlResponse))]
[JsonSerializable(typeof(CloudSongDetailResponse))]
[JsonSerializable(typeof(CloudAlbumResponse))]
[JsonSerializable(typeof(CloudLyricResponse))]
[JsonSerializable(typeof(CloudArtistAlbumResponse))]
[JsonSerializable(typeof(CloudArtistDescriptionResponse))]
[JsonSerializable(typeof(CloudPlaylistDetailResponse))]
public partial class CloudJsonContext : JsonSerializerContext;
