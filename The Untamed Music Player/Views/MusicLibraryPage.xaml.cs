using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class MusicLibraryPage : Page
{
    public MusicLibraryViewModel ViewModel
    {
        get;
    }

    public MusicLibraryPage()
    {
        InitializeComponent();
        Data.MusicLibraryPage = this;
        ViewModel = App.GetService<MusicLibraryViewModel>();
    }

    public Frame GetContentFrame()
    {
        return ContentFrame;
    }
}
