using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UntamedMediaPlayer.Controls.CustomItemPresenters;

public sealed partial class CardItemPresenter : ListViewItemPresenter
{
    public ControlTemplate? Template
    {
        get => (ControlTemplate?)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
        nameof(Template), typeof(ControlTemplate), typeof(CardItemPresenter), new PropertyMetadata(null));

    public bool IsWideImage
    {
        get => (bool)GetValue(IsWideImageProperty);
        set => SetValue(IsWideImageProperty, value);
    }

    public static readonly DependencyProperty IsWideImageProperty = DependencyProperty.Register(
        nameof(IsWideImage), typeof(bool), typeof(CardItemPresenter), new PropertyMetadata(false));
}
