using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;
public partial class MusicLibraryViewModel : ObservableRecipient
{
    /// <summary>
    /// 是否已经导航到了页面
    /// </summary>
    private bool _hasNavigated = false;

    /// <summary>
    /// 是否显示加载进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = true;

    public MusicLibraryViewModel()
    {
        _ = InitializeLibraryAsync();
    }

    private async Task InitializeLibraryAsync()
    {
        Data.MusicLibrary.PropertyChanged += MusicLibrary_PropertyChanged;
        if (!Data.HasMusicLibraryLoaded)
        {
            await Task.Run(Data.MusicLibrary.LoadLibraryAsync);
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
        Data.MusicLibraryPage?.GetContentFrame().Navigate(Data.MusicLibrary.HasMusics ? typeof(HaveMusicPage) : typeof(NoMusicPage));
    }
}
