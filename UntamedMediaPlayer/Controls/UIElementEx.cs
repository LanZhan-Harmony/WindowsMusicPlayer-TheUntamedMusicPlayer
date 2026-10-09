using Microsoft.UI.Xaml;
using UntamedMediaPlayer.Helpers;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// Attached-property bridge that applies a DropConfiguration to any WinUI element.
/// </summary>
internal static class UIElementEx
{
    private static readonly DependencyProperty DropConfigurationProperty =
        DependencyProperty.RegisterAttached(
            "DropConfiguration",
            typeof(DropConfiguration),
            typeof(UIElementEx),
            new PropertyMetadata(null, OnDropConfigurationChanged)
        );

    public static DropConfiguration? GetDropConfiguration(DependencyObject target)
    {
        return (DropConfiguration?)target.GetValue(DropConfigurationProperty);
    }

    public static void SetDropConfiguration(DependencyObject target, DropConfiguration? value)
    {
        target.SetValue(DropConfigurationProperty, value);
    }

    private static void OnDropConfigurationChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is not UIElement element)
        {
            return;
        }

        if (args.OldValue is not null)
        {
            element.DragOver -= OnDragOver;
            element.Drop -= OnDrop;
        }

        if (args.NewValue is DropConfiguration)
        {
            element.AllowDrop = true;
            element.DragOver += OnDragOver;
            element.Drop += OnDrop;
        }
    }

    private static void OnDragOver(object sender, DragEventArgs args)
    {
        if (
            sender is not DependencyObject target
            || GetDropConfiguration(target) is not { } configuration
        )
        {
            return;
        }

        args.AcceptedOperation = configuration.AcceptedOperation;
        ApplyDragUiOverride(args.DragUIOverride, configuration.AcceptedUIOverride);
        args.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs args)
    {
        if (
            sender is not DependencyObject target
            || GetDropConfiguration(target) is not { } configuration
        )
        {
            return;
        }

        args.AcceptedOperation = configuration.AcceptedOperation;
        if (configuration.Command?.CanExecute(args) == true)
        {
            configuration.Command.Execute(args);
        }

        args.Handled = true;
    }

    private static void ApplyDragUiOverride(
        DragUIOverride dragUIOverride,
        DragAndDropUIOverride? configuration
    )
    {
        if (configuration is null)
        {
            return;
        }

        dragUIOverride.IsCaptionVisible = configuration.IsCaptionVisible;
        dragUIOverride.IsContentVisible = configuration.IsContentVisible;
        dragUIOverride.IsGlyphVisible = configuration.IsGlyphVisible;
    }
}
