using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using UntamedMediaPlayer.Behaviors;

namespace UntamedMediaPlayer.Controls.CustomItemPresenters;

public sealed partial class CustomListViewItemPresenter : ListViewItemPresenter
{
    public ControlTemplate? Template
    {
        get => (ControlTemplate?)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
        nameof(Template),
        typeof(ControlTemplate),
        typeof(CustomListViewItemPresenter),
        new PropertyMetadata(null)
    );

    public object? MediaType
    {
        get => GetValue(MediaTypeProperty);
        set => SetValue(MediaTypeProperty, value);
    }

    public static readonly DependencyProperty MediaTypeProperty = DependencyProperty.Register(
        nameof(MediaType),
        typeof(object),
        typeof(CustomListViewItemPresenter),
        new PropertyMetadata(null)
    );

    public bool IsCurrentItem
    {
        get => (bool)GetValue(IsCurrentItemProperty);
        set => SetValue(IsCurrentItemProperty, value);
    }

    public static readonly DependencyProperty IsCurrentItemProperty = DependencyProperty.Register(
        nameof(IsCurrentItem),
        typeof(bool),
        typeof(CustomListViewItemPresenter),
        new PropertyMetadata(false)
    );

    public Brush? AlternateBackground
    {
        get => (Brush?)GetValue(AlternateBackgroundProperty);
        set => SetValue(AlternateBackgroundProperty, value);
    }

    public static readonly DependencyProperty AlternateBackgroundProperty =
        DependencyProperty.Register(
            nameof(AlternateBackground),
            typeof(Brush),
            typeof(AlternateListViewBehavior),
            new PropertyMetadata(default(Brush))
        );

    public Thickness AlternateBorderThickness
    {
        get => (Thickness)GetValue(AlternateBorderThicknessProperty);
        set => SetValue(AlternateBorderThicknessProperty, value);
    }

    public static readonly DependencyProperty AlternateBorderThicknessProperty =
        DependencyProperty.Register(
            nameof(AlternateBorderThickness),
            typeof(Thickness),
            typeof(AlternateListViewBehavior),
            new PropertyMetadata(default(Thickness))
        );

    public Brush? AlternateBorderBrush
    {
        get => (Brush?)GetValue(AlternateBorderBrushProperty);
        set => SetValue(AlternateBorderBrushProperty, value);
    }

    public static readonly DependencyProperty AlternateBorderBrushProperty =
        DependencyProperty.Register(
            nameof(AlternateBorderBrush),
            typeof(Brush),
            typeof(AlternateListViewBehavior),
            new PropertyMetadata(default(Brush?))
        );

    public bool IsAlternate
    {
        get => (bool)GetValue(IsAlternateProperty);
        set => SetValue(IsAlternateProperty, value);
    }

    public static readonly DependencyProperty IsAlternateProperty = DependencyProperty.Register(
        nameof(IsAlternate),
        typeof(bool),
        typeof(CustomListViewItemPresenter),
        new PropertyMetadata(false, OnIsAlternateChanged)
    );

    private static void OnIsAlternateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ((CustomListViewItemPresenter)d).ApplyAlternate();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyAlternate();
    }

    private void ApplyAlternate()
    {
        if (IsAlternate)
        {
            Background = AlternateBackground;
            BorderBrush = AlternateBorderBrush;
            BorderThickness = AlternateBorderThickness;
        }
        else
        {
            Background = null;
            BorderBrush = null;
            BorderThickness = default;
        }
    }
}
