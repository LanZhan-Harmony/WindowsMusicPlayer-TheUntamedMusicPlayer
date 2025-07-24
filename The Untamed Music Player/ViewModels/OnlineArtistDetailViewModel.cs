using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public partial class OnlineArtistDetailViewModel : ObservableRecipient
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    public IBriefOnlineArtistInfo BriefArtist { get; set; } = Data.SelectedOnlineArtist!;

    [ObservableProperty]
    public partial IDetailedOnlineArtistInfo Artist { get; set; } =
        IDetailedOnlineArtistInfo.CreateFastOnlineArtistInfoAsync(Data.SelectedOnlineArtist!);

    public OnlineArtistDetailViewModel()
    {
        LoadArtistAsync();
    }

    private async void LoadArtistAsync()
    {
        Artist = await IDetailedOnlineArtistInfo.CreateDetailedOnlineArtistInfoAsync(BriefArtist);
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e) { }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "LocalArtistDetailSelectionBarSelectedIndex"
        );
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "LocalArtistDetailSelectionBarSelectedIndex",
            selectedIndex
        );
    }
}
