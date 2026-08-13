using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Helpers.Animations;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;
using Windows.Storage;
using Windows.System;

namespace UntamedMusicPlayer.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; set; }
    public MusicLibrary MusicLibrary { get; } = App.GetService<MusicLibrary>();
    public string AppDisplayName { get; } = "AppDisplayName".GetLocalized();
    private bool _isInitialized = false;

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    public Visibility ToVisibility(bool isVisible) =>
        isVisible ? Visibility.Visible : Visibility.Collapsed;

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
                await ViewModel.RemoveMusicFolderAsync(folder);
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
            ViewModel.ResetSoftwareButtonCommand.Execute(null);
        }
        (sender as Button)!.IsEnabled = true;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }
        const int delayMs = 10;
        const int fromOffsetY = 80;
        const int durationMs = 300;
        const int staggerMs = 50;

        var targets = GetEntranceTargets(ContentPanel);
        ContentScroller.ChangeView(null, 0, null, true);
        CompositionFactory.PlayEntrance(
            targets,
            delayMs,
            fromOffsetY,
            durationMs: durationMs,
            staggerMs: staggerMs
        );

        ContentPanel.Opacity = 1;

        var totalAnimationMs = delayMs + durationMs;
        if (targets.Count > 1)
        {
            totalAnimationMs += (targets.Count - 1) * staggerMs;
        }
        await Task.Delay(totalAnimationMs);
        SetRepositionTransitions();
        _isInitialized = true;
    }

    private static List<UIElement> GetEntranceTargets(Panel panel)
    {
        var targets = new List<UIElement>();
        foreach (var child in panel.Children)
        {
            if (child is StackPanel childPanel && childPanel.Children.Count > 0)
            {
                foreach (var nested in childPanel.Children)
                {
                    targets.Add(nested);
                }
            }
            else
            {
                targets.Add(child);
            }
        }
        return targets;
    }

    private void SetRepositionTransitions()
    {
        ApplyRepositionTransition(ContentPanel);
        foreach (var child in ContentPanel.Children)
        {
            if (child is StackPanel childPanel)
            {
                ApplyRepositionTransition(childPanel);
            }
        }
    }

    private static void ApplyRepositionTransition(StackPanel panel)
    {
        panel.ChildrenTransitions = [new RepositionThemeTransition { IsStaggeringEnabled = false }];
    }

    private async void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await ViewModel.UpdateSelectedMaterialAsync();
    }

    private void FontFamilyComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        var selectedFontName = ViewModel.SelectedFontFamily.Source;
        var index = ViewModel.FontFamilies.FindIndex(f => f.Name == selectedFontName);
        if (index >= 0)
        {
            comboBox.SelectedIndex = index;
        }
    }

    private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FontFamilyInfo selectedFont)
        {
            ViewModel.SelectFontFamily(selectedFont);
        }
    }

    private void LyricPageCurrentFontSizeComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        var selectedItem = ViewModel.LyricPageCurrentFontSizes.FirstOrDefault(f =>
            f == ViewModel.LyricPageCurrentFontSize
        );
        if (selectedItem != 0.0)
        {
            comboBox.SelectedItem = selectedItem;
        }
        else
        {
            comboBox.Text = $"{ViewModel.LyricPageCurrentFontSize}";
        }
    }

    private void LyricPageCurrentFontSizeComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e
    )
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is double fontSize)
        {
            ViewModel.SelectLyricPageCurrentFontSize(fontSize);
        }
    }

    private void LyricPageCurrentFontSizeComboBox_TextSubmitted(
        ComboBox sender,
        ComboBoxTextSubmittedEventArgs args
    )
    {
        if (!ViewModel.TrySubmitLyricPageCurrentFontSize(args.Text))
        {
            sender.Text = $"{ViewModel.LyricPageCurrentFontSize}";
        }
    }

    private void LyricPageNotCurrentFontSizeComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        var selectedItem = ViewModel.LyricPageNotCurrentFontSizes.FirstOrDefault(f =>
            f == ViewModel.LyricPageNotCurrentFontSize
        );
        if (selectedItem != 0.0)
        {
            comboBox.SelectedItem = selectedItem;
        }
        else
        {
            comboBox.Text = $"{ViewModel.LyricPageNotCurrentFontSize}";
        }
    }

    private void LyricPageNotCurrentFontSizeComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e
    )
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is double fontSize)
        {
            ViewModel.SelectLyricPageNotCurrentFontSize(fontSize);
        }
    }

    private void LyricPageNotCurrentFontSizeComboBox_TextSubmitted(
        ComboBox sender,
        ComboBoxTextSubmittedEventArgs args
    )
    {
        if (!ViewModel.TrySubmitLyricPageNotCurrentFontSize(args.Text))
        {
            sender.Text = $"{ViewModel.LyricPageNotCurrentFontSize}";
        }
    }

    private void FontWeightComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        var selectedItem = ViewModel.FontWeights.FirstOrDefault(weight =>
            weight.FontWeight.Weight == ViewModel.LyricPageFontWeight.Weight
        );
        if (selectedItem is not null)
        {
            comboBox.SelectedItem = selectedItem;
        }
    }

    private void FontWeightComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FontWeightInfo selectedWeight)
        {
            ViewModel.SelectFontWeight(selectedWeight);
        }
    }
}
