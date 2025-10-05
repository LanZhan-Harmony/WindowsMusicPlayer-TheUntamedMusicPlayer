using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;

namespace UntamedMusicPlayer.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
        Data.HomePage = this;
    }

    public Frame GetFrame()
    {
        return SelectFrame;
    }
}
