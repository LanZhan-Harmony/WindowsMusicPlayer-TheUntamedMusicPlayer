using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI;
using ZLinq;

namespace UntamedMusicPlayer.Playback;

public partial class PlayQueueManager : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly SharedPlaybackState _state;

    private string _playQueueName = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentQueue))]
    private partial ObservableCollection<IndexedPlayQueueSong> NormalPlayQueue { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentQueue))]
    private partial ObservableCollection<IndexedPlayQueueSong> ShuffledPlayQueue { get; set; } = [];

    public ObservableCollection<IndexedPlayQueueSong> CurrentQueue =>
        _state.ShuffleMode == ShuffleState.Normal ? NormalPlayQueue : ShuffledPlayQueue;

    public event Action? OnPlayQueueEmpty;
    public event Action? OnCurrentSongRemoved;

    public PlayQueueManager(SharedPlaybackState state)
    {
        _state = state;
        _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? _, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedPlaybackState.PlayQueueIndex):
                UpdateQueueIndexes();
                break;
            case nameof(SharedPlaybackState.CurrentBriefSong):
                _ = SaveCurrentBriefSongAsync();
                break;
            default:
                break;
        }
    }

    public void SetNormalPlayQueue(string name, IReadOnlyList<IBriefSongInfoBase> list)
    {
        if (_state.PlayQueueCount == list.Count && _playQueueName == name)
        {
            return;
        }
        _playQueueName = name;
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
        _ = FileManager.SavePlayQueueDataAsync(NormalPlayQueue, ShuffledPlayQueue);
    }

    public void SetShuffledPlayQueue(string name, IReadOnlyList<IBriefSongInfoBase> list)
    {
        if (
            _state.ShuffleMode == ShuffleState.Shuffled
            && _state.PlayQueueCount == list.Count
            && _playQueueName == name
        )
        {
            return;
        }
        _state.ShuffleMode = ShuffleState.Shuffled;
        _playQueueName = name;
        NormalPlayQueue =
        [
            .. list.AsValueEnumerable()
                .Select((song, index) => new IndexedPlayQueueSong(index, song)),
        ];
        _state.PlayQueueCount = NormalPlayQueue.Count;
        UpdateShufflePlayQueue();
        UpdateQueueIndexes();
        _ = FileManager.SavePlayQueueDataAsync(NormalPlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 将歌曲添加到下一首播放
    /// </summary>
    /// <param name="songs"></param>
    public void AddSongsToNextPlay(IReadOnlyList<IBriefSongInfoBase> songs)
    {
        var insertIndex = _state.PlayQueueIndex + 1;
        foreach (var song in songs)
        {
            CurrentQueue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, song));
            insertIndex++;
        }
        _state.PlayQueueCount += songs.Count;
        UpdateQueueIndexes(insertIndex);
        if (_state.ShuffleMode == ShuffleState.Shuffled)
        {
            foreach (var song in songs)
            {
                NormalPlayQueue.Add(new IndexedPlayQueueSong(NormalPlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 将歌曲添加到播放队列
    /// </summary>
    /// <param name="songs"></param>
    public void AddSongsToEnd(IReadOnlyList<IBriefSongInfoBase> songs)
    {
        foreach (var song in songs)
        {
            CurrentQueue.Add(new IndexedPlayQueueSong(CurrentQueue.Count, song));
        }
        _state.PlayQueueCount += songs.Count;
        if (_state.ShuffleMode == ShuffleState.Shuffled)
        {
            foreach (var song in songs)
            {
                NormalPlayQueue.Add(new IndexedPlayQueueSong(NormalPlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 将歌曲插入到指定位置
    /// </summary>
    /// <param name="songs"></param>
    /// <param name="index"></param>
    public void InsertSongsAt(IReadOnlyList<IBriefSongInfoBase> songs, int index)
    {
        var insertIndex = Math.Clamp(index, 0, _state.PlayQueueCount);
        foreach (var song in songs)
        {
            CurrentQueue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, song));
            insertIndex++;
        }
        _state.PlayQueueCount += songs.Count;
        UpdateQueueIndexes(insertIndex);
        if (insertIndex <= _state.PlayQueueIndex)
        {
            _state.PlayQueueIndex += songs.Count;
        }
        if (_state.ShuffleMode == ShuffleState.Shuffled)
        {
            foreach (var song in songs)
            {
                NormalPlayQueue.Add(new IndexedPlayQueueSong(NormalPlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 上移歌曲
    /// </summary>
    /// <param name="info"></param>
    public void MoveUpSong(IndexedPlayQueueSong info)
    {
        var currentIndex = info.Index;
        if (currentIndex <= 0)
        {
            return;
        }
        var targetIndex = currentIndex - 1;
        CurrentQueue.Move(currentIndex, targetIndex);
        UpdateQueueIndexes(targetIndex);
        if (_state.PlayQueueIndex == currentIndex)
        {
            _state.PlayQueueIndex = targetIndex;
        }
        else if (_state.PlayQueueIndex == targetIndex)
        {
            _state.PlayQueueIndex = currentIndex;
        }
    }

    /// <summary>
    /// 下移歌曲
    /// </summary>
    /// <returns></returns>
    public void MoveDownSong(IndexedPlayQueueSong info)
    {
        var currentIndex = info.Index;
        if (currentIndex >= _state.PlayQueueCount - 1)
        {
            return;
        }
        var targetIndex = currentIndex + 1;
        CurrentQueue.Move(currentIndex, targetIndex);
        UpdateQueueIndexes(currentIndex);
        if (_state.PlayQueueIndex == currentIndex)
        {
            _state.PlayQueueIndex = targetIndex;
        }
        else if (_state.PlayQueueIndex == targetIndex)
        {
            _state.PlayQueueIndex = currentIndex;
        }
    }

    /// <summary>
    /// 移除歌曲
    /// </summary>
    /// <returns></returns>
    public void RemoveSong(IndexedPlayQueueSong info)
    {
        var removingIndex = info.Index;
        CurrentQueue.RemoveAt(removingIndex);
        _state.PlayQueueCount--;
        if (_state.PlayQueueCount == 0)
        {
            OnPlayQueueEmpty?.Invoke();
            return;
        }
        if (removingIndex < _state.PlayQueueIndex)
        {
            _state.PlayQueueIndex--;
        }
        else if (removingIndex == _state.PlayQueueIndex)
        {
            if (removingIndex == _state.PlayQueueCount)
            {
                _state.PlayQueueIndex = 0;
            }
            // 保持当前索引不变，下一首歌会自动补上
            OnCurrentSongRemoved?.Invoke();
        }
        UpdateQueueIndexes(removingIndex);
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

    public void ShuffleModeUpdate()
    {
        _state.ShuffleMode =
            _state.ShuffleMode == ShuffleState.Normal ? ShuffleState.Shuffled : ShuffleState.Normal;
        if (_state.ShuffleMode == ShuffleState.Shuffled)
        {
            UpdateShufflePlayQueue();
        }
        else
        {
            ShuffledPlayQueue = [];
        }
        UpdateQueueIndexes();
        if (_state.CurrentSong is not null)
        {
            _state.PlayQueueIndex =
                CurrentQueue
                    .AsValueEnumerable()
                    .FirstOrDefault(info =>
                        SongComparer.CurrentIsSameSong(_state.CurrentSong, info.Song)
                    )
                    ?.Index ?? 0;
        }
    }

    public void RepeatModeUpdate() =>
        _state.RepeatMode = _state.RepeatMode switch
        {
            RepeatState.NoRepeat => RepeatState.RepeatAll,
            RepeatState.RepeatAll => RepeatState.RepeatOne,
            _ => RepeatState.NoRepeat,
        };

    private void UpdateShufflePlayQueue() =>
        ShuffledPlayQueue = [.. NormalPlayQueue.AsValueEnumerable().OrderBy(x => Guid.NewGuid())];

    private void UpdateQueueIndexes(int startIndex = 0)
    {
        for (var i = startIndex; i < CurrentQueue.Count; i++)
        {
            CurrentQueue[i].Index = i;
        }
    }

    public void Reset()
    {
        NormalPlayQueue.Clear();
        ShuffledPlayQueue.Clear();
        _state.PlayQueueIndex = -1;
        _state.PlayQueueCount = 0;
        _state.CurrentBriefSong = null;
        _state.CurrentSong = null;
        _playQueueName = "";
        _ = FileManager.SavePlayQueueDataAsync(NormalPlayQueue, ShuffledPlayQueue);
    }

    public async Task LoadStateAsync()
    {
        try
        {
            (NormalPlayQueue, ShuffledPlayQueue) = await FileManager.LoadPlayQueueDataAsync();
            _state.PlayQueueCount = NormalPlayQueue.Count;
            if (_state.PlayQueueCount == 0)
            {
                return;
            }

            _state.PlayQueueIndex = await _localSettingsService.ReadSettingAsync<int>(
                nameof(SharedPlaybackState.PlayQueueIndex)
            );
            var sourceModeName = await _localSettingsService.ReadSettingAsync<string>(
                nameof(SourceMode)
            );
            if (!Enum.TryParse<SourceMode>(sourceModeName, out var cacheMode))
            {
                return;
            }

            IBriefSongInfoBase? currentBriefSong = cacheMode switch
            {
                SourceMode.Local =>
                    await _localSettingsService.ReadSettingAsync<BriefLocalSongInfo>(
                        nameof(SharedPlaybackState.CurrentBriefSong)
                    ),
                SourceMode.Unknown =>
                    await _localSettingsService.ReadSettingAsync<BriefUnknownSongInfo>(
                        nameof(SharedPlaybackState.CurrentBriefSong)
                    ),
                SourceMode.Netease =>
                    await _localSettingsService.ReadSettingAsync<BriefCloudOnlineSongInfo>(
                        nameof(SharedPlaybackState.CurrentBriefSong)
                    ),
                _ => null,
            };
            if (
                currentBriefSong is not null
                && NormalPlayQueue.FirstOrDefault(song =>
                    SongComparer.IsSameSong(currentBriefSong, song.Song)
                )
                    is not null
            )
            {
                _state.CurrentBriefSong = currentBriefSong;
                _state.CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
                    currentBriefSong
                );
            }
        }
        catch
        {
            NormalPlayQueue = [];
            ShuffledPlayQueue = [];
            _state.CurrentBriefSong = null;
        }
    }

    public async Task SaveStateAsync()
    {
        try
        {
            await FileManager.SavePlayQueueDataAsync(NormalPlayQueue, ShuffledPlayQueue);
            await _localSettingsService.SaveSettingAsync(
                nameof(SharedPlaybackState.PlayQueueIndex),
                _state.PlayQueueIndex
            );
            await SaveCurrentBriefSongAsync();
        }
        catch { }
    }

    public async Task SaveCurrentBriefSongAsync()
    {
        try
        {
            await _localSettingsService.SaveSettingAsync(
                nameof(SharedPlaybackState.CurrentBriefSong),
                _state.CurrentBriefSong
            );
            var sourceMode = SourceModeHelper.GetSourceMode(_state.CurrentBriefSong);
            await _localSettingsService.SaveSettingAsync(nameof(SourceMode), $"{sourceMode}");
        }
        catch { }
    }
}
