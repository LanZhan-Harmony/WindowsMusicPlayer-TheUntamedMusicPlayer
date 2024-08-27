using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.ViewModels;


namespace The_Untamed_Music_Player.Views;

public sealed partial class 有音乐Page : Page
{
    private readonly SettingsViewModel SettingsViewModel;
    private int previousSelectedIndex = 0;
    public 有音乐ViewModel ViewModel
    {
        get;
    }
    public 有音乐Page()
    {
        ViewModel = App.GetService<有音乐ViewModel>();
        InitializeComponent();
        SettingsViewModel = App.GetService<SettingsViewModel>();
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        var pageType = currentSelectedIndex switch
        {
            0 => typeof(歌曲Page),
            1 => typeof(专辑Page),
            _ => typeof(艺术家Page),
        };
        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        SelectFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;
    }
}
