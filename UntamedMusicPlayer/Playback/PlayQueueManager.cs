using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;
using ZLinq;

namespace UntamedMusicPlayer.Playback;

public partial class PlayQueueManager : ObservableObject
{
    private readonly PlaybackState _state;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentQueue))]
    private partial ObservableCollection<IndexedPlayQueueSong> NormalPlayQueue { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentQueue))]
    private partial ObservableCollection<IndexedPlayQueueSong> ShuffledPlayQueue { get; set; } = [];

    public ObservableCollection<IndexedPlayQueueSong> CurrentQueue =>
        _state.ShuffleMode == ShuffleState.Normal ? NormalPlayQueue : ShuffledPlayQueue;

    [ObservableProperty]
    public partial string PlayQueueName { get; set; } = "";

    public PlayQueueManager(PlaybackState state)
    {
        _state = state;
        _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaybackState.ShuffleMode))
        {
            UpdateQueueIndexes();
        }
    }

    public void SetPlayQueue(string name, IList<IBriefSongInfoBase> list)
    {
        PlayQueueName = name;
        NormalPlayQueue =
        [
            .. list.AsValueEnumerable()
                .Select((song, index) => new IndexedPlayQueueSong(index, song)),
        ];
        _state.PlayQueueCount = NormalPlayQueue.Count;
        if (_state.ShuffleMode == ShuffleState.Shuffled)
        {
            UpdateShufflePlayQueue();
            UpdateQueueIndexes();
        }
    }

    public void AddSongToNextPlay(IBriefSongInfoBase info)
    {
        var insertIndex = _state.PlayQueueIndex + 1;
        CurrentQueue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, info));
        _state.PlayQueueCount++;
        UpdateQueueIndexes(insertIndex + 1);
        if (_state.ShuffleMode == ShuffleState.Shuffled)
        {
            NormalPlayQueue.Add(new IndexedPlayQueueSong(NormalPlayQueue.Count, info));
        }
    }

    public int GetPreviousSongIndex()
    {
        if (_state.RepeatMode == RepeatState.RepeatAll)
        {
            return (_state.PlayQueueIndex + _state.PlayQueueCount - 1) % _state.PlayQueueCount;
        }
        return _state.PlayQueueIndex > 0 ? _state.PlayQueueIndex - 1 : _state.PlayQueueIndex;
    }

    public (int, bool) GetNextSongIndex()
    {
        var newIndex =
            _state.PlayQueueIndex < _state.PlayQueueCount - 1 ? _state.PlayQueueIndex + 1 : 0;
        var isLast = _state.PlayQueueIndex >= _state.PlayQueueCount - 1;
        if (_state.RepeatMode == RepeatState.RepeatAll)
        {
            newIndex = (_state.PlayQueueIndex + 1) % _state.PlayQueueCount;
            isLast = false;
        }
        return (newIndex, isLast);
    }

    public IndexedPlayQueueSong? GetCurrentSong()
    {
        return _state.PlayQueueIndex < CurrentQueue.Count
            ? CurrentQueue[_state.PlayQueueIndex]
            : null;
    }

    private void UpdateShufflePlayQueue()
    {
        ShuffledPlayQueue = [.. NormalPlayQueue.AsValueEnumerable().OrderBy(x => Guid.NewGuid())];
    }

    private void UpdateQueueIndexes(int startIndex = 0)
    {
        for (var i = startIndex; i < CurrentQueue.Count; i++)
        {
            CurrentQueue[i].Index = i;
        }
    }
}
