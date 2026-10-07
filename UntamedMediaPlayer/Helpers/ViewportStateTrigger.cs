using Microsoft.UI.Xaml;

namespace UntamedMediaPlayer.Helpers;

public class ViewportStateTrigger : StateTriggerBase
{
    public bool IsEnabled { get; set; } = true;
    public string State { get; set; } = string.Empty;
    public InteractionMode PrimaryInteractionMode { get; set; } = InteractionMode.Mouse;
}

public enum ViewportState
{
    Narrow, // 0
    Normal, // 641
    Large, // 850
    ExtraLarge,
    FullScreen,
}

public enum InteractionMode
{
    Mouse,
    Touch,
}
