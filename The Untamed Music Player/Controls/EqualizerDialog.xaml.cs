using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Services;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EqualizerDialog : ContentDialog
{
    public EqualizerDialog()
    {
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }
}
