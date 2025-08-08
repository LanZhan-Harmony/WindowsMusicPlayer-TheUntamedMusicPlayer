using System.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Helpers;

/// <summary>
/// 为 ListView 提供当前播放歌曲高亮功能的附加属性
/// </summary>
public static class CurrentSongHighlightExtensions
{
    private static readonly Dictionary<ListViewBase, object> _registeredListViews = [];

    /// <summary>
    /// 当前播放歌曲的前景色画刷
    /// </summary>
    public static readonly DependencyProperty PlayingBrushProperty =
        DependencyProperty.RegisterAttached(
            "PlayingBrush",
            typeof(Brush),
            typeof(CurrentSongHighlightExtensions),
            new PropertyMetadata(null, OnHighlightPropertyChanged)
        );

    /// <summary>
    /// 非当前播放歌曲的前景色画刷
    /// </summary>
    public static readonly DependencyProperty NotPlayingBrushProperty =
        DependencyProperty.RegisterAttached(
            "NotPlayingBrush",
            typeof(Brush),
            typeof(CurrentSongHighlightExtensions),
            new PropertyMetadata(null, OnHighlightPropertyChanged)
        );

    /// <summary>
    /// 是否为播放队列模式，启用时会额外检查 PlayQueueIndex 匹配
    /// </summary>
    public static readonly DependencyProperty IsPlayQueueProperty =
        DependencyProperty.RegisterAttached(
            "IsPlayQueue",
            typeof(bool),
            typeof(CurrentSongHighlightExtensions),
            new PropertyMetadata(false, OnHighlightPropertyChanged)
        );

    public static Brush GetPlayingBrush(DependencyObject obj) =>
        (Brush)obj.GetValue(PlayingBrushProperty);

    public static void SetPlayingBrush(DependencyObject obj, Brush value) =>
        obj.SetValue(PlayingBrushProperty, value);

    public static Brush GetNotPlayingBrush(DependencyObject obj) =>
        (Brush)obj.GetValue(NotPlayingBrushProperty);

    public static void SetNotPlayingBrush(DependencyObject obj, Brush value) =>
        obj.SetValue(NotPlayingBrushProperty, value);

    public static bool GetIsPlayQueue(DependencyObject obj) =>
        (bool)obj.GetValue(IsPlayQueueProperty);

    public static void SetIsPlayQueue(DependencyObject obj, bool value) =>
        obj.SetValue(IsPlayQueueProperty, value);

    private static void OnHighlightPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not ListViewBase listView)
        {
            return;
        }

        // 移除旧的事件处理器
        UnregisterListView(listView);

        // 如果启用了高亮功能且设置了画刷，则注册事件处理器
        if (GetPlayingBrush(listView) is not null && GetNotPlayingBrush(listView) is not null)
        {
            RegisterListView(listView);
        }
    }

    private static void RegisterListView(ListViewBase listView)
    {
        if (_registeredListViews.ContainsKey(listView))
        {
            return;
        }

        // 注册事件处理器
        listView.ContainerContentChanging += OnContainerContentChanging;
        listView.Unloaded += OnListViewUnloaded;
        Data.MusicPlayer.PropertyChanged += OnMusicPlayerPropertyChanged;

        // 记录已注册的 ListView
        _registeredListViews[listView] = new object();

        // 立即更新当前显示的项目
        UpdateAllVisibleItems(listView);
    }

    /// <summary>
    /// 手动重新激活指定页面中的 ListView 的高亮功能
    /// </summary>
    /// <param name="page">需要重新激活的页面</param>
    public static void ReactivateHighlightForPage(Page? page)
    {
        if (page is null)
        {
            return;
        }

        // 查找页面中所有具有高亮属性的 ListView
        var listViews = FindAllListViewsWithHighlight(page);

        foreach (var listView in listViews)
        {
            // 如果 ListView 有高亮属性但未注册，则重新注册
            if (!_registeredListViews.ContainsKey(listView))
            {
                RegisterListView(listView);
            }
            else
            {
                // 如果已经注册，立即更新显示
                UpdateAllVisibleItems(listView);
            }
        }
    }

    /// <summary>
    /// 递归查找页面中所有具有高亮属性的 ListView
    /// </summary>
    /// <param name="parent">父级容器</param>
    /// <returns>具有高亮属性的 ListView 列表</returns>
    private static IEnumerable<ListViewBase> FindAllListViewsWithHighlight(DependencyObject? parent)
    {
        if (parent is null)
        {
            return [];
        }

        return parent
            .FindDescendants()
            .OfType<ListViewBase>()
            .Where(listView =>
                GetPlayingBrush(listView) is not null && GetNotPlayingBrush(listView) is not null
            );
    }

    private static void UnregisterListView(ListViewBase listView)
    {
        if (!_registeredListViews.ContainsKey(listView))
        {
            return;
        }

        // 移除事件处理器
        listView.ContainerContentChanging -= OnContainerContentChanging;
        listView.Unloaded -= OnListViewUnloaded;

        // 从记录中移除
        _registeredListViews.Remove(listView);

        // 如果没有注册的 ListView 了，移除全局事件监听
        if (_registeredListViews.Count == 0)
        {
            Data.MusicPlayer.PropertyChanged -= OnMusicPlayerPropertyChanged;
        }
    }

    private static void OnListViewUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListViewBase listView)
        {
            UnregisterListView(listView);
        }
    }

    private static void OnMusicPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName == nameof(MusicPlayer.CurrentSong)
            || e.PropertyName == nameof(MusicPlayer.PlayQueueIndex)
        )
        {
            // 当前歌曲或播放队列索引变化时，更新所有注册的 ListView
            foreach (var listView in _registeredListViews.Keys.ToList())
            {
                listView.DispatcherQueue.TryEnqueue(() => UpdateAllVisibleItems(listView));
            }
        }
    }

    private static void OnContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args
    )
    {
        if (args.Phase > 0 || args.InRecycleQueue)
        {
            return;
        }

        if (
            args.ItemContainer is ListViewItem container
            && args.Item is IBriefSongInfoBase songInfo
        )
        {
            UpdateItemHighlight(sender, container, songInfo);
        }
    }

    private static void UpdateAllVisibleItems(ListViewBase listView)
    {
        foreach (var item in listView.Items)
        {
            if (
                listView.ContainerFromItem(item) is ListViewItem container
                && item is IBriefSongInfoBase songInfo
            )
            {
                UpdateItemHighlight(listView, container, songInfo);
            }
        }
    }

    private static void UpdateItemHighlight(
        ListViewBase listView,
        ListViewItem container,
        IBriefSongInfoBase songInfo
    )
    {
        if (container.ContentTemplateRoot is not Grid grid)
        {
            return;
        }

        // 判断是否为当前播放歌曲
        var currentSong = Data.MusicPlayer.CurrentSong;
        var isPlayQueue = GetIsPlayQueue(listView);
        var isCurrentlyPlaying = IsCurrentlyPlaying(currentSong, songInfo, isPlayQueue);

        // 获取对应的画刷
        var (brush, visibility) = isCurrentlyPlaying
            ? (GetPlayingBrush(listView), Visibility.Visible)
            : (GetNotPlayingBrush(listView), Visibility.Collapsed);

        // 查找并更新所有文本块的前景色
        UpdateTextBlocksForeground(grid, brush, visibility);
    }

    private static bool IsCurrentlyPlaying(
        IDetailedSongInfoBase? currentSong,
        IBriefSongInfoBase songInfo,
        bool isPlayQueue
    )
    {
        if (currentSong is null)
        {
            return false;
        }

        bool isSameSong;

        // 支持本地和在线歌曲的比较
        if (currentSong.IsOnline && songInfo is IBriefOnlineSongInfo onlineSong)
        {
            isSameSong = ((IDetailedOnlineSongInfo)currentSong).ID == onlineSong.ID;
        }
        else if (!currentSong.IsOnline && songInfo is BriefLocalSongInfo localSong)
        {
            isSameSong = ((BriefLocalSongInfo)currentSong).Path == localSong.Path;
        }
        else
        {
            return false;
        }

        // 如果是播放队列模式，还需要检查 PlayQueueIndex
        if (isPlayQueue && isSameSong)
        {
            return songInfo.PlayQueueIndex == Data.MusicPlayer.PlayQueueIndex;
        }

        return isSameSong;
    }

    private static void UpdateTextBlocksForeground(Grid grid, Brush brush, Visibility visibility)
    {
        // 常见的文本块名称
        var textBlockNames = new[]
        {
            "TitleText",
            "ArtistText",
            "AlbumText",
            "YearText",
            "GenreText",
            "DurationText",
        };

        foreach (var name in textBlockNames)
        {
            if (grid.FindName(name) is TextBlock textBlock)
            {
                textBlock.Foreground = brush;
            }
        }

        // 支持 FontIcon (如播放队列中的音乐图标)
        if (
            grid.FindName("MusicFontIcon") is FontIcon musicFontIcon
            && grid.FindName("PlayingFontIcon") is FontIcon playingFontIcon
        )
        {
            musicFontIcon.Foreground = brush;
            playingFontIcon.Visibility = visibility;
        }
    }
}
