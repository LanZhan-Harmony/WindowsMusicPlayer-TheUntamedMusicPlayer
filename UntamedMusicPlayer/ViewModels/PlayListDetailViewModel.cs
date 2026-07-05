using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;
using Windows.ApplicationModel.DataTransfer;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class PlayListDetailViewModel
    : ObservableRecipient,
        IRecipient<PlaylistRenameMessage>,
        IRecipient<PlaylistChangeMessage>,
        IDisposable
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    [ObservableProperty]
    public partial PlaylistInfo Playlist { get; set; } = null!;

    [ObservableProperty]
    public partial string PlaylistName { get; set; } = "";

    [ObservableProperty]
    public partial string TotalSongNumStr { get; set; } = "";

    [ObservableProperty]
    public partial WriteableBitmap? Cover { get; set; }
        = null;

    [ObservableProperty]
    public partial ObservableCollection<IndexedPlaylistSong> SongList { get; set; }

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    public PlayListDetailViewModel()
        : base(StrongReferenceMessenger.Default)
    {
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
        Cover = CoverManager.GetPlaylistCoverBitmap(playlist);
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
            Cover = CoverManager.GetPlaylistCoverBitmap(Playlist);
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
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
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
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd(songList);
        }
    }

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
        if (e.ClickedItem is IndexedPlaylistSong indexedInfo)
        {
            App.GetService<MusicPlayer>().PlaySongByInfo(indexedInfo.Song);
        }
    }

    [RelayCommand]
    private void Play(IBriefSongInfoBase info)
    {
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"Songs:Playlist:{Playlist.Name}",
            SongList.AsValueEnumerable().Select(s => s.Song).ToArray()
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]
    private void PlayNext(IBriefSongInfoBase info)
    {
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"Songs:Playlist:{Playlist.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]
    private void AddToPlayQueue(IBriefSongInfoBase info)
    {
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"Songs:Playlist:{Playlist.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd([info]);
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
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineAlbumDetailPage),
                    new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(PlayListDetailPage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    [RelayCommand]
    private async Task ShowArtist(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = App.GetService<MusicLibrary>().GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalArtistDetailPage),
                    new LocalArtistNavigationArgs(localArtistInfo, nameof(PlayListDetailPage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineArtistDetailPage),
                    new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(PlayListDetailPage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    public void SongListView_DragItemsStarting(object _, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0)
        {
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    public void SongListView_DragItemsCompleted(ListViewBase _1, DragItemsCompletedEventArgs args)
    {
        if (args.DropResult == DataPackageOperation.Move && args.Items.Count > 0)
        {
            var songs = args.Items.AsValueEnumerable().OfType<IndexedPlaylistSong>().ToArray();
            if (songs.Length == 0)
            {
                return;
            }
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

