using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;


public sealed partial class 无音乐Page : Page
{
    private readonly SettingsViewModel SettingsViewModel;
    public 无音乐ViewModel ViewModel
    {
        get;
    }
    public 无音乐Page()
    {
        ViewModel = App.GetService<无音乐ViewModel>();
        InitializeComponent();
        SettingsViewModel = App.GetService<SettingsViewModel>();
    }
}
