namespace The_Untamed_Music_Player.Contracts.Models;

public interface IOnlineAlbumInfo : IAlbumInfoBase
{
    long ID { get; set; }
    long[] Artists { get; set; }
}
