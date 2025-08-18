using CommunityToolkit.Mvvm.Messaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Messages;
using ZLinq;

namespace The_Untamed_Music_Player.Models;

public class PlaylistLibrary
{
    public List<PlaylistInfo> Playlists { get; set; } = [];

    public PlaylistLibrary()
    {
        _ = LoadLibraryAsync();
    }

    public async Task LoadLibraryAsync()
    {
        await Task.Run(async () => Playlists = await FileManager.LoadPlaylistDataAsync());
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
    }

    public PlaylistInfo? NewPlaylist(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        var uniqueName = GetUniquePlaylistName(name);
        var info = new PlaylistInfo(uniqueName);
        Playlists.Add(info);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(true));
        FileManager.SavePlaylistDataAsync(Playlists);
        return info;
    }

    public bool RenamePlaylist(PlaylistInfo info, string newName)
    {
        if (string.IsNullOrEmpty(newName) || newName == info.Name)
        {
            return false;
        }
        var uniqueName = GetUniquePlaylistName(newName);
        info.Name = uniqueName;
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
        StrongReferenceMessenger.Default.Send(new PlaylistChangeMessage(info));
        FileManager.SavePlaylistDataAsync(Playlists);
        return true;
    }

    public void DeletePlaylist(PlaylistInfo info)
    {
        Playlists.Remove(info);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
        FileManager.SavePlaylistDataAsync(Playlists);
    }

    public async Task AddToPlaylist(PlaylistInfo info, IBriefSongInfoBase song)
    {
        await info.Add(song);
        FileManager.SavePlaylistDataAsync(Playlists);
    }

    public async Task AddToPlaylist(PlaylistInfo info, IEnumerable<IBriefSongInfoBase> songs)
    {
        await info.AddRange(songs);
        FileManager.SavePlaylistDataAsync(Playlists);
    }

    public void DeleteFromPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        info.Delete(song);
        FileManager.SavePlaylistDataAsync(Playlists);
    }

    public void MoveUpInPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        info.MoveUp(song);
        FileManager.SavePlaylistDataAsync(Playlists);
    }

    public void MoveDownInPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        info.MoveDown(song);
        FileManager.SavePlaylistDataAsync(Playlists);
    }

    private string GetUniquePlaylistName(string baseName)
    {
        var existingNames = Playlists.AsValueEnumerable().Select(p => p.Name).ToHashSet();
        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }
        var counter = 2;
        string candidateName;
        do
        {
            candidateName = $"{baseName} ({counter})";
            counter++;
        } while (existingNames.Contains(candidateName));
        return candidateName;
    }
}
