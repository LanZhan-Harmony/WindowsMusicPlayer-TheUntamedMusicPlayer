using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

namespace UntamedMusicPlayer.Helpers;

public sealed class FadeImageBehavior : Behavior<Image>
{
    private CompositionScopedBatch? _currentTransitionBatch;
    private Visual? _associatedVisual;
    private Visual? _overlayVisual;
    private Image? _tempOverlayImage;

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(ImageSource),
        typeof(FadeImageBehavior),
        new PropertyMetadata(null, OnSourceChanged)
    );

    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration),
        typeof(Duration),
        typeof(FadeImageBehavior),
        new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(450)))
    );

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Duration Duration
    {
        get => (Duration)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null && Source is not null)
        {
            AssociatedObject.Source = Source;
        }
    }

    protected override void OnDetaching()
    {
        StopAndCleanup();
        base.OnDetaching();
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FadeImageBehavior behavior)
        {
            return;
        }

        behavior.TransitionToNewSource(e.NewValue as ImageSource);
    }

    private void TransitionToNewSource(ImageSource? newSource)
    {
        if (AssociatedObject is null || AssociatedObject.Source == newSource)
        {
            return;
        }

        if (VisualTreeHelper.GetParent(AssociatedObject) is not Panel parent)
        {
            AssociatedObject.Source = newSource;
            return;
        }

        StopAndCleanup();

        var oldSource = AssociatedObject.Source;
        if (oldSource is null)
        {
            AssociatedObject.Source = newSource;

            StartSingleFadeInTransition();
            return;
        }

        _tempOverlayImage = new Image
        {
            Source = oldSource,
            Stretch = AssociatedObject.Stretch,
            HorizontalAlignment = AssociatedObject.HorizontalAlignment,
            VerticalAlignment = AssociatedObject.VerticalAlignment,
            Width = AssociatedObject.ActualWidth,
            Height = AssociatedObject.ActualHeight,
            Margin = AssociatedObject.Margin,
            Opacity = 1,
            IsHitTestVisible = false,
        };

        if (parent is Grid)
        {
            Grid.SetRow(_tempOverlayImage, Grid.GetRow(AssociatedObject));
            Grid.SetColumn(_tempOverlayImage, Grid.GetColumn(AssociatedObject));
            Grid.SetRowSpan(_tempOverlayImage, Grid.GetRowSpan(AssociatedObject));
            Grid.SetColumnSpan(_tempOverlayImage, Grid.GetColumnSpan(AssociatedObject));
        }

        var currentZIndex = Canvas.GetZIndex(AssociatedObject);
        Canvas.SetZIndex(_tempOverlayImage, currentZIndex + 1);
        parent.Children.Add(_tempOverlayImage);

        AssociatedObject.Source = newSource;

        StartCrossFadeTransition();
    }

    private void StartSingleFadeInTransition()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        _associatedVisual = ElementCompositionPreview.GetElementVisual(AssociatedObject);
        var compositor = _associatedVisual.Compositor;
        var duration = GetAnimationDuration();
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.215f, 0.61f),
            new Vector2(0.355f, 1f)
        );

        var fadeIn = compositor.CreateScalarKeyFrameAnimation();
        fadeIn.InsertKeyFrame(0f, 0f);
        fadeIn.InsertKeyFrame(1f, 1f, easing);
        fadeIn.Duration = duration;

        _currentTransitionBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        _currentTransitionBatch.Completed += OnCompositionTransitionCompleted;
        _associatedVisual.Opacity = 0f;
        _associatedVisual.StartAnimation(nameof(Visual.Opacity), fadeIn);
        _currentTransitionBatch.End();
    }

    private void StartCrossFadeTransition()
    {
        if (AssociatedObject is null || _tempOverlayImage is null)
        {
            return;
        }

        _associatedVisual = ElementCompositionPreview.GetElementVisual(AssociatedObject);
        _overlayVisual = ElementCompositionPreview.GetElementVisual(_tempOverlayImage);

        var compositor = _associatedVisual.Compositor;
        var duration = GetAnimationDuration();
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.215f, 0.61f),
            new Vector2(0.355f, 1f)
        );

        var oldFadeOut = compositor.CreateScalarKeyFrameAnimation();
        oldFadeOut.InsertKeyFrame(0f, 1f);
        oldFadeOut.InsertKeyFrame(1f, 0f, easing);
        oldFadeOut.Duration = duration;

        var newFadeIn = compositor.CreateScalarKeyFrameAnimation();
        newFadeIn.InsertKeyFrame(0f, 0f);
        newFadeIn.InsertKeyFrame(1f, 1f, easing);
        newFadeIn.Duration = duration;

        _currentTransitionBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        _currentTransitionBatch.Completed += OnCompositionTransitionCompleted;

        _associatedVisual.Opacity = 0f;
        _overlayVisual.Opacity = 1f;
        _overlayVisual.StartAnimation(nameof(Visual.Opacity), oldFadeOut);
        _associatedVisual.StartAnimation(nameof(Visual.Opacity), newFadeIn);

        _currentTransitionBatch.End();
    }

    private TimeSpan GetAnimationDuration()
    {
        return Duration.HasTimeSpan ? Duration.TimeSpan : TimeSpan.FromMilliseconds(450);
    }

    private void OnCompositionTransitionCompleted(
        object? sender,
        CompositionBatchCompletedEventArgs args
    )
    {
        if (_associatedVisual is not null)
        {
            _associatedVisual.StopAnimation(nameof(Visual.Opacity));
            _associatedVisual.Opacity = 1f;
        }
        StopAndCleanup();
    }

    private void StopAndCleanup()
    {
        _currentTransitionBatch?.Completed -= OnCompositionTransitionCompleted;
        _currentTransitionBatch = null;
        _associatedVisual?.StopAnimation(nameof(Visual.Opacity));
        _associatedVisual = null;
        _overlayVisual?.StopAnimation(nameof(Visual.Opacity));
        _overlayVisual = null;

        if (_tempOverlayImage is not null)
        {
            if (VisualTreeHelper.GetParent(_tempOverlayImage) is Panel parent)
            {
                parent.Children.Remove(_tempOverlayImage);
            }

            _tempOverlayImage.Source = null;
            _tempOverlayImage = null;
        }
    }
}
