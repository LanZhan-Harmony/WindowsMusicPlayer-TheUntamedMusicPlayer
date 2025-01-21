using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class MusicLibraryViewModel : ObservableRecipient
{
    private bool _hasNavigated = false;

    private bool _isProgressRingActive = true;
    public bool IsProgressRingActive
    {
        get => _isProgressRingActive;
        set
        {
            _isProgressRingActive = value;
            OnPropertyChanged(nameof(IsProgressRingActive));
        }
    }

    public MusicLibraryViewModel()
    {
        InitializeLibraryAsync();
    }

    private async void InitializeLibraryAsync()
    {
        Data.MusicLibrary.PropertyChanged += MusicLibrary_PropertyChanged;
        if (!Data.hasMusicLibraryLoaded)
        {
            await Data.MusicLibrary.LoadLibrary();
            Data.hasMusicLibraryLoaded = true;
        }
        IsProgressRingActive = false;
        if (!_hasNavigated)
        {
            UpdateContentFrame();
        }
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
        _hasNavigated = true;
        if (Data.MusicLibrary.HasMusics)
        {
            Data.MusicLibraryPage?.GetContentFrame().Navigate(typeof(HaveMusicPage));
        }
        else
        {
            Data.MusicLibraryPage?.GetContentFrame().Navigate(typeof(NoMusicPage));
        }
    }
}
