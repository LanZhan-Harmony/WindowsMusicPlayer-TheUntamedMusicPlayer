using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EqualizerDialog
    : ContentDialog,
        INotifyPropertyChanged,
        IRecipient<ThemeChangeMessage>
{
    private bool IsEqualizerOn
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsEqualizerOn));
            Settings.IsEqualizerOn = value;
        }
    } = Settings.IsEqualizerOn;

    private bool _isMoveNearby = Settings.IsMoveNearby;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EqualizerDialog()
    {
        StrongReferenceMessenger.Default.Register(this);
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    public void Receive(ThemeChangeMessage message)
    {
        RequestedTheme = message.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
    }

    private new void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        StrongReferenceMessenger.Default.Unregister<ThemeChangeMessage>(this);
    }
}
