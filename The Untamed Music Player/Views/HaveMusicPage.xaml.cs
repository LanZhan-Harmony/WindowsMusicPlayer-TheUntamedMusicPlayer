using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.ViewModels;


namespace The_Untamed_Music_Player.Views;

public sealed partial class HaveMusicPage : Page
{
    private readonly SettingsViewModel SettingsViewModel;
    private int previousSelectedIndex = 0;
    public HaveMusicViewModel ViewModel
    {
        get;
    }
    public HaveMusicPage()
    {
        ViewModel = App.GetService<HaveMusicViewModel>();
        InitializeComponent();
        SettingsViewModel = App.GetService<SettingsViewModel>();
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        var pageType = currentSelectedIndex switch
        {
            0 => typeof(LocalSongsPage),
            1 => typeof(LocalAlbumsPage),
            _ => typeof(LocalArtistsPage),
        };
        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        SelectFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;
    }
}
