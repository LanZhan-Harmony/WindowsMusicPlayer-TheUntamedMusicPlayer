using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class AddFolderButton : UserControl
{
    public bool SupportNarrowMode
    {
        get => (bool)GetValue(SupportNarrowModeProperty);
        set => SetValue(SupportNarrowModeProperty, value);
    }

    private static readonly DependencyProperty SupportNarrowModeProperty =
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

    private static readonly DependencyProperty IsAccessKeyEnabledProperty =
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
