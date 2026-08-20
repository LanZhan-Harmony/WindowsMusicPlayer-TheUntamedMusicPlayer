using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace UntamedMusicPlayer.Core.Models;

/// <summary>
/// Holds the observable state of one online search result set.
/// </summary>
/// <typeparam name="T">The type of item returned by the search.</typeparam>
public sealed class OnlineSearchState<T> : ObservableObject
{
    private string _query = string.Empty;
    private int _totalCount;
    private bool _hasMore;
    private bool _isLoading;
    private bool _isLoadingMore;
    private string? _errorMessage;

    public ObservableCollection<T> Items { get; } = [];

    public string Query
    {
        get => _query;
        private set => SetProperty(ref _query, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public bool HasMore
    {
        get => _hasMore;
        private set => SetProperty(ref _hasMore, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Clears the result set and returns the state to its initial state.
    /// </summary>
    public void Reset(string? query = null)
    {
        Items.Clear();
        Query = query ?? string.Empty;
        TotalCount = 0;
        HasMore = false;
        IsLoading = false;
        IsLoadingMore = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Replaces all loaded items with a new result set and completes loading.
    /// </summary>
    /// <param name="items">Items from the first page or a refreshed search.</param>
    /// <param name="totalCount">
    /// The total number of matching items. When omitted, the loaded item count is used.
    /// </param>
    /// <param name="hasMore">Whether another page is available.</param>
    /// <param name="query">A new query, or <see langword="null"/> to retain the current query.</param>
    public void Replace(
        IEnumerable<T> items,
        int? totalCount = null,
        bool hasMore = false,
        string? query = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        var replacement = new List<T>(items);

        Items.Clear();
        foreach (var item in replacement)
        {
            Items.Add(item);
        }

        if (query is not null)
        {
            Query = query;
        }

        TotalCount = totalCount ?? replacement.Count;
        HasMore = hasMore;
        IsLoading = false;
        IsLoadingMore = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Appends a page of items and completes loading more results.
    /// </summary>
    /// <param name="items">Items from the next page.</param>
    /// <param name="totalCount">
    /// The total number of matching items. When omitted, the loaded count is retained or increased.
    /// </param>
    /// <param name="hasMore">Whether another page is available; <see langword="null"/> retains the current value.</param>
    public void Append(
        IEnumerable<T> items,
        int? totalCount = null,
        bool? hasMore = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        var additions = new List<T>(items);

        foreach (var item in additions)
        {
            Items.Add(item);
        }

        TotalCount = totalCount ?? Math.Max(TotalCount, Items.Count);
        if (hasMore.HasValue)
        {
            HasMore = hasMore.Value;
        }

        IsLoading = false;
        IsLoadingMore = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marks an initial search request as active.
    /// </summary>
    public void BeginLoading(string? query = null)
    {
        if (query is not null)
        {
            Query = query;
        }

        ErrorMessage = null;
        IsLoadingMore = false;
        IsLoading = true;
    }

    /// <summary>
    /// Marks a request for the next page as active.
    /// </summary>
    public void BeginLoadingMore()
    {
        ErrorMessage = null;
        IsLoading = false;
        IsLoadingMore = true;
    }

    /// <summary>
    /// Stops loading and exposes an error for the current result set.
    /// </summary>
    public void SetError(string? errorMessage)
    {
        IsLoading = false;
        IsLoadingMore = false;
        ErrorMessage = errorMessage;
    }
}
