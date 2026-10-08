using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

/// <summary>Selects the track-specific result group template when the item represents tracks.</summary>
public sealed partial class SearchResultsGroupTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }

    public DataTemplate? TrackTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        IsTrackGroup(item) ? TrackTemplate ?? DefaultTemplate : DefaultTemplate;

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);

    private static bool IsTrackGroup(object item) =>
        item.GetType().Name.Contains("Track", StringComparison.OrdinalIgnoreCase);
}
