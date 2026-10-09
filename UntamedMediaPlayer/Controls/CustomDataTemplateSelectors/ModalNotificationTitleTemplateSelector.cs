using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls.CustomDataTemplateSelectors;

internal sealed partial class ModalNotificationTitleTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }
    public DataTemplate? EditAlbumInfoTitleTemplate { get; set; }
    public DataTemplate? EqualizerTitleTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return DefaultTemplate;
    }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return DefaultTemplate;
    }
}
