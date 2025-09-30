using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using ZLinq;

namespace The_Untamed_Music_Player.Playback;

public partial class PlayQueueManager : ObservableObject
{
    private readonly PlaybackState _state;
    public int PlayQueueLength { get; set; }

    public PlayQueueManager(PlaybackState state)
    {
        _state = state;
        _state.PropertyChanged += OnStateChanged;
    }

    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> PlayQueue { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> ShuffledPlayQueue { get; set; } = [];

    [ObservableProperty]
    public partial string PlayQueueName { get; set; } = "";

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaybackState.ShuffleMode))
        {
            UpdateQueueIndexes();
        }
    }

    public void SetPlayQueue(string name, IEnumerable<IBriefSongInfoBase> list)
    {
        PlayQueueName = name;
        PlayQueue =
        [
            .. list.AsValueEnumerable()
                .Select((song, index) => new IndexedPlayQueueSong(index, song)),
        ];

        PlayQueueLength = list.AsValueEnumerable().Count();

        if (_state.ShuffleMode)
        {
            UpdateShufflePlayQueue();
            UpdateQueueIndexes();
        }
    }

    public void AddSongToNextPlay(IBriefSongInfoBase info)
    {
        var queue = _state.ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var insertIndex = _state.PlayQueueIndex + 1;

        queue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, info));
        PlayQueueLength++;

        UpdateQueueIndexes(insertIndex + 1);

        if (_state.ShuffleMode)
        {
            PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, info));
        }
    }

    public int GetNextSongIndex()
    {
        if (_state.RepeatMode == 1) // 列表循环
        {
            return (_state.PlayQueueIndex + 1) % PlayQueueLength;
        }

        return _state.PlayQueueIndex < PlayQueueLength - 1 ? _state.PlayQueueIndex + 1 : 0;
    }

    public int GetPreviousSongIndex()
    {
        if (_state.RepeatMode == 1) // 列表循环
        {
            return (_state.PlayQueueIndex + PlayQueueLength - 1) % PlayQueueLength;
        }

        return _state.PlayQueueIndex > 0 ? _state.PlayQueueIndex - 1 : _state.PlayQueueIndex;
    }

    public IndexedPlayQueueSong? GetCurrentSong()
    {
        var queue = _state.ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        return _state.PlayQueueIndex < queue.Count ? queue[_state.PlayQueueIndex] : null;
    }

    private void UpdateShufflePlayQueue()
    {
        ShuffledPlayQueue = [.. PlayQueue.AsValueEnumerable().OrderBy(x => Guid.NewGuid())];
    }

    private void UpdateQueueIndexes(int startIndex = 0)
    {
        var queue = _state.ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        for (var i = startIndex; i < queue.Count; i++)
        {
            queue[i].Index = i;
        }
    }
}
