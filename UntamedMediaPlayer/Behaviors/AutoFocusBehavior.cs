using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Moves keyboard focus to the associated element when it is loaded.
/// </summary>
internal sealed class AutoFocusBehavior : BehaviorBase<Control>
{
    protected override void OnAssociatedObjectLoaded()
    {
        AssociatedObject.Focus(FocusState.Programmatic);
    }
}
