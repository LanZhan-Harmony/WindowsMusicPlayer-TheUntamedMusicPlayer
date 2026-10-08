using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace UntamedMediaPlayer.Controls;

public sealed partial class ResponsiveItemsRepeaterList : UserControl
{
    private readonly HashSet<UIElement> _realizedElements = new();

    public ResponsiveItemsRepeaterList()
    {
        InitializeComponent();
        Repeater.ElementPrepared += OnElementPrepared;
        Repeater.ElementClearing += OnElementClearing;
    }

    public ResponsiveListBreakpointCollection? Breakpoints
    {
        get => (ResponsiveListBreakpointCollection?)GetValue(BreakpointsProperty);
        set => SetValue(BreakpointsProperty, value);
    }

    public static readonly DependencyProperty BreakpointsProperty =
        DependencyProperty.Register(
            nameof(Breakpoints),
            typeof(ResponsiveListBreakpointCollection),
            typeof(ResponsiveItemsRepeaterList),
            new PropertyMetadata(null)
        );

    public bool CanDragItems
    {
        get => (bool)GetValue(CanDragItemsProperty);
        set => SetValue(CanDragItemsProperty, value);
    }

    public static readonly DependencyProperty CanDragItemsProperty =
        DependencyProperty.Register(
            nameof(CanDragItems),
            typeof(bool),
            typeof(ResponsiveItemsRepeaterList),
            new PropertyMetadata(false, OnCanDragItemsChanged)
        );

    public int ColumnSpan
    {
        get => (int)GetValue(ColumnSpanProperty);
        set => SetValue(ColumnSpanProperty, value);
    }

    public static readonly DependencyProperty ColumnSpanProperty =
        DependencyProperty.Register(
            nameof(ColumnSpan),
            typeof(int),
            typeof(ResponsiveItemsRepeaterList),
            new PropertyMetadata(1)
        );

    public int Row
    {
        get => (int)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public static readonly DependencyProperty RowProperty =
        DependencyProperty.Register(
            nameof(Row),
            typeof(int),
            typeof(ResponsiveItemsRepeaterList),
            new PropertyMetadata(0)
        );

    public double DefaultColumnSpacing
    {
        get => (double)GetValue(DefaultColumnSpacingProperty);
        set => SetValue(DefaultColumnSpacingProperty, value);
    }

    public static readonly DependencyProperty DefaultColumnSpacingProperty =
        DependencyProperty.Register(
            nameof(DefaultColumnSpacing),
            typeof(double),
            typeof(ResponsiveItemsRepeaterList),
            new PropertyMetadata(0d)
        );

    public bool StretchContent
    {
        get => (bool)GetValue(StretchContentProperty);
        set => SetValue(StretchContentProperty, value);
    }

    public static readonly DependencyProperty StretchContentProperty =
        DependencyProperty.Register(
            nameof(StretchContent),
            typeof(bool),
            typeof(ResponsiveItemsRepeaterList),
            new PropertyMetadata(true)
        );

    public MenuFlyout? ItemsMenuFlyout
    {
        get => (MenuFlyout?)GetValue(ItemsMenuFlyoutProperty);
        set => SetValue(ItemsMenuFlyoutProperty, value);
    }

    public static readonly DependencyProperty ItemsMenuFlyoutProperty =
        DependencyProperty.Register(
            nameof(ItemsMenuFlyout),
            typeof(MenuFlyout),
            typeof(ResponsiveItemsRepeaterList),
            new PropertyMetadata(null)
        );

    private static void OnCanDragItemsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is ResponsiveItemsRepeaterList list)
        {
            list.UpdateRealizedDragState((bool)args.NewValue);
        }
    }

    private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        _realizedElements.Add(args.Element);
        args.Element.CanDrag = CanDragItems;
    }

    private void OnElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        args.Element.CanDrag = false;
        _realizedElements.Remove(args.Element);
    }

    private void UpdateRealizedDragState(bool canDrag)
    {
        foreach (UIElement element in _realizedElements)
        {
            element.CanDrag = canDrag;
        }
    }
}
