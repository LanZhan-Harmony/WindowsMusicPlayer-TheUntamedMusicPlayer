using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace UntamedMusicPlayer.Helpers;

public static class DialogHelper
{
    extension(ContentDialog dialog)
    {
        public void EnableLightDismiss()
        {
            void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
            {
                var root = VisualTreeHelper.GetChild(sender, 0) as FrameworkElement;
                var smokeLayer = root?.FindName("SmokeLayerBackground") as FrameworkElement;
                void OnPressed(object s, PointerRoutedEventArgs a)
                {
                    smokeLayer.PointerPressed -= OnPressed;
                    sender.Hide();
                }
                smokeLayer?.PointerPressed += OnPressed;
            }

            void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
            {
                sender.Opened -= OnOpened;
                sender.Closed -= OnClosed;
            }

            dialog.Opened += OnOpened;
            dialog.Closed += OnClosed;
        }
    }
}
