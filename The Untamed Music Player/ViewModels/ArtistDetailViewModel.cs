using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;
public class ArtistDetailViewModel
{
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();

    public ArtistInfo Artist { get; set; } = Data.SelectedArtist!;

    public List<BriefAlbumInfo> AlbumList
    {
        get; set;
    }

    public ArtistDetailViewModel()
    {
        AlbumList = Data.MusicLibrary.GetAlbumsByArtist(Artist);
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>("ArtistDetailSelectionBarSelectedIndex");
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync("ArtistDetailSelectionBarSelectedIndex", selectedIndex);
    }
}
