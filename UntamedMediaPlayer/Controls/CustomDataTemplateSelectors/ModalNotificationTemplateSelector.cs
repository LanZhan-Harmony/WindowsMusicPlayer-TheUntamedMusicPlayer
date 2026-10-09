using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls.CustomDataTemplateSelectors;

internal partial class ModalNotificationTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }
    public DataTemplate? CDRipSettingsTemplate { get; set; }
    public DataTemplate? EditAlbumInfoTemplate { get; set; }
    public DataTemplate? EditMediaInfoTemplate { get; set; }
    public DataTemplate? EqualizerControlTemplate { get; set; }
    public DataTemplate? MediaExtensionAcquisitionUserConsentTemplate { get; set; }
    public DataTemplate? MediaPropertiesTemplate { get; set; }
    public DataTemplate? PlaylistNameDialogTemplate { get; set; }
    public DataTemplate? UrlPickerTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return DefaultTemplate;
    }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return DefaultTemplate;
    }
}
