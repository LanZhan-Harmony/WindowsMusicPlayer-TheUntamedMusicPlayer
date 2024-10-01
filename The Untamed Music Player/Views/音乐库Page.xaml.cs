using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class 音乐库Page : Page
{
    public 音乐库ViewModel ViewModel
    {
        get;
    }

    public 音乐库Page()
    {
        ViewModel = App.GetService<音乐库ViewModel>();
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
            ContentFrame.Navigate(typeof(有音乐Page));
        }
        else
        {
            ContentFrame.Navigate(typeof(无音乐Page));
        }
    }
}
