using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;
using ZLogger;

namespace UntamedMusicPlayer.Core.Services;

/// <summary>
/// Coordinates the active online search and exposes provider-independent result state to the UI.
/// </summary>
public sealed partial class OnlineMusicLibrary : ObservableObject
{
    private static readonly ILogger _logger = CoreLoggingService.CreateLogger<OnlineMusicLibrary>();
    private readonly CloudMusicSearchService _cloudSearch;
    private readonly SemaphoreSlim _searchSemaphore = new(1, 1);
    private bool _isSearchingMore;

    public OnlineMusicLibrary(CloudMusicSearchService cloudSearch)
    {
        _cloudSearch = cloudSearch ?? throw new ArgumentNullException(nameof(cloudSearch));
    }

    // These states are the only application-facing search result source.
    public OnlineSearchState<IBriefOnlineSongInfo> SongSearchState { get; } = new();
    public OnlineSearchState<IBriefOnlineAlbumInfo> AlbumSearchState { get; } = new();
    public OnlineSearchState<IBriefOnlineArtistInfo> ArtistSearchState { get; } = new();
    public OnlineSearchState<IBriefOnlinePlaylistInfo> PlaylistSearchState { get; } = new();

    private string? _lastSearchKeyWords;
    private byte? _lastMusicLibraryIndex;

    /// <summary>
    /// Page index: 0 songs, 1 albums, 2 artists, 3 playlists.
    /// </summary>
    public byte PageIndex { get; set; }

    /// <summary>
    /// Music library index: 0 is NetEase Cloud Music.
    /// </summary>
    public byte MusicLibraryIndex { get; set; }

    public string SuggestKeyWords { get; set; } = null!;

    [ObservableProperty]
    public partial string SearchKeyWords { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsKeyWordsTextBlockVisible { get; set; }

    [ObservableProperty]
    public partial bool IsNetworkErrorVisible { get; set; }

    [ObservableProperty]
    public partial bool IsListViewVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; }

    [ObservableProperty]
    public partial bool IsSearchMoreProgressRingActive { get; set; }

    [ObservableProperty]
    public partial List<SuggestResult> SuggestResultList { get; set; } = [];

    public async Task Search()
    {
        var query = SearchKeyWords?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            IsKeyWordsTextBlockVisible = false;
            IsNetworkErrorVisible = false;
            IsListViewVisible = false;
            return;
        }

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            IsKeyWordsTextBlockVisible = false;
            IsNetworkErrorVisible = true;
            IsListViewVisible = false;
            return;
        }

        if (ShouldSkipSearch(query))
        {
            IsKeyWordsTextBlockVisible = true;
            IsNetworkErrorVisible = false;
            IsListViewVisible = true;
            return;
        }

        await _searchSemaphore.WaitAsync();
        try
        {
            // The query may have changed while waiting for another search to finish.
            query = SearchKeyWords?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            IsKeyWordsTextBlockVisible = false;
            IsNetworkErrorVisible = false;
            IsListViewVisible = false;
            IsSearchProgressRingActive = true;
            BeginLoadingCurrentState(query);

            await SearchCurrentPageAsync(query);

            _lastSearchKeyWords = query;
            _lastMusicLibraryIndex = MusicLibraryIndex;
            IsKeyWordsTextBlockVisible = true;
            IsListViewVisible = true;
        }
        catch (Exception ex)
        {
            SetCurrentStateError(ex.Message);
            _logger.ZLogInformation(ex, $"在线搜索{query}时发生错误");
        }
        finally
        {
            IsSearchProgressRingActive = false;
            _searchSemaphore.Release();
        }
    }

    private bool ShouldSkipSearch(string query)
    {
        if (_lastSearchKeyWords != query || _lastMusicLibraryIndex != MusicLibraryIndex)
        {
            return false;
        }

        return CurrentStateQuery() == query && GetCurrentStateError() is null;
    }

    public async Task ForceSearch()
    {
        _lastSearchKeyWords = null;
        _lastMusicLibraryIndex = null;
        ResetCurrentState(SearchKeyWords?.Trim());
        await Search();
    }

    public async Task SearchMore()
    {
        if (_isSearchingMore || !CurrentStateHasMore())
        {
            return;
        }

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            return;
        }

        _isSearchingMore = true;
        IsSearchMoreProgressRingActive = true;
        try
        {
            await _searchSemaphore.WaitAsync();
            try
            {
                if (!CurrentStateHasMore())
                {
                    return;
                }

                BeginLoadingMoreCurrentState();
                await SearchMoreCurrentPageAsync();
            }
            finally
            {
                _searchSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            SetCurrentStateError(ex.Message);
            _logger.ZLogInformation(ex, $"在线搜索更多{SearchKeyWords}时发生错误");
        }
        finally
        {
            _isSearchingMore = false;
            IsSearchMoreProgressRingActive = false;
        }
    }

    public async Task UpdateSuggestResult()
    {
        if (string.IsNullOrWhiteSpace(SuggestKeyWords))
        {
            ClearSuggestResult();
            return;
        }

        if (MusicLibraryIndex != 0)
        {
            ClearSuggestResult();
            return;
        }

        SuggestResultList = await _cloudSearch.GetSuggestionsAsync(SuggestKeyWords);
    }

    public void ClearSuggestResult() => SuggestResultList = [];

    public Task RetryAsync() => ForceSearch();

    private bool CurrentStateHasMore() =>
        PageIndex switch
        {
            0 => SongSearchState.HasMore,
            1 => AlbumSearchState.HasMore,
            2 => ArtistSearchState.HasMore,
            3 => PlaylistSearchState.HasMore,
            _ => false,
        };

    private string CurrentStateQuery() =>
        PageIndex switch
        {
            0 => SongSearchState.Query,
            1 => AlbumSearchState.Query,
            2 => ArtistSearchState.Query,
            3 => PlaylistSearchState.Query,
            _ => string.Empty,
        };

    private string? GetCurrentStateError() =>
        PageIndex switch
        {
            0 => SongSearchState.ErrorMessage,
            1 => AlbumSearchState.ErrorMessage,
            2 => ArtistSearchState.ErrorMessage,
            3 => PlaylistSearchState.ErrorMessage,
            _ => null,
        };

    private void ResetCurrentState(string? query)
    {
        switch (PageIndex)
        {
            case 0:
                SongSearchState.Reset(query);
                break;
            case 1:
                AlbumSearchState.Reset(query);
                break;
            case 2:
                ArtistSearchState.Reset(query);
                break;
            case 3:
                PlaylistSearchState.Reset(query);
                break;
        }
    }

    private void BeginLoadingCurrentState(string query)
    {
        switch (PageIndex)
        {
            case 0:
                SongSearchState.BeginLoading(query);
                break;
            case 1:
                AlbumSearchState.BeginLoading(query);
                break;
            case 2:
                ArtistSearchState.BeginLoading(query);
                break;
            case 3:
                PlaylistSearchState.BeginLoading(query);
                break;
            default:
                throw new InvalidOperationException($"未知的在线页面索引: {PageIndex}");
        }
    }

    private void BeginLoadingMoreCurrentState()
    {
        switch (PageIndex)
        {
            case 0:
                SongSearchState.BeginLoadingMore();
                break;
            case 1:
                AlbumSearchState.BeginLoadingMore();
                break;
            case 2:
                ArtistSearchState.BeginLoadingMore();
                break;
            case 3:
                PlaylistSearchState.BeginLoadingMore();
                break;
            default:
                throw new InvalidOperationException($"未知的在线页面索引: {PageIndex}");
        }
    }

    private void SetCurrentStateError(string message)
    {
        switch (PageIndex)
        {
            case 0:
                SongSearchState.SetError(message);
                break;
            case 1:
                AlbumSearchState.SetError(message);
                break;
            case 2:
                ArtistSearchState.SetError(message);
                break;
            case 3:
                PlaylistSearchState.SetError(message);
                break;
        }
    }

    private Task SearchCurrentPageAsync(string query)
    {
        if (MusicLibraryIndex != 0)
        {
            ResetCurrentState(query);
            return Task.CompletedTask;
        }

        return PageIndex switch
        {
            0 => SearchSongsAsync(query),
            1 => SearchAlbumsAsync(query),
            2 => SearchArtistsAsync(query),
            3 => SearchPlaylistsAsync(query),
            _ => Task.CompletedTask,
        };
    }

    private async Task SearchSongsAsync(string query)
    {
        var result = await _cloudSearch.SearchSongsAsync(query);
        SongSearchState.Replace(result.Items, result.TotalCount, result.HasMore, result.Query);
    }

    private async Task SearchAlbumsAsync(string query)
    {
        var result = await _cloudSearch.SearchAlbumsAsync(query);
        AlbumSearchState.Replace(result.Items, result.TotalCount, result.HasMore, result.Query);
    }

    private async Task SearchArtistsAsync(string query)
    {
        var result = await _cloudSearch.SearchArtistsAsync(query);
        ArtistSearchState.Replace(result.Items, result.TotalCount, result.HasMore, result.Query);
    }

    private async Task SearchPlaylistsAsync(string query)
    {
        var result = await _cloudSearch.SearchPlaylistsAsync(query);
        PlaylistSearchState.Replace(result.Items, result.TotalCount, result.HasMore, result.Query);
    }

    private async Task SearchMoreCurrentPageAsync()
    {
        if (MusicLibraryIndex != 0)
        {
            return;
        }

        switch (PageIndex)
        {
            case 0:
            {
                var result = await _cloudSearch.SearchMoreSongsAsync();
                SongSearchState.Append(result.Items, result.TotalCount, result.HasMore);
                break;
            }
            case 1:
            {
                var result = await _cloudSearch.SearchMoreAlbumsAsync();
                AlbumSearchState.Append(result.Items, result.TotalCount, result.HasMore);
                break;
            }
            case 2:
            {
                var result = await _cloudSearch.SearchMoreArtistsAsync();
                ArtistSearchState.Append(result.Items, result.TotalCount, result.HasMore);
                break;
            }
            case 3:
            {
                var result = await _cloudSearch.SearchMorePlaylistsAsync();
                PlaylistSearchState.Append(result.Items, result.TotalCount, result.HasMore);
                break;
            }
            default:
                throw new InvalidOperationException($"未知的在线页面索引: {PageIndex}");
        }
    }
}
