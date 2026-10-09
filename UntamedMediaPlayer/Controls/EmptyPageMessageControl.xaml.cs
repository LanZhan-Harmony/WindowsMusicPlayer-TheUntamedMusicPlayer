using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class EmptyPageMessageControl : UserControl
{
    public string CommandIcon
    {
        get => (string)GetValue(CommandIconProperty);
        set => SetValue(CommandIconProperty, value);
    }

    private static readonly DependencyProperty CommandIconProperty = DependencyProperty.Register(
        nameof(CommandIcon),
        typeof(string),
        typeof(EmptyPageMessageControl),
        new PropertyMetadata(null, OnCommandIconChanged)
    );

    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    private static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
        nameof(ImageSource),
        typeof(ImageSource),
        typeof(EmptyPageMessageControl),
        new PropertyMetadata(null, OnImageSourceChanged)
    );

    public bool IsHomePage
    {
        get => (bool)GetValue(IsHomePageProperty);
        set => SetValue(IsHomePageProperty, value);
    }

    private static readonly DependencyProperty IsHomePageProperty = DependencyProperty.Register(
        nameof(IsHomePage),
        typeof(bool),
        typeof(EmptyPageMessageControl),
        new PropertyMetadata(false, OnIsHomePageChanged)
    );

    public string? CommandAccessVirtualKey
    {
        get => (string?)GetValue(CommandAccessVirtualKeyProperty);
        set => SetValue(CommandAccessVirtualKeyProperty, value);
    }

    private static readonly DependencyProperty CommandAccessVirtualKeyProperty =
        DependencyProperty.Register(
            nameof(CommandAccessVirtualKey),
            typeof(string),
            typeof(EmptyPageMessageControl),
            new PropertyMetadata(null, OnCommandAccessVirtualKeyChanged)
        );

    public FlyoutBase? CommandButtonFlyout
    {
        get => (FlyoutBase?)GetValue(CommandButtonFlyoutProperty);
        set => SetValue(CommandButtonFlyoutProperty, value);
    }

    private static readonly DependencyProperty CommandButtonFlyoutProperty =
        DependencyProperty.Register(
            nameof(CommandButtonFlyout),
            typeof(FlyoutBase),
            typeof(EmptyPageMessageControl),
            new PropertyMetadata(null, OnCommandButtonFlyoutChanged)
        );

    public EmptyPageMessageControl()
    {
        InitializeComponent();
        ApplyCommandIcon();
        ApplyImageSource();
        ApplyCommandAccessVirtualKey();
        ApplyCommandButtonFlyout();
        ApplyHomePageState();
    }

    private static void OnCommandIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is EmptyPageMessageControl control)
        {
            control.ApplyCommandIcon();
        }
    }

    private static void OnImageSourceChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is EmptyPageMessageControl control)
        {
            control.ApplyImageSource();
        }
    }

    private static void OnCommandAccessVirtualKeyChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is EmptyPageMessageControl control)
        {
            control.ApplyCommandAccessVirtualKey();
        }
    }

    private static void OnCommandButtonFlyoutChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is EmptyPageMessageControl control)
        {
            control.ApplyCommandButtonFlyout();
        }
    }

    private static void OnIsHomePageChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is EmptyPageMessageControl control)
        {
            control.ApplyHomePageState();
        }
    }

    private void ApplyCommandIcon()
    {
        commandIcon?.Glyph = CommandIcon;
    }

    private void ApplyImageSource()
    {
        image?.Source = ImageSource;
    }

    private void ApplyCommandAccessVirtualKey()
    {
        button?.AccessKey = CommandAccessVirtualKey;
    }

    private void ApplyCommandButtonFlyout()
    {
        button?.Flyout = CommandButtonFlyout;
    }

    private void ApplyHomePageState()
    {
        VisualStateManager.GoToState(this, IsHomePage ? "HomePageState" : "Default", false);
    }
}
