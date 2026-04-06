using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.ViewModels;
using Windows.Storage;
using Windows.System;

namespace UntamedMusicPlayer.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; set; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    private async void RemoveMusicFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string folder })
        {
            var folderName = (await StorageFolder.GetFolderFromPathAsync(folder)).DisplayName;
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Style = Application.Current.Resources["NormalContentDialogStyle"] as Style,
                RequestedTheme = ThemeSelectorService.IsDarkTheme
                    ? ElementTheme.Dark
                    : ElementTheme.Light,
                Title = new TextBlock { Text = "Settings_RemoveFolderDialogTitle".GetLocalized() },
                Content = "Settings_RemoveFolderDialogContent".GetLocalizedWithReplace(
                    "{title}",
                    folderName
                ),
                PrimaryButtonText = "Settings_RemoveFolderDialogPrimary".GetLocalized(),
                CloseButtonText = "Settings_RemoveFolderDialogClose".GetLocalized(),
                DefaultButton = ContentDialogButton.Primary,
            };
            dialog.EnableLightDismiss();

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.RemoveMusicFolder(folder);
            }
        }
    }

    public async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["NormalContentDialogStyle"] as Style,
            RequestedTheme = ThemeSelectorService.IsDarkTheme
                ? ElementTheme.Dark
                : ElementTheme.Light,
            Title = new TextBlock { Text = "Settings_OpenSettingDialogTitle".GetLocalized() },
            Content = "Settings_OpenSettingDialogContent".GetLocalized(),
            PrimaryButtonText = "Settings_OpenSettingDialogPrimary".GetLocalized(),
            CloseButtonText = "Settings_OpenSettingDialogClose".GetLocalized(),
            DefaultButton = ContentDialogButton.Close,
        };
        dialog.EnableLightDismiss();

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:search"));
        }
    }

    private async void ImportIndividualPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        var dialog = new ImportPlaylistDialog { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
        (sender as Button)!.IsEnabled = true;
    }

    private async void ResetSoftwareButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["NormalContentDialogStyle"] as Style,
            RequestedTheme = ThemeSelectorService.IsDarkTheme
                ? ElementTheme.Dark
                : ElementTheme.Light,
            Title = new TextBlock { Text = "Settings_ResetSoftwareDialogTitle".GetLocalized() },
            Content = "Settings_ResetSoftwareDialogContent".GetLocalized(),
            PrimaryButtonText = "Settings_ResetSoftwareDialogPrimary".GetLocalized(),
            CloseButtonText = "Settings_ResetSoftwareDialogClose".GetLocalized(),
            DefaultButton = ContentDialogButton.Close,
        };
        dialog.EnableLightDismiss();

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ResetSoftwareButton_Click();
        }
        (sender as Button)!.IsEnabled = true;
    }
}
