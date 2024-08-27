using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using The_Untamed_Music_Player.ViewModels;
using Windows.Storage;
using Windows.System;

namespace The_Untamed_Music_Player.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get; set;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    private async void RemoveMusicFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var folderName = "";
        if (sender is Button button && button.DataContext is StorageFolder folder)
        {
            folderName = folder.DisplayName;
        }
        var resourceLoader = new ResourceLoader();
        var titleTextBlock = new TextBlock
        {
            Text = resourceLoader.GetString("Settings_Dialog1Title"),
            FontWeight = Microsoft.UI.Text.FontWeights.Normal
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = titleTextBlock,
            Content = resourceLoader.GetString("Settings_Dialog1Content1") + folderName + resourceLoader.GetString("Settings_Dialog1Content2"),
            PrimaryButtonText = resourceLoader.GetString("Settings_Dialog1Primary"),
            CloseButtonText = resourceLoader.GetString("Settings_Dialog1Close"),
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.RemoveMusicFolderButtonClick(sender, e);
        }
    }

    public async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        var resourceLoader = new ResourceLoader();
        var titleTextBlock = new TextBlock
        {
            Text = resourceLoader.GetString("Settings_Dialog2Title"),
            FontWeight = Microsoft.UI.Text.FontWeights.Normal
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = titleTextBlock,
            Content = resourceLoader.GetString("Settings_Dialog2Content"),
            PrimaryButtonText = resourceLoader.GetString("Settings_Dialog2Primary"),
            CloseButtonText = resourceLoader.GetString("Settings_Dialog2Close"),
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:search"));
        }
    }

}
