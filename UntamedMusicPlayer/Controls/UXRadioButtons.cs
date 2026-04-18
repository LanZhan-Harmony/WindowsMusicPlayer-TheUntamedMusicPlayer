using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Helpers.Animations;
using Windows.Foundation;

namespace UntamedMusicPlayer.Controls;

public partial class UXRadioButtons : RadioButtons
{
    public event TypedEventHandler<
        UXRadioButtons,
        ItemsRepeaterElementPreparedEventArgs
    >? ElementPrepared;

    private ItemsRepeater? _innerRepeater = null;

    private Vector2 _prevPoint = Vector2.Zero;

    public static double GetColumnSpacing(DependencyObject obj) =>
        (double)obj.GetValue(ColumnSpacingProperty);

    public static void SetColumnSpacing(DependencyObject obj, double value) =>
        obj.SetValue(ColumnSpacingProperty, value);

    public static readonly DependencyProperty ColumnSpacingProperty =
        DependencyProperty.RegisterAttached(
            "ColumnSpacing",
            typeof(double),
            typeof(UXRadioButtons),
            new PropertyMetadata(default(double))
        );

    public static double GetRowSpacing(DependencyObject obj) =>
        (double)obj.GetValue(RowSpacingProperty);

    public static void SetRowSpacing(DependencyObject obj, double value) =>
        obj.SetValue(RowSpacingProperty, value);

    public static readonly DependencyProperty RowSpacingProperty =
        DependencyProperty.RegisterAttached(
            "RowSpacing",
            typeof(double),
            typeof(UXRadioButtons),
            new PropertyMetadata(default(double))
        );

    public static DataTemplate GetLayoutTemplate(DependencyObject obj) =>
        (DataTemplate)obj.GetValue(LayoutTemplateProperty);

    public static void SetLayoutTemplate(DependencyObject obj, DataTemplate value) =>
        obj.SetValue(LayoutTemplateProperty, value);

    public static readonly DependencyProperty LayoutTemplateProperty =
        DependencyProperty.RegisterAttached(
            "LayoutTemplate",
            typeof(DataTemplate),
            typeof(UXRadioButtons),
            new PropertyMetadata(null, (d, e) => OnLayoutTemplateChanged(d, e))
        );

    public UXRadioButtons()
    {
        DefaultStyleKey = typeof(RadioButtons);
        Loaded += UXRadioButtons_Loaded;
    }

    private void UXRadioButtons_Loaded(object sender, RoutedEventArgs e)
    {
        if (SelectorVisualElement.GetElement(this) is not { } selector)
        {
            return;
        }

        selector.MoveTo(
            this.GetFirstLevelDescendantsOfType<RadioButton>()
                .FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value),
            this
        );
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _innerRepeater?.ElementPrepared -= OnElementPrepared;
        _innerRepeater = null;

        if (GetTemplateChild("InnerRepeater") is ItemsRepeater repeater)
        {
            repeater.ElementPrepared -= OnElementPrepared;
            repeater.ElementPrepared += OnElementPrepared;

            _innerRepeater = repeater;
        }

        AddHandler(
            PointerPressedEvent,
            new PointerEventHandler(UXRadioButtons_PointerPressed),
            true
        );

        AddHandler(
            PointerReleasedEvent,
            new PointerEventHandler(UXRadioButtons_PointerReleased),
            true
        );
    }

    private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        ElementPrepared?.Invoke(this, args);

        if (args.Element is { } element)
        {
            element.PointerEntered -= Element_PointerEntered;
            element.PointerEntered += Element_PointerEntered;

            element.PointerExited -= Element_PointerExited;
            element.PointerExited += Element_PointerExited;

            if (element is RadioButton rb)
            {
                rb.Tapped -= Rb_Tapped;
                rb.Tapped += Rb_Tapped;
            }
        }
    }

    private static void OnLayoutTemplateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is ItemsRepeater repeater)
        {
            if (e.NewValue is DataTemplate t && t.LoadContent() is Layout l)
            {
                repeater.Layout = l;
            }
            else
            {
                repeater.Layout = null;
            }
        }
    }

    // TODO : Handle PointerOver if selection changes by ScrollWheel

    //------------------------------------------------------
    //
    // Drag Handling
    //
    //------------------------------------------------------

    private void UXRadioButtons_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        PointerMoved -= UXRadioButtons_PointerMoved;
        ReleasePointerCaptures();

        if (SelectorVisualElement.GetElement(this) is not { } selector)
        {
            return;
        }

        // Renable auto-animate
        selector.EndMove();

        // see if our selection has changed
        HandleIntersect(selector);
    }

    private void UXRadioButtons_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (SelectorVisualElement.GetElement(this) is not { } selector)
        {
            return;
        }

        // disable auto-animate
        selector.StartMove();

        _prevPoint = e.GetCurrentPoint(this).Position.ToVector2();
        CapturePointer(e.Pointer);
        PointerMoved -= UXRadioButtons_PointerMoved;
        PointerMoved += UXRadioButtons_PointerMoved;
    }

    private void UXRadioButtons_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (SelectorVisualElement.GetElement(this) is not { } selector)
        {
            return;
        }

        // get current point
        var point = e.GetCurrentPoint(this).Position.ToVector2();

        // move container by delta
        selector.Move(point - _prevPoint);

        // store current point for next delta calculation
        _prevPoint = point;
    }

    private void HandleIntersect(SelectorVisualElement selector)
    {
        var bounds = selector.GetBounds();

        var match = Rect.Empty;
        var i = 0;
        var index = 0;
        var radios = this.GetFirstLevelDescendantsOfType<RadioButton>().ToArray();
        var orientation = selector.Orientation;
        foreach (var rb in radios)
        {
            var intersect = GetIntersection(rb.GetBoundingRect(this), bounds);
            if (intersect != Rect.Empty)
            {
                if (
                    match == Rect.Empty
                    || (
                        orientation == Orientation.Horizontal
                            ? match.Width < intersect.Width
                            : match.Height < intersect.Height
                    )
                )
                {
                    match = intersect;
                    index = i;
                }
            }
            i++;
        }

        if (match != Rect.Empty)
        {
            if (SelectedIndex != index)
            {
                SelectedIndex = index;
            }
            else
            {
                selector.RadioMoveTo(this, index);
            }
        }
    }

    /// <summary>
    /// Calculates the intersection region of two rectangles
    /// </summary>
    /// <param name="rect1">The first rectangle</param>
    /// <param name="rect2">The second rectangle</param>
    /// <returns>A rectangle representing the intersection region, or an empty rectangle if there is no intersection</returns>
    public static Rect GetIntersection(Rect rect1, Rect rect2)
    {
        var left = Math.Max(rect1.X, rect2.X);
        var top = Math.Max(rect1.Y, rect2.Y);
        var right = Math.Min(rect1.X + rect1.Width, rect2.X + rect2.Width);
        var bottom = Math.Min(rect1.Y + rect1.Height, rect2.Y + rect2.Height);

        if (right >= left && bottom >= top)
        {
            return new Rect(left, top, right - left, bottom - top);
        }
        return Rect.Empty;
    }

    //------------------------------------------------------
    //
    // PointerOver animation handling
    //
    //------------------------------------------------------

    private void Rb_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_innerRepeater is not null)
        {
            SetPointerOver();
        }
    }

    private void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (
            _innerRepeater is not null
            && _innerRepeater.GetElementIndex(sender as UIElement) == SelectedIndex
        )
        {
            SetPointerOver();
        }
    }

    private void Element_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (
            _innerRepeater is not null
            && _innerRepeater.GetElementIndex(sender as UIElement) == SelectedIndex
        )
        {
            SetPointerExited();
        }
    }

    private void SetPointerOver()
    {
        if (SelectorVisualElement.GetElement(this) is not { } selector)
        {
            return;
        }

        selector.SetState(SelectorInteractionState.PointerOver);
    }

    private void SetPointerExited()
    {
        if (SelectorVisualElement.GetElement(this) is not { } selector)
        {
            return;
        }

        selector.SetState(SelectorInteractionState.None);
    }
}
