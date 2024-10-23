using System.ComponentModel;
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
        ViewModel = App.GetService<MusicLibraryViewModel>();
        InitializeComponent();
        Data.MusicLibrary.PropertyChanged += MusicLibrary_PropertyChanged;
        UpdateContentFrame();
    }

    private void MusicLibrary_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Data.MusicLibrary.HasMusics))
        {
            UpdateContentFrame();
        }
    }

    private void UpdateContentFrame()
    {
        if (Data.MusicLibrary.HasMusics)
        {
            ContentFrame.Navigate(typeof(HaveMusicPage));
        }
        else
        {
            ContentFrame.Navigate(typeof(NoMusicPage));
        }
    }
}
