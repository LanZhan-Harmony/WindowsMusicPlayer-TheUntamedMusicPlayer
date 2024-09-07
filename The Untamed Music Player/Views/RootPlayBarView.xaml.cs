using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class RootPlayBarView : Page
{
    public RootPlayBarViewModel ViewModel
    {
        get;
    }

    public RootPlayBarView()
    {
        RootPlayBarViewModel.RootPlayBarView = this;
        InitializeComponent();
        ViewModel = App.GetService<RootPlayBarViewModel>();
        MusicPlayer.PlayBarUI = this;
        DataContext = ViewModel;
    }

    public Slider GetProgressSlider()
    {
        return ProgressSlider;
    }

    private void SpeedListView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Data.MusicPlayer.SpeedListView_Loaded(sender, e);
    }

    private void SpeedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Data.MusicPlayer.SpeedListView_SelectionChanged(sender, e);
    }
}
