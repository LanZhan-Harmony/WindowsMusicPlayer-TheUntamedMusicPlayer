using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Xaml.Interactivity;

namespace UntamedMusicPlayer.Helpers;

public sealed class FadeImageBehavior : Behavior<Image>
{
    private Storyboard? _currentTransitionStoryboard;
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
        if (AssociatedObject is null)
        {
            return;
        }

        if (ReferenceEquals(AssociatedObject.Source, newSource))
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
            AssociatedObject.Opacity = 0;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = Duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };

            _currentTransitionStoryboard = new Storyboard();
            _currentTransitionStoryboard.Children.Add(fadeIn);
            Storyboard.SetTarget(fadeIn, AssociatedObject);
            Storyboard.SetTargetProperty(fadeIn, nameof(UIElement.Opacity));
            _currentTransitionStoryboard.Completed += OnTransitionCompleted;
            _currentTransitionStoryboard.Begin();
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
        AssociatedObject.Opacity = 0;

        var oldFadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = Duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        var newFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = Duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        _currentTransitionStoryboard = new Storyboard();
        _currentTransitionStoryboard.Children.Add(oldFadeOut);
        _currentTransitionStoryboard.Children.Add(newFadeIn);

        Storyboard.SetTarget(oldFadeOut, _tempOverlayImage);
        Storyboard.SetTargetProperty(oldFadeOut, nameof(UIElement.Opacity));

        Storyboard.SetTarget(newFadeIn, AssociatedObject);
        Storyboard.SetTargetProperty(newFadeIn, nameof(UIElement.Opacity));

        _currentTransitionStoryboard.Completed += OnTransitionCompleted;
        _currentTransitionStoryboard.Begin();
    }

    private void OnTransitionCompleted(object? sender, object e)
    {
        StopAndCleanup();
        AssociatedObject?.Opacity = 1;
    }

    private void StopAndCleanup()
    {
        if (_currentTransitionStoryboard is not null)
        {
            _currentTransitionStoryboard.Completed -= OnTransitionCompleted;
            _currentTransitionStoryboard.Stop();
            _currentTransitionStoryboard = null;
        }

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
