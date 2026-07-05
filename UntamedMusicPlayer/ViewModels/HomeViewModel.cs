using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();
    private readonly OnlineMusicLibrary _onlineMusicLibrary = App.GetService<OnlineMusicLibrary>();

    /// <summary>
    /// 页面索引, 0为歌曲, 1为专辑, 2为艺术家, 3为歌单
    /// </summary>
    public byte PageIndex
    {
        get;
        set
        {
            field = value;
            _onlineMusicLibrary.PageIndex = value;
            _ = SavePageIndexAsync();
        }
    }

    [ObservableProperty]
    public partial byte MusicLibraryIndex { get; set; } = 0;

    partial void OnMusicLibraryIndexChanged(byte value)
    {
        _onlineMusicLibrary.MusicLibraryIndex = value;
        UpdateLibraryVisibilityState();
        _ = SaveMusicLibraryIndexAsync();
        // 音乐库索引改变时强制重新搜索
        if (!string.IsNullOrWhiteSpace(_onlineMusicLibrary.SearchKeyWords))
        {
            _ = _onlineMusicLibrary.ForceSearch();
        }
    }

    /// <summary>
    /// 乐库未开放提示可见性
    /// </summary>
    [ObservableProperty]
    public partial bool IsLibraryNotOpenVisible { get; set; }

    /// <summary>
    /// 主界面可见性
    /// </summary>
    [ObservableProperty]
    public partial bool IsMainGridVisible { get; set; }

    public HomeViewModel()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        PageIndex = await LoadPageIndex();
        MusicLibraryIndex = await LoadMusicLibraryIndex();
        UpdateLibraryVisibilityState();
    }

    public async Task UpdateSuggestTextAsync(string text, bool isUserInput)
    {
        if (isUserInput)
        {
            _onlineMusicLibrary.SuggestKeyWords = text;
            await _onlineMusicLibrary.UpdateSuggestResult();
        }
    }

    public async Task SubmitSearchAsync(SuggestResult? chosenSuggestion, string queryText)
    {
        if (chosenSuggestion is SuggestResult result)
        {
            var keyWords = result.Label;
            _onlineMusicLibrary.ClearSuggestResult();
            var currentSelectedIndex = result.Icon switch
            {
                "\uE8D6" => 0,
                "\uE93C" => 1,
                "\uE77B" => 2,
                "\uE728" => 3,
                _ => 0,
            };
            _onlineMusicLibrary.SearchKeyWords = keyWords;
            Navigate(currentSelectedIndex);
            // 搜索关键词改变时强制重新搜索
            await _onlineMusicLibrary.ForceSearch();
        }
        else
        {
            _onlineMusicLibrary.SearchKeyWords = queryText;
            _onlineMusicLibrary.ClearSuggestResult();
            // 搜索关键词改变时强制重新搜索
            await _onlineMusicLibrary.ForceSearch();
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
        if (currentSelectedIndex < 0)
        {
            return;
        }

        var page = currentSelectedIndex switch
        {
            0 => HomeNavigationPage.OnlineSongs,
            1 => HomeNavigationPage.OnlineAlbums,
            2 => HomeNavigationPage.OnlineArtists,
            3 => HomeNavigationPage.OnlinePlayLists,
            _ => HomeNavigationPage.OnlineSongs,
        };
        var direction =
            currentSelectedIndex - PageIndex > 0
                ? HomeNavigationDirection.Forward
                : HomeNavigationDirection.Backward;
        PageIndex = (byte)currentSelectedIndex;

        _ = _onlineMusicLibrary.Search();
        _navigationService.NavigateHome(page, null, direction);
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

    private void UpdateLibraryVisibilityState()
    {
        IsLibraryNotOpenVisible = MusicLibraryIndex != 0;
        IsMainGridVisible = MusicLibraryIndex == 0;
    }
}

