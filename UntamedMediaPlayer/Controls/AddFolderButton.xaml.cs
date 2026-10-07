using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

public sealed partial class AddFolderButton : UserControl
{
    public bool SupportNarrowMode
    {
        get => (bool)GetValue(SupportNarrowModeProperty);
        set => SetValue(SupportNarrowModeProperty, value);
    }

    public static readonly DependencyProperty SupportNarrowModeProperty =
        DependencyProperty.Register(
            nameof(SupportNarrowMode),
            typeof(bool),
            typeof(AddFolderButton),
            new PropertyMetadata(false)
        );

    public bool IsAccessKeyEnabled
    {
        get => (bool)GetValue(IsAccessKeyEnabledProperty);
        set => SetValue(IsAccessKeyEnabledProperty, value);
    }

    public static readonly DependencyProperty IsAccessKeyEnabledProperty =
        DependencyProperty.Register(
            nameof(IsAccessKeyEnabled),
            typeof(bool),
            typeof(AddFolderButton),
            new PropertyMetadata(true)
        );

    public AddFolderButton()
    {
        InitializeComponent();
    }
}
