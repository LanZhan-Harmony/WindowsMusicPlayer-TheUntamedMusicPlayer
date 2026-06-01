using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    private SelectorBar? _selectorBar;

    /// <summary>
    /// 页面索引, 0为歌曲, 1为专辑, 2为艺术家, 3为歌单
    /// </summary>
    public byte PageIndex
    {
        get;
        set
        {
            field = value;
            App.GetService<OnlineMusicLibrary>().PageIndex = value;
            _ = SavePageIndexAsync();
        }
    }

    [ObservableProperty]
    public partial byte MusicLibraryIndex { get; set; } = 0;

    partial void OnMusicLibraryIndexChanged(byte value)
    {
        App.GetService<OnlineMusicLibrary>().MusicLibraryIndex = value;
        LibraryNotOpenVisibility =
            MusicLibraryIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
        MainGridVisibility = MusicLibraryIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        _ = SaveMusicLibraryIndexAsync();
        // 音乐库索引改变时强制重新搜索
        if (!string.IsNullOrWhiteSpace(App.GetService<OnlineMusicLibrary>().SearchKeyWords))
        {
            _ = App.GetService<OnlineMusicLibrary>().ForceSearch();
        }
    }

    /// <summary>
    /// 乐库未开放提示可见性
    /// </summary>
    [ObservableProperty]
    public partial Visibility LibraryNotOpenVisibility { get; set; } = Visibility.Collapsed;

    /// <summary>
    /// 主界面可见性
    /// </summary>
    [ObservableProperty]
    public partial Visibility MainGridVisibility { get; set; } = Visibility.Collapsed;

    public HomeViewModel()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        PageIndex = await LoadPageIndex();
        MusicLibraryIndex = await LoadMusicLibraryIndex();
        LibraryNotOpenVisibility =
            MusicLibraryIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
        MainGridVisibility = MusicLibraryIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public async void SuggestBox_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args
    )
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            App.GetService<OnlineMusicLibrary>().SuggestKeyWords = sender.Text;
            await App.GetService<OnlineMusicLibrary>().UpdateSuggestResult();
        }
    }

    public async void SuggestBox_QuerySubmitted(
        AutoSuggestBox _,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        if (args.ChosenSuggestion is SuggestResult result)
        {
            var keyWords = result.Label;
            App.GetService<OnlineMusicLibrary>().ClearSuggestResult();
            var currentSelectedIndex = result.Icon switch
            {
                "\uE8D6" => 0,
                "\uE93C" => 1,
                "\uE77B" => 2,
                "\uE728" => 3,
                _ => 0,
            };
            App.GetService<OnlineMusicLibrary>().SearchKeyWords = keyWords;
            Navigate(currentSelectedIndex);
            // 搜索关键词改变时强制重新搜索
            await App.GetService<OnlineMusicLibrary>().ForceSearch();
        }
        else
        {
            App.GetService<OnlineMusicLibrary>().SearchKeyWords = args.QueryText;
            App.GetService<OnlineMusicLibrary>().ClearSuggestResult();
            // 搜索关键词改变时强制重新搜索
            await App.GetService<OnlineMusicLibrary>().ForceSearch();
        }
    }

    public void SelectorBar_Loaded(object sender, RoutedEventArgs _)
    {
        if (sender is SelectorBar selectorBar)
        {
            _selectorBar = selectorBar;
            var selectedItem = selectorBar.Items[PageIndex];
            selectorBar.SelectedItem = selectedItem;
        }
    }

    public void SelectorBar_SelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs _
    )
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);

        Navigate(currentSelectedIndex);
    }

    public void MusicLibraryIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is UXRadioButtons buttons)
        {
            var currentSelectedIndex = buttons.SelectedIndex;
            if (currentSelectedIndex < 0)
            {
                return;
            }
            _ = ChangeMusicLibraryIndexAsync(currentSelectedIndex);
        }
    }

    public Task ChangeMusicLibraryIndexAsync(int selectedIndex)
    {
        if (selectedIndex < 0)
        {
            return Task.CompletedTask;
        }

        MusicLibraryIndex = (byte)selectedIndex;
        return Task.CompletedTask;
    }

    public void Navigate(int currentSelectedIndex)
    {
        var page = currentSelectedIndex switch
        {
            0 => typeof(OnlineSongsPage),
            1 => typeof(OnlineAlbumsPage),
            2 => typeof(OnlineArtistsPage),
            3 => typeof(OnlinePlayListsPage),
            _ => typeof(OnlineSongsPage),
        };
        var slideNavigationTransitionEffect =
            currentSelectedIndex - PageIndex > 0
                ? SlideNavigationTransitionEffect.FromRight
                : SlideNavigationTransitionEffect.FromLeft;
        PageIndex = (byte)currentSelectedIndex;
        _selectorBar?.SelectedItem = _selectorBar.Items[currentSelectedIndex];

        _ = App.GetService<OnlineMusicLibrary>().Search();
        _navigationService.NavigateHome(
            page,
            null,
            new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect }
        );
    }

    public async Task<byte> LoadPageIndex()
    {
        return await _localSettingsService.ReadSettingAsync<byte>("HomePageIndex");
    }

    public async Task<byte> LoadMusicLibraryIndex()
    {
        return await _localSettingsService.ReadSettingAsync<byte>("HomeMusicLibraryIndex");
    }

    public async Task SavePageIndexAsync()
    {
        await _localSettingsService.SaveSettingAsync("HomePageIndex", PageIndex);
    }

    public async Task SaveMusicLibraryIndexAsync()
    {
        await _localSettingsService.SaveSettingAsync("HomeMusicLibraryIndex", MusicLibraryIndex);
    }
}

