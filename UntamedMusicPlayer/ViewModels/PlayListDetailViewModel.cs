using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Messages;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.Views;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class PlayListDetailViewModel
    : ObservableRecipient,
        IRecipient<PlaylistRenameMessage>,
        IRecipient<PlaylistChangeMessage>,
        IDisposable
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer;
    private readonly CloudMusicApiService _cloudApi;

    [ObservableProperty]
    public partial PlaylistInfo Playlist { get; set; } = null!;

    [ObservableProperty]
    public partial string PlaylistName { get; set; } = "";

    [ObservableProperty]
    public partial string TotalSongNumStr { get; set; } = "";

    [ObservableProperty]
    public partial ObservableCollection<IndexedPlaylistSong> SongList { get; set; }

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    public PlayListDetailViewModel(MusicPlayer musicPlayer, CloudMusicApiService cloudApi)
        : base(StrongReferenceMessenger.Default)
    {
        _musicPlayer = musicPlayer;
        _cloudApi = cloudApi;
        Messenger.Register<PlaylistRenameMessage>(this);
        Messenger.Register<PlaylistChangeMessage>(this);
        SongList = [];
        IsPlayAllButtonEnabled = false;
    }

    public void Initialize(PlaylistInfo playlist)
    {
        Playlist = playlist;
        PlaylistName = playlist.Name;
        TotalSongNumStr = playlist.TotalSongNumStr;
        SongList = playlist.SongList;
        IsPlayAllButtonEnabled = SongList.Count > 0;
    }

    public void Receive(PlaylistRenameMessage message)
    {
        if (PlaylistName == message.OldName)
        {
            PlaylistName = message.NewName;
        }
    }

    public void Receive(PlaylistChangeMessage message)
    {
        if (PlaylistName == message.Playlist.Name)
        {
            TotalSongNumStr = Playlist.TotalSongNumStr;
            SongList = Playlist.SongList;
            IsPlayAllButtonEnabled = SongList.Count > 0;
        }
    }

    [RelayCommand]
    private void PlayAll()
    {
        if (SongList.Count == 0)
        {
            return;
        }
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        _musicPlayer.QueueManager.SetNormalPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    private async Task AddAllToPlaylist(PlaylistInfo playlist)
    {
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }

    [RelayCommand]
    private void AddAllToPlayQueue()
    {
        if (SongList.Count == 0)
        {
            return;
        }
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"Songs:Playlist:{Playlist.Name}",
                songList
            );
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(songList);
        }
    }

    public void SongListView_ItemClick(IndexedPlaylistSong info)
    {
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        _musicPlayer.QueueManager.SetNormalPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
        _musicPlayer.PlaySongByInfo(info.Song);
    }

    [RelayCommand]
    private void Play(IBriefSongInfoBase info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"Songs:Playlist:{Playlist.Name}",
            SongList.AsValueEnumerable().Select(s => s.Song).ToArray()
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    private void PlayNext(IBriefSongInfoBase info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"Songs:Playlist:{Playlist.Name}:Part",
                list
            );
            _musicPlayer.PlaySongByInfo(info);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]
    private void AddToPlayQueue(IBriefSongInfoBase info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"Songs:Playlist:{Playlist.Name}:Part",
                list
            );
            _musicPlayer.PlaySongByInfo(info);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd([info]);
        }
    }

    [RelayCommand]
    private async Task AddToPlaylist(Tuple<IBriefSongInfoBase, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]
    private async Task Remove(IndexedPlaylistSong info)
    {
        await App.GetService<PlaylistLibrary>().DeleteFromPlaylist(Playlist, info);
        if (SongList.Count == 0)
        {
            IsPlayAllButtonEnabled = false;
        }
    }

    [RelayCommand]
    private void MoveUp(IndexedPlaylistSong info)
    {
        App.GetService<PlaylistLibrary>().MoveUpInPlaylist(Playlist, info);
    }

    [RelayCommand]
    private void MoveDown(IndexedPlaylistSong info)
    {
        App.GetService<PlaylistLibrary>().MoveDownInPlaylist(Playlist, info);
    }

    [RelayCommand]
    private async Task ShowAlbum(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(localAlbumInfo, nameof(PlayListDetailPage)),
                    NavigationTransition.Suppress
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineAlbumInfo = await CloudMusicModelFactory.CreateAlbumFromSongAsync(
                onlineInfo,
                _cloudApi
            );
            if (onlineAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineAlbumDetailPage),
                    new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(PlayListDetailPage)),
                    NavigationTransition.Suppress
                );
            }
        }
    }

    [RelayCommand]
    private async Task ShowArtist(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = App.GetService<MusicLibrary>()
                .GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalArtistDetailPage),
                    new LocalArtistNavigationArgs(localArtistInfo, nameof(PlayListDetailPage)),
                    NavigationTransition.Suppress
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineArtistInfo = await CloudMusicModelFactory.CreateArtistFromSongAsync(
                onlineInfo,
                _cloudApi
            );
            if (onlineArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineArtistDetailPage),
                    new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(PlayListDetailPage)),
                    NavigationTransition.Suppress
                );
            }
        }
    }

    public void SongListView_DragItemsCompleted(IEnumerable<IndexedPlaylistSong> songs)
    {
        var reorderedSongs = songs.ToArray();
        if (reorderedSongs.Length > 0)
        {
            Playlist.ReindexSongs();
            Messenger.Send(new HavePlaylistMessage(true));
            _ = FileManager.SavePlaylistDataAsync(App.GetService<PlaylistLibrary>().Playlists);
        }
    }

    public void Dispose()
    {
        Messenger.Unregister<PlaylistRenameMessage>(this);
        Messenger.Unregister<PlaylistChangeMessage>(this);
    }
}
