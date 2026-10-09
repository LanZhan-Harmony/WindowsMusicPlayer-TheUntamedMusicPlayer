using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls.CustomDataTemplateSelectors;

/// <summary>
/// Selects the track-specific result group template when the item represents tracks.
/// </summary>
internal sealed partial class SearchResultsGroupTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }

    public DataTemplate? TrackTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return IsTrackGroup(item) ? TrackTemplate ?? DefaultTemplate : DefaultTemplate;
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }

    private static bool IsTrackGroup(object item)
    {
        return item.GetType().Name.Contains("Track", StringComparison.OrdinalIgnoreCase);
    }
}
