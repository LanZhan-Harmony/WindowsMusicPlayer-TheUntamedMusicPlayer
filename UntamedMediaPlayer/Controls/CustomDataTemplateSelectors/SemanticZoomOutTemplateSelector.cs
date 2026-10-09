using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls.CustomDataTemplateSelectors;

internal sealed partial class SemanticZoomOutTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GroupTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return GroupTemplate;
    }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return GroupTemplate;
    }
}
