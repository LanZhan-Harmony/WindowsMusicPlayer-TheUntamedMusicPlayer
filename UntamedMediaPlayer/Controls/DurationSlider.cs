using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class DurationSlider : Slider
{
    internal TimeSpan Duration { get; set; }
}
