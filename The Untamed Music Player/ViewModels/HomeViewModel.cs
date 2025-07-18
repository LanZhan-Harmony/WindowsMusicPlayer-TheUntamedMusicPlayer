using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class HomeViewModel : ObservableRecipient
{
    /// <summary>
    /// 页面索引, 0为歌曲, 1为专辑, 2为艺术家, 3为歌单
    /// </summary>
    public byte PageIndex
    {
        get;
        set
        {
            field = value;
            Data.OnlineMusicLibrary.PageIndex = value;
        }
    }

    [ObservableProperty]
    public partial byte MusicLibraryIndex { get; set; }

    partial void OnMusicLibraryIndexChanged(byte value)
    {
        Data.OnlineMusicLibrary.MusicLibraryIndex = value;
        SaveMusicLibraryIndex();
    }

    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    public HomeViewModel()
    {
        Initialize();
    }

    private async void Initialize()
    {
        PageIndex = await LoadPageIndex();
        MusicLibraryIndex = await LoadMusicLibraryIndex();
    }

    public async void SuggestBox_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args
    )
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            Data.OnlineMusicLibrary.SuggestKeyWords = sender.Text;
            await Data.OnlineMusicLibrary.UpdateSuggestResult();
        }
    }

    public async void SuggestBox_QuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        if (args.ChosenSuggestion is SuggestResult result)
        {
            var keyWords = result.Label;
            Data.OnlineMusicLibrary.ClearSuggestResult();
            var currentSelectedIndex = result.Icon switch
            {
                "\uE8D6" => 0,
                "\uE93C" => 1,
                "\uE77B" => 2,
                "\uE728" => 3,
                _ => 0,
            };
            Data.OnlineMusicLibrary.SearchKeyWords = keyWords;
            Navigate(currentSelectedIndex);
            await Data.OnlineMusicLibrary.Search();
        }
        else
        {
            Data.OnlineMusicLibrary.SearchKeyWords = args.QueryText;
            Data.OnlineMusicLibrary.ClearSuggestResult();
            await Data.OnlineMusicLibrary.Search();
        }
    }

    public void SelectorBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is SelectorBar selectorBar)
        {
            var selectedItem = selectorBar.Items[PageIndex];
            selectorBar.SelectedItem = selectedItem;
        }
    }

    public void SelectorBar_SelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);

        Navigate(currentSelectedIndex);
    }

    public void Navigate(int currentSelectedIndex, bool isFirstLoaded = false)
    {
        if (!isFirstLoaded && PageIndex == currentSelectedIndex)
        {
            return;
        }
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
        _ = Data.OnlineMusicLibrary.Search();
        Data.HomePage.GetFrame()
            .Navigate(
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

    public async void SavePageIndex()
    {
        await _localSettingsService.SaveSettingAsync("HomePageIndex", PageIndex);
    }

    public async void SaveMusicLibraryIndex()
    {
        await _localSettingsService.SaveSettingAsync("HomeMusicLibraryIndex", MusicLibraryIndex);
    }
}
