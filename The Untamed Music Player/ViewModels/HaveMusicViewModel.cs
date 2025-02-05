using The_Untamed_Music_Player.Contracts.Services;

namespace The_Untamed_Music_Player.ViewModels;
public class HaveMusicViewModel
{
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();

    public HaveMusicViewModel()
    {
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>("HaveMusicSelectionBarSelectedIndex");
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync("HaveMusicSelectionBarSelectedIndex", selectedIndex);
    }
}
