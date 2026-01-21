using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using ZLinq;

namespace UntamedMusicPlayer.Models;

public sealed partial class PlaylistLibrary : ObservableRecipient
{
    public bool HasLoaded { get; private set; } = false;

    public List<PlaylistInfo> Playlists { get; set; } = [];

    public PlaylistLibrary()
        : base(StrongReferenceMessenger.Default)
    {
        _ = LoadLibraryAsync();
    }

    public async Task LoadLibraryAsync()
    {
        Playlists = await FileManager.LoadPlaylistDataAsync();
        HasLoaded = true;
        CoverManager.ForceAllPlaylistCoversRefresh();
        Messenger.Send(new HavePlaylistMessage(Playlists.Count > 0));
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
        Playlists = [.. Playlists.AsValueEnumerable().OrderBy(p => p.Name, new TitleComparer())];
        Messenger.Send(new HavePlaylistMessage(true));
        Messenger.Send(
            new LogMessage(
                LogLevel.None,
                "PlaylistInfo_Create".GetLocalizedWithReplace("{title}", uniqueName)
            )
        );
        _ = FileManager.SavePlaylistDataAsync(Playlists);
        return info;
    }

    public void NewPlaylists(List<PlaylistInfo> list)
    {
        foreach (var info in list)
        {
            var name = GetUniquePlaylistName(info.Name);
            if (name == info.Name)
            {
                Playlists.Add(info);
            }
            else
            {
                var newInfo = new PlaylistInfo(name, info);
                Playlists.Add(newInfo);
            }
        }
        Playlists = [.. Playlists.AsValueEnumerable().OrderBy(p => p.Name, new TitleComparer())];
        Messenger.Send(new HavePlaylistMessage(true));
        _ = FileManager.SavePlaylistDataAsync(Playlists);
    }

    public void DeletePlaylist(PlaylistInfo info)
    {
        CoverManager.ForcePlaylistCoverRefresh(info);
        info.PrepareForRemoval();
        Playlists.Remove(info);
        Messenger.Send(new HavePlaylistMessage(Playlists.Count > 0));
        Messenger.Send(
            new LogMessage(
                LogLevel.None,
                "PlaylistInfo_Delete".GetLocalizedWithReplace("{title}", info.Name)
            )
        );
        _ = FileManager.SavePlaylistDataAsync(Playlists);
    }

    public async Task AddToPlaylist(PlaylistInfo info, IBriefSongInfoBase song)
    {
        await info.Add(song);
        Messenger.Send(new HavePlaylistMessage(Playlists.Count > 0));
        Messenger.Send(new PlaylistChangeMessage(info));
        var replacements = new Dictionary<string, string>
        {
            { "{num}", "1" },
            { "{title}", info.Name },
        };
        Messenger.Send(
            new LogMessage(
                LogLevel.None,
                "PlaylistInfo_AddItem".GetLocalizedWithReplace(replacements)
            )
        );
        _ = FileManager.SavePlaylistDataAsync(Playlists);
    }

    public async Task AddToPlaylist(PlaylistInfo info, IEnumerable<IBriefSongInfoBase> songs)
    {
        if (!songs.AsValueEnumerable().Any())
        {
            return;
        }
        await info.AddRange(songs);
        Messenger.Send(new HavePlaylistMessage(Playlists.Count > 0));
        Messenger.Send(new PlaylistChangeMessage(info));
        var count = songs.AsValueEnumerable().Count();
        var replacements = new Dictionary<string, string>
        {
            { "{num}", $"{count}" },
            { "{title}", info.Name },
        };
        Messenger.Send(
            new LogMessage(
                LogLevel.None,
                count == 1
                    ? "PlaylistInfo_AddItem".GetLocalizedWithReplace(replacements)
                    : "PlaylistInfo_AddItems".GetLocalizedWithReplace(replacements)
            )
        );
        _ = FileManager.SavePlaylistDataAsync(Playlists);
    }

    public async Task DeleteFromPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        await info.Delete(song);
        Messenger.Send(new HavePlaylistMessage(Playlists.Count > 0));
        Messenger.Send(new PlaylistChangeMessage(info));
        _ = FileManager.SavePlaylistDataAsync(Playlists);
    }

    public void MoveUpInPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        info.MoveUp(song);
        _ = FileManager.SavePlaylistDataAsync(Playlists);
    }

    public void MoveDownInPlaylist(PlaylistInfo info, IndexedPlaylistSong song)
    {
        info.MoveDown(song);
        _ = FileManager.SavePlaylistDataAsync(Playlists);
    }

    public string GetUniquePlaylistName(string baseName)
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

    public async Task SaveLibraryAsync()
    {
        await FileManager.SavePlaylistDataAsync(Playlists);
        await FileManager.SavePlaylistDataToM3u8Async();
    }
}
