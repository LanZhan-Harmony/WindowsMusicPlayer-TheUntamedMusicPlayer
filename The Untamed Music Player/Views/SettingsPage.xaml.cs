using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Helpers;
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
        var folderName = "";
        StorageFolder? folder = null;
        if (sender is FrameworkElement { DataContext: StorageFolder storageFolder })
        {
            folderName = storageFolder.DisplayName;
            folder = storageFolder;
        }
        var titleTextBlock = new TextBlock
        {
            Text = "Settings_Dialog1Title".GetLocalized(),
            FontWeight = Microsoft.UI.Text.FontWeights.Normal,
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = titleTextBlock,
            Content =
                "Settings_Dialog1Content1".GetLocalized()
                + folderName
                + "Settings_Dialog1Content2".GetLocalized(),
            PrimaryButtonText = "Settings_Dialog1Primary".GetLocalized(),
            CloseButtonText = "Settings_Dialog1Close".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.RemoveMusicFolder(folder!);
        }
    }

    public async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "Settings_Dialog2Title".GetLocalized(),
            FontWeight = Microsoft.UI.Text.FontWeights.Normal,
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = titleTextBlock,
            Content = "Settings_Dialog2Content".GetLocalized(),
            PrimaryButtonText = "Settings_Dialog2Primary".GetLocalized(),
            CloseButtonText = "Settings_Dialog2Close".GetLocalized(),
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:search"));
        }
    }
}
