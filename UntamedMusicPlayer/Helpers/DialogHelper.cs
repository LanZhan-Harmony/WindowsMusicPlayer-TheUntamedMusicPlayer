using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace UntamedMusicPlayer.Helpers;

public static class DialogHelper
{
    extension(ContentDialog dialog)
    {
        public void EnableLightDismiss()
        {
            dialog.Opened += (s, e) =>
            {
                var root = VisualTreeHelper.GetChild(dialog, 0) as FrameworkElement;
                var smokeLayer = root?.FindName("SmokeLayerBackground") as FrameworkElement;
                smokeLayer?.PointerPressed += (sender, args) => dialog.Hide();
            };
        }
    }
}
