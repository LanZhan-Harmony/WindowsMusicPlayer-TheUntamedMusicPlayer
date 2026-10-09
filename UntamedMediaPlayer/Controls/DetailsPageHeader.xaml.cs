using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class DetailsPageHeader : UserControl
{
    public string? ImageType
    {
        get => (string?)GetValue(ImageTypeProperty);
        set => SetValue(ImageTypeProperty, value);
    }

    private static readonly DependencyProperty ImageTypeProperty = DependencyProperty.Register(
        nameof(ImageType),
        typeof(string),
        typeof(DetailsPageHeader),
        new PropertyMetadata(null, OnImageTypeChanged)
    );

    public UIElement? TitleContentBar
    {
        get => (UIElement?)GetValue(TitleContentBarProperty);
        set => SetValue(TitleContentBarProperty, value);
    }

    private static readonly DependencyProperty TitleContentBarProperty =
        DependencyProperty.Register(
            nameof(TitleContentBar),
            typeof(UIElement),
            typeof(DetailsPageHeader),
            new PropertyMetadata(null, OnTitleContentBarChanged)
        );

    public DetailsPageHeader()
    {
        InitializeComponent();
        ApplyImageType();
        ApplyTitleContentBar();
    }

    private static void OnImageTypeChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is DetailsPageHeader header)
        {
            header.ApplyImageType();
        }
    }

    private static void OnTitleContentBarChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is DetailsPageHeader header)
        {
            header.ApplyTitleContentBar();
        }
    }

    private void ApplyImageType()
    {
        image?.ImageType = ImageType;
    }

    private void ApplyTitleContentBar()
    {
        pageHeaderTitleContentBarContainer?.Child = TitleContentBar;
    }
}
