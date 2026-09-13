using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace UntamedMediaPlayer.Helpers;

internal sealed partial class ImageIconSourceExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the <see cref="Source"/> representing the image to display.
    /// </summary>
    public ImageSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the foreground <see cref="Brush"/> for the icon.
    /// </summary>
    public Brush? Foreground { get; set; }

    protected override object ProvideValue()
    {
        ArgumentNullException.ThrowIfNull(Source);
        ImageIconSource imageIconSource = new() { ImageSource = Source, Foreground = Foreground };
        return imageIconSource;
    }
}
