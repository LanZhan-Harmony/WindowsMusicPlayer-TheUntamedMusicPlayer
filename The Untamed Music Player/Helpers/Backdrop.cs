using Microsoft.UI;
using Windows.UI;
using Windows.UI.Composition;

namespace The_Untamed_Music_Player.Helpers;

public partial class ColorAnimatedBackdrop : CompositionBrushBackdrop
{
    protected override CompositionBrush CreateBrush(Compositor compositor)
    {
        var brush = compositor.CreateColorBrush(Color.FromArgb(255, 255, 0, 0));
        var animation = compositor.CreateColorKeyFrameAnimation();
        var easing = compositor.CreateLinearEasingFunction();
        animation.InsertKeyFrame(0, Colors.Red, easing);
        animation.InsertKeyFrame(.333f, Colors.Green, easing);
        animation.InsertKeyFrame(.667f, Colors.Blue, easing);
        animation.InsertKeyFrame(1, Colors.Red, easing);
        animation.InterpolationColorSpace = CompositionColorSpace.Hsl;
        animation.Duration = TimeSpan.FromSeconds(15);
        animation.IterationBehavior = AnimationIterationBehavior.Forever;
        brush.StartAnimation("Color", animation);
        return brush;
    }
}

public partial class BlurredBackdrop : CompositionBrushBackdrop
{
    protected override CompositionBrush CreateBrush(Compositor compositor) =>
        compositor.CreateHostBackdropBrush();
}
