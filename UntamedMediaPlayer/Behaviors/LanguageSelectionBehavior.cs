using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using UntamedMediaPlayer.Core.ViewModels;

namespace UntamedMediaPlayer.Behaviors;

internal sealed class LanguageSelectionBehavior : Behavior<AppBarButton>
{
    internal MediaPlayerViewModel ViewModel { get; set; } = null!;
}
