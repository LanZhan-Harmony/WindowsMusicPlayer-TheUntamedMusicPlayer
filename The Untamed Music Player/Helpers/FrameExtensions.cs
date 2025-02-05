using Microsoft.UI.Xaml.Controls;

namespace The_Untamed_Music_Player.Helpers;
public static class FrameExtensions
{
    public static object? GetPageViewModel(this Frame frame) => frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}
