using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;
using Windows.Foundation.Collections;
using Windows.UI;

namespace The_Untamed_Music_Player.Helpers;

internal class AlternatingListViewBehavior : Behavior<ListViewBase>
{
    public static readonly DependencyProperty AlternateBackgroundProperty =
        DependencyProperty.Register(
            nameof(AlternateBackground),
            typeof(Brush),
            typeof(AlternatingListViewBehavior),
            new PropertyMetadata(default(Brush))
        );

    public static readonly DependencyProperty AlternateBorderThicknessProperty =
        DependencyProperty.Register(
            nameof(AlternateBorderThickness),
            typeof(Thickness),
            typeof(AlternatingListViewBehavior),
            new PropertyMetadata(default(Thickness))
        );

    public static readonly DependencyProperty AlternateBorderBrushProperty =
        DependencyProperty.Register(
            nameof(AlternateBorderBrush),
            typeof(Brush),
            typeof(AlternatingListViewBehavior),
            new PropertyMetadata(default(Brush?))
        );

    public Brush? AlternateBorderBrush
    {
        get => (Brush?)GetValue(AlternateBorderBrushProperty);
        set => SetValue(AlternateBorderBrushProperty, value);
    }

    public Thickness AlternateBorderThickness
    {
        get => (Thickness)GetValue(AlternateBorderThicknessProperty);
        set => SetValue(AlternateBorderThicknessProperty, value);
    }

    public Brush? AlternateBackground
    {
        get => (Brush?)GetValue(AlternateBackgroundProperty);
        set => SetValue(AlternateBackgroundProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.ActualThemeChanged += OnActualThemeChanged;
        AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
        if (AssociatedObject.Items is not null)
        {
            AssociatedObject.Items.VectorChanged += ItemsOnVectorChanged;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.ActualThemeChanged -= OnActualThemeChanged;
        AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        if (AssociatedObject.Items is not null)
        {
            AssociatedObject.Items.VectorChanged -= ItemsOnVectorChanged;
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (AssociatedObject.Items is null)
        {
            return;
        }

        for (var i = 0; i < AssociatedObject.Items.Count; i++)
        {
            if (AssociatedObject.ContainerFromIndex(i) is SelectorItem itemContainer)
            {
                UpdateAlternateLayout(itemContainer, i);
            }
        }
    }

    private void ItemsOnVectorChanged(
        IObservableVector<object> sender,
        IVectorChangedEventArgs args
    )
    {
        // If the index is at the end we can ignore
        if (args.Index == sender.Count - 1)
        {
            return;
        }

        // Only need to handle Inserted and Removed because we'll handle everything else in the
        // OnContainerContentChanging method
        if (args.CollectionChange is CollectionChange.ItemInserted or CollectionChange.ItemRemoved)
        {
            for (var i = (int)args.Index; i < sender.Count; i++)
            {
                if (AssociatedObject.ContainerFromIndex(i) is SelectorItem itemContainer)
                {
                    UpdateAlternateLayout(itemContainer, i);
                }
            }
        }
    }

    private void OnContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args
    )
    {
        if (args.Phase > 0 || args.InRecycleQueue)
        {
            return;
        }

        UpdateAlternateLayout(args.ItemContainer, args.ItemIndex);
    }

    private void UpdateAlternateLayout(SelectorItem itemContainer, int itemIndex)
    {
        if (itemIndex < 0 || AlternateBackground is null)
        {
            return;
        }

        var evenBackground = AlternateBackground;
        itemContainer.Background = itemIndex % 2 == 0 ? evenBackground : null;
        if (itemContainer.FindDescendant<Border>() is not { } border)
        {
            return;
        }

        if (itemIndex % 2 == 0)
        {
            border.Background = evenBackground;
            border.BorderBrush = AlternateBorderBrush;
            border.BorderThickness = AlternateBorderThickness;
        }
        else
        {
            border.Background = null;
            border.BorderThickness = default;
        }
    }

    public static Brush GetAlternateBackgroundBrush(bool isDarkTheme)
    {
        return isDarkTheme
            ? new SolidColorBrush(Color.FromArgb(240, 48, 53, 57))
            : new SolidColorBrush(Color.FromArgb(240, 253, 254, 254));
    }
}
