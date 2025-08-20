using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
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
        Playlists = await FileManager.LoadPlaylistDataAsync();
        foreach (var playlist in Playlists)
        {
            playlist.GetCover();
        }
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
        StrongReferenceMessenger.Default.Send(
            new LogMessage(
                LogLevel.None,
                "PlaylistInfo_Create".GetLocalizedWithReplace("{title}", uniqueName)
            )
        );
        return info;
    }

    public bool RenamePlaylist(PlaylistInfo info, string newName)
    {
        var oldName = info.Name;
        if (string.IsNullOrEmpty(newName) || newName == oldName)
        {
            return false;
        }
        var uniqueName = GetUniquePlaylistName(newName);
        info.Name = uniqueName;
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
        StrongReferenceMessenger.Default.Send(new PlaylistRenameMessage(oldName, info));
        return true;
    }

    public void DeletePlaylist(PlaylistInfo info)
    {
        Playlists.Remove(info);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
        StrongReferenceMessenger.Default.Send(
            new LogMessage(
                LogLevel.None,
                "PlaylistInfo_Delete".GetLocalizedWithReplace("{title}", info.Name)
            )
        );
    }

    public async Task AddToPlaylist(PlaylistInfo info, IBriefSongInfoBase song)
    {
        await info.Add(song);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
        StrongReferenceMessenger.Default.Send(new PlaylistChangeMessage(info));
        var replacements = new Dictionary<string, string>
        {
            { "{num}", "1" },
            { "{title}", info.Name },
        };
        StrongReferenceMessenger.Default.Send(
            new LogMessage(
                LogLevel.None,
                "PlaylistInfo_AddItem".GetLocalizedWithReplace(replacements)
            )
        );
    }

    public async Task AddToPlaylist(PlaylistInfo info, IEnumerable<IBriefSongInfoBase> songs)
    {
        if (!songs.Any())
        {
            return;
        }
        await info.AddRange(songs);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
        StrongReferenceMessenger.Default.Send(new PlaylistChangeMessage(info));
        var count = songs.Count();
        var replacements = new Dictionary<string, string>
        {
            { "{num}", $"{count}" },
            { "{title}", info.Name },
        };
        StrongReferenceMessenger.Default.Send(
            new LogMessage(
                LogLevel.None,
                count == 1
                    ? "PlaylistInfo_AddItem".GetLocalizedWithReplace(replacements)
                    : "PlaylistInfo_AddItems".GetLocalizedWithReplace(replacements)
            )
        );
    }

    public async Task DeleteFromPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        await info.Delete(song);
        StrongReferenceMessenger.Default.Send(new HavePlaylistMessage(Playlists.Count > 0));
        StrongReferenceMessenger.Default.Send(new PlaylistChangeMessage(info));
    }

    public void MoveUpInPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        info.MoveUp(song);
    }

    public void MoveDownInPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        info.MoveDown(song);
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

    public async void SaveLibraryAsync()
    {
        await FileManager.SavePlaylistDataAsync(Playlists);
    }
}
