using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PlayListsPage : Page
{
    public PlayListsViewModel ViewModel { get; }

    public PlayListsPage()
    {
        ViewModel = App.GetService<PlayListsViewModel>();
        InitializeComponent();
    }

    private void PlaylistGridView_Loaded(object sender, RoutedEventArgs e) { }

    private void PlaylistGridView_ItemClick(object sender, ItemClickEventArgs e) { }
}
