using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using ZLogger;

namespace UntamedMusicPlayer.Controls;

public sealed partial class ImageViewerWindow : Window, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<ImageViewerWindow>();

    private readonly Guid _windowId;
    private readonly BitmapImage _image;
    private readonly IDetailedSongInfoBase _info;

    private bool _closed = false;
    private bool _isDisposed = false;

    public ImageViewerWindow(Guid windowId, IDetailedSongInfoBase info)
    {
        InitializeComponent();
        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        AppWindow.SetTitleBarIcon(
            Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico")
        );
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        Title = "ImageViewerTitle".GetLocalized();
        ExtendsContentIntoTitleBar = true;
        var theme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        ((FrameworkElement)Content).RequestedTheme = theme;
        TitleBarHelper.UpdateTitleBar(
            AppWindow.TitleBar,
            ((FrameworkElement)Content).RequestedTheme
        );

        _windowId = windowId;
        _image = info.Cover!;
        _info = info;

        Activate();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        var picker = new FileSavePicker(AppWindow.Id)
        {
            SuggestedFileName = _info.Title,
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            FileTypeChoices =
            {
                { "EditSongInfoDialog_CoverImage".GetLocalized(), [ExtractExtension()] },
            },
        };
        var file = await picker.PickSaveFileAsync();
        if (file is not null)
        {
            byte[] data;
            try
            {
                if (_info.IsOnline)
                {
                    using var client = new HttpClient();
                    data = await client.GetByteArrayAsync(
                        ((IDetailedOnlineSongInfo)_info).CoverPath!
                    );
                }
                else
                {
                    data = ((DetailedLocalSongInfo)_info).CoverBuffer!;
                }
                if (data.Length > 0)
                {
                    await File.WriteAllBytesAsync(file.Path, data);
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"{_info.Title}封面保存失败");
            }
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
                var buffer = ((DetailedLocalSongInfo)_info).CoverBuffer!;
                var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(buffer.AsBuffer());
                stream.Seek(0);
                var streamRef = RandomAccessStreamReference.CreateFromStream(stream);
                package.SetBitmap(streamRef);
                package.OperationCompleted += (s, _) => stream.Dispose();
            }
            Clipboard.SetContent(package);
            Clipboard.Flush();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"复制封面失败");
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

    private string ExtractExtension()
    {
        if (_info.IsOnline)
        {
            var coverPath = ((IDetailedOnlineSongInfo)_info).CoverPath;
            if (string.IsNullOrEmpty(coverPath))
            {
                return ".jpeg";
            }
            var uri = new Uri(coverPath);
            var extension = Path.GetExtension(uri.AbsolutePath).ToLower();
            if (string.IsNullOrEmpty(extension))
            {
                return ".jpeg";
            }
            return extension;
        }

        var buffer = ((DetailedLocalSongInfo)_info).CoverBuffer;
        if (buffer is not { Length: >= 2 })
        {
            return ".jpeg";
        }

        // JPEG (jpg, jpeg, jpe, jfif): FF D8
        if (buffer[0] == 0xFF && buffer[1] == 0xD8)
        {
            return ".jpeg";
        }

        // BMP (bmp, dip): 42 4D
        if (buffer[0] == 0x42 && buffer[1] == 0x4D)
        {
            return ".bmp";
        }

        if (buffer.Length >= 4)
        {
            // PNG: 89 50 4E 47
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            {
                return ".png";
            }
            // GIF: 47 49 46 38 ('GIF8')
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38)
            {
                return ".gif";
            }
            // TIFF (Little Endian): 49 49 2A 00, (Big Endian): 4D 4D 00 2A
            if (
                (buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2A && buffer[3] == 0x00)
                || (
                    buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && buffer[3] == 0x2A
                )
            )
            {
                return ".tiff";
            }
        }

        return ".jpeg";
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        _closed = true;
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (!_closed)
        {
            Close();
        }

        Data.ImageViewerWindows?.Remove(_windowId);
    }
}
