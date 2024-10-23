using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;


public sealed partial class NoMusicPage : Page
{
    private readonly SettingsViewModel SettingsViewModel;
    public NoMusicViewModel ViewModel
    {
        get;
    }
    public NoMusicPage()
    {
        ViewModel = App.GetService<NoMusicViewModel>();
        InitializeComponent();
        SettingsViewModel = App.GetService<SettingsViewModel>();
    }
}
