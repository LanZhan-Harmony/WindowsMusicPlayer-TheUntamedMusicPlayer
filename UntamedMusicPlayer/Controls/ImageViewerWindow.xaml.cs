using System.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace UntamedMusicPlayer.Controls;

public sealed partial class ImageViewerWindow : Window
{
    private readonly BitmapImage _image;
    private readonly IDetailedSongInfoBase _info;

    public ImageViewerWindow(IDetailedSongInfoBase info)
    {
        InitializeComponent();
        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        AppWindow.SetTitleBarIcon(
            Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico")
        );
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        Title = "AppDisplayName".GetLocalized();
        _image = info.Cover!;
        _info = info;
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.Maximize();
        Activate();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        var extension = ".jpg";
        var picker = new FileSavePicker(App.MainWindow!.AppWindow.Id)
        {
            CommitButtonText = "保存图片",
            SuggestedFileName = _info.Title,
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            FileTypeChoices = { { "EditSongInfoDialog_CoverImage".GetLocalized(), [extension] } },
        };
        var file = await picker.PickSaveFileAsync();
        if (file is not null)
        {
            await File.WriteAllBytesAsync(file.Path, null);
        }
        (sender as Button)!.IsEnabled = true;
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var package = new DataPackage();
            if (_info.IsOnline)
            {
                package.SetBitmap(
                    RandomAccessStreamReference.CreateFromUri(
                        new Uri(((IDetailedOnlineSongInfo)_info).CoverPath!)
                    )
                );
            }
            else
            {
                var stream = RandomAccessStreamReference.CreateFromStream(null);
                package.SetBitmap(stream);
            }
            Clipboard.SetContent(package);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void OriginButton_Click(object sender, RoutedEventArgs e)
    {
        Scroll.ChangeView(null, null, 1);
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        Scroll.ChangeView(null, null, Scroll.ZoomFactor - 0.1F);
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        Scroll.ChangeView(null, null, Scroll.ZoomFactor + 0.1F);
    }

    private string GetZoomText(float zoom) => $"{(int)(zoom * 100)}%";
}
