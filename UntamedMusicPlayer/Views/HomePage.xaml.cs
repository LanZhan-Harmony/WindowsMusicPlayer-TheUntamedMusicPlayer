using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.ViewModels;

namespace UntamedMusicPlayer.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
        App.GetService<INavigationService>().InitializeHome(SelectFrame);
    }

    public Frame GetFrame()
    {
        return SelectFrame;
    }

    public Visibility ToVisibility(bool isVisible) =>
        isVisible ? Visibility.Visible : Visibility.Collapsed;

    private async void AutoSuggestBox_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args
    )
    {
        await ViewModel.UpdateSuggestTextAsync(
            sender.Text,
            args.Reason == AutoSuggestionBoxTextChangeReason.UserInput
        );
    }

    private void AutoSuggestBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is AutoSuggestBox autoSuggestBox)
        {
            autoSuggestBox.Text = ViewModel.OnlineLibrary.SearchKeyWords;
        }
    }

    private async void AutoSuggestBox_QuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        await ViewModel.SubmitSearchAsync(args.ChosenSuggestion as SuggestResult, args.QueryText);
        SetSelectedHomeSection(ViewModel.PageIndex);
    }

    private void MainSelectorBar_Loaded(object sender, RoutedEventArgs e)
    {
        SetSelectedHomeSection(ViewModel.PageIndex);
    }

    private void MainSelectorBar_SelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);

        ViewModel.Navigate(currentSelectedIndex);
    }

    private async void MusicLibraryIndex_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e
    )
    {
        if (sender is UXRadioButtons buttons && buttons.SelectedIndex >= 0)
        {
            await ViewModel.ChangeMusicLibraryIndexAsync(buttons.SelectedIndex);
        }
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.OnlineLibrary.RetryAsync();
    }

    private void SetSelectedHomeSection(int selectedIndex)
    {
        if (selectedIndex >= 0 && selectedIndex < MainSelectorBar.Items.Count)
        {
            MainSelectorBar.SelectedItem = MainSelectorBar.Items[selectedIndex];
        }
    }
}
