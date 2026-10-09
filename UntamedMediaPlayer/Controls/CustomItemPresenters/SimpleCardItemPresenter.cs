using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UntamedMediaPlayer.Controls.CustomItemPresenters;

internal sealed partial class SimpleCardItemPresenter : ListViewItemPresenter
{
    public ControlTemplate? Template
    {
        get => (ControlTemplate?)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    private static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
        nameof(Template),
        typeof(ControlTemplate),
        typeof(SimpleCardItemPresenter),
        new PropertyMetadata(null)
    );
}
