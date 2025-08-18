using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class MusicLibraryPage : Page
{
    private readonly SettingsViewModel _settingsViewModel = App.GetService<SettingsViewModel>();
    public MusicLibraryViewModel ViewModel { get; }

    private int SelectionBarSelectedIndex
    {
        get;
        set
        {
            field = value;
            ViewModel.SaveSelectionBarSelectedIndex(value);
        }
    } = 0;

    public MusicLibraryPage()
    {
        ViewModel = App.GetService<MusicLibraryViewModel>();
        InitializeAsync();
        InitializeComponent();
    }

    private async void InitializeAsync()
    {
        SelectionBarSelectedIndex = await ViewModel.LoadSelectionBarSelectedIndex();
    }

    private void SelectorBar_SelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        var pageType = currentSelectedIndex switch
        {
            0 => typeof(LocalSongsPage),
            1 => typeof(LocalAlbumsPage),
            _ => typeof(LocalArtistsPage),
        };
        var slideNavigationTransitionEffect =
            currentSelectedIndex - SelectionBarSelectedIndex > 0
                ? SlideNavigationTransitionEffect.FromRight
                : SlideNavigationTransitionEffect.FromLeft;

        SelectFrame.Navigate(
            pageType,
            null,
            new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect }
        );

        SelectionBarSelectedIndex = currentSelectedIndex;
    }

    private void SelectorBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is SelectorBar selectorBar)
        {
            var selectedItem = selectorBar.Items[SelectionBarSelectedIndex];
            selectorBar.SelectedItem = selectedItem;
        }
    }
}
