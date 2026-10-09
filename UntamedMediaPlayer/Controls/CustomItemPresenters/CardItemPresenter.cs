using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UntamedMediaPlayer.Controls.CustomItemPresenters;

internal sealed partial class CardItemPresenter : ListViewItemPresenter
{
    public ControlTemplate? Template
    {
        get => (ControlTemplate?)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    private static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
        nameof(Template),
        typeof(ControlTemplate),
        typeof(CardItemPresenter),
        new PropertyMetadata(null)
    );

    public bool IsWideImage
    {
        get => (bool)GetValue(IsWideImageProperty);
        set => SetValue(IsWideImageProperty, value);
    }

    private static readonly DependencyProperty IsWideImageProperty = DependencyProperty.Register(
        nameof(IsWideImage),
        typeof(bool),
        typeof(CardItemPresenter),
        new PropertyMetadata(false)
    );
}
