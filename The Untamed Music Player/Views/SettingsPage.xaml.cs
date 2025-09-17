using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Services;
using The_Untamed_Music_Player.ViewModels;
using Windows.Storage;
using Windows.System;

namespace The_Untamed_Music_Player.Views;

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
            var titleTextBlock = new TextBlock
            {
                Text = "Settings_RemoveFolderDialogTitle".GetLocalized(),
                FontWeight = FontWeights.Normal,
            };
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                RequestedTheme = ThemeSelectorService.IsDarkTheme
                    ? ElementTheme.Dark
                    : ElementTheme.Light,
                Title = titleTextBlock,
                Content = "Settings_RemoveFolderDialogContent".GetLocalizedWithReplace(
                    "{title}",
                    folderName
                ),
                PrimaryButtonText = "Settings_RemoveFolderDialogPrimary".GetLocalized(),
                CloseButtonText = "Settings_RemoveFolderDialogClose".GetLocalized(),
                DefaultButton = ContentDialogButton.Primary,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.RemoveMusicFolder(folder);
            }
        }
    }

    public async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "Settings_OpenSettingDialogTitle".GetLocalized(),
            FontWeight = FontWeights.Normal,
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            RequestedTheme = ThemeSelectorService.IsDarkTheme
                ? ElementTheme.Dark
                : ElementTheme.Light,
            Title = titleTextBlock,
            Content = "Settings_OpenSettingDialogContent".GetLocalized(),
            PrimaryButtonText = "Settings_OpenSettingDialogPrimary".GetLocalized(),
            CloseButtonText = "Settings_OpenSettingDialogClose".GetLocalized(),
            DefaultButton = ContentDialogButton.Close,
        };

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
        var titleTextBlock = new TextBlock
        {
            Text = "Settings_ResetSoftwareDialogTitle".GetLocalized(),
            FontWeight = FontWeights.Normal,
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            RequestedTheme = ThemeSelectorService.IsDarkTheme
                ? ElementTheme.Dark
                : ElementTheme.Light,
            Title = titleTextBlock,
            Content = "Settings_ResetSoftwareDialogContent".GetLocalized(),
            PrimaryButtonText = "Settings_ResetSoftwareDialogPrimary".GetLocalized(),
            CloseButtonText = "Settings_ResetSoftwareDialogClose".GetLocalized(),
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ResetSoftwareButton_Click();
        }
        (sender as Button)!.IsEnabled = true;
    }
}
