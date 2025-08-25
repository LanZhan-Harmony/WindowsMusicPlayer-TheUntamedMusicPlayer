using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    public bool IsFirstLoaded { get; set; } = true;

    public string NavigatePage { get; set; } = null!;

    public string CurrentPage { get; set; } = null!;

    public PlaylistInfo? PrevPlaylistInfo { get; set; }

    [ObservableProperty]
    public partial object SelectedItem { get; set; } = null!;

    public ShellViewModel()
    {
        Data.ShellViewModel = this;
        LoadAsync();
    }

    public void NavigationFrame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back)
        {
            PrevPlaylistInfo = null;
        }
        CurrentPage = e.SourcePageType.Name;
        var navView = Data.ShellPage!.GetNavigationView();
        if (
            CurrentPage
            is nameof(HomePage)
                or nameof(OnlineAlbumDetailPage)
                or nameof(OnlineArtistDetailPage)
                or nameof(OnlinePlayListDetailPage)
        )
        {
            SelectedItem = navView.MenuItems[0];
        }
        else if (
            CurrentPage
            is nameof(MusicLibraryPage)
                or nameof(LocalAlbumDetailPage)
                or nameof(LocalArtistDetailPage)
        )
        {
            SelectedItem = navView.MenuItems[1];
        }
        else if (CurrentPage is nameof(PlayQueuePage))
        {
            SelectedItem = navView.MenuItems[3];
        }
        else if (CurrentPage is nameof(PlayListsPage))
        {
            SelectedItem = navView.MenuItems[4];
        }
        else if (CurrentPage is nameof(PlayListDetailPage))
        {
            var playlistsNavItem = navView.MenuItems[4] as NavigationViewItem;
            var playlistSubItem = playlistsNavItem!
                .MenuItems.AsValueEnumerable()
                .Cast<NavigationViewItem>()
                .FirstOrDefault(item =>
                    item.DataContext is PlaylistInfo playlist && playlist == PrevPlaylistInfo
                );
            if (playlistSubItem is not null)
            {
                SelectedItem = playlistSubItem;
            }
            else
            {
                SelectedItem = playlistsNavItem;
            }
        }
        else if (CurrentPage is nameof(SettingsPage))
        {
            SelectedItem = navView.FooterMenuItems[0];
        }
        SaveCurrentPageAsync();
    }

    private async void LoadAsync()
    {
        CurrentPage =
            await _localSettingsService.ReadSettingAsync<string>("CurrentPage") ?? nameof(HomePage);
    }

    private async void SaveCurrentPageAsync()
    {
        await _localSettingsService.SaveSettingAsync("CurrentPage", CurrentPage);
    }
}
