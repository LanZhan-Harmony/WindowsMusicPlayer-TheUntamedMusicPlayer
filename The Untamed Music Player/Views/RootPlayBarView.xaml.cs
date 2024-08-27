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
    public Border GetCoverBorder()
    {
        return CoverBorder;
    }
}
