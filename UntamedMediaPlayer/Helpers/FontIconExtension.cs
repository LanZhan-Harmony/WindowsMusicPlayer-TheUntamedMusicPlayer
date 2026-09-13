using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace UntamedMediaPlayer.Helpers;

[MarkupExtensionReturnType(ReturnType = typeof(FontIcon))]
internal sealed partial class FontIconExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the <see cref="string"/> value representing the icon to display.
    /// </summary>
    public string? Glyph { get; set; }

    /// <summary>
    /// Gets or sets the font family to use to display the icon. If <see langword="null"/>, "Untamed Fluent Icons" will be used.
    /// </summary>
    public FontFamily? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the size of the icon to display.
    /// </summary>
    public double FontSize { get; set; }

    /// <summary>
    /// Gets or sets the thickness of the icon glyph.
    /// </summary>
    public FontWeight FontWeight { get; set; } = FontWeights.Normal;

    /// <summary>
    /// Gets or sets the font style for the icon glyph.
    /// </summary>
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;

    /// <summary>
    /// Gets or sets the foreground <see cref="Brush"/> for the icon.
    /// </summary>
    public Brush? Foreground { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether automatic text enlargement, to reflect the system text size setting, is enabled.
    /// </summary>
    public bool IsTextScaleFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the icon is mirrored when the flow direction is right to left.
    /// </summary>
    public bool MirroredWhenRightToLeft { get; set; }

    /// <inheritdoc/>
    protected override object ProvideValue()
    {
        ArgumentNullException.ThrowIfNull(Glyph);

        FontIcon fontIcon = new()
        {
            Glyph = Glyph,
            FontFamily =
                FontFamily ?? Application.Current.Resources["UntamedFontFamily"] as FontFamily,
            FontWeight = FontWeight,
            FontStyle = FontStyle,
            IsTextScaleFactorEnabled = IsTextScaleFactorEnabled,
            MirroredWhenRightToLeft = MirroredWhenRightToLeft,
        };

        if (FontSize > 0)
        {
            fontIcon.FontSize = FontSize;
        }

        if (Foreground != null)
        {
            fontIcon.Foreground = Foreground;
        }

        return fontIcon;
    }
}
