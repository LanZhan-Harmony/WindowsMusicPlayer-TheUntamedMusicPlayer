using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace UntamedMediaPlayer.Compatibility;

/// <summary>
/// Small value objects retained for resources recovered from the UWP markup.
/// They intentionally keep the original string value so existing commands and
/// templates can consume the resource without depending on the removed UWP
/// application model types.
/// </summary>
public sealed class PlaybackRate : DependencyObject
{
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(PlaybackRate),
        new PropertyMetadata(null)
    );
}

public sealed class MediaPlayerRepeatMode : PlaybackModeValue { }

public sealed class MediaPlayerDisplayMode : PlaybackModeValue { }

public sealed class ContentDisplayMode : PlaybackModeValue { }

public abstract class PlaybackModeValue : DependencyObject
{
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(PlaybackModeValue),
        new PropertyMetadata(null)
    );

    public override string ToString() => Value ?? string.Empty;
}

public sealed class DragAndDropUIOverride : DependencyObject
{
    public bool IsCaptionVisible
    {
        get => (bool)GetValue(IsCaptionVisibleProperty);
        set => SetValue(IsCaptionVisibleProperty, value);
    }

    public static readonly DependencyProperty IsCaptionVisibleProperty =
        DependencyProperty.Register(
            nameof(IsCaptionVisible),
            typeof(bool),
            typeof(DragAndDropUIOverride),
            new PropertyMetadata(false)
        );

    public bool IsContentVisible
    {
        get => (bool)GetValue(IsContentVisibleProperty);
        set => SetValue(IsContentVisibleProperty, value);
    }

    public static readonly DependencyProperty IsContentVisibleProperty =
        DependencyProperty.Register(
            nameof(IsContentVisible),
            typeof(bool),
            typeof(DragAndDropUIOverride),
            new PropertyMetadata(false)
        );

    public bool IsGlyphVisible
    {
        get => (bool)GetValue(IsGlyphVisibleProperty);
        set => SetValue(IsGlyphVisibleProperty, value);
    }

    public static readonly DependencyProperty IsGlyphVisibleProperty = DependencyProperty.Register(
        nameof(IsGlyphVisible),
        typeof(bool),
        typeof(DragAndDropUIOverride),
        new PropertyMetadata(false)
    );
}

/// <summary>Drop configuration recovered from the original UWP markup.</summary>
public sealed partial class DropConfiguration : DependencyObject
{
    public DataPackageOperation AcceptedOperation
    {
        get => (DataPackageOperation)GetValue(AcceptedOperationProperty);
        set => SetValue(AcceptedOperationProperty, value);
    }

    public static readonly DependencyProperty AcceptedOperationProperty =
        DependencyProperty.Register(
            nameof(AcceptedOperation),
            typeof(DataPackageOperation),
            typeof(DropConfiguration),
            new PropertyMetadata(DataPackageOperation.Copy)
        );

    public DragAndDropUIOverride? AcceptedUIOverride
    {
        get => (DragAndDropUIOverride?)GetValue(AcceptedUIOverrideProperty);
        set => SetValue(AcceptedUIOverrideProperty, value);
    }

    public static readonly DependencyProperty AcceptedUIOverrideProperty =
        DependencyProperty.Register(
            nameof(AcceptedUIOverride),
            typeof(DragAndDropUIOverride),
            typeof(DropConfiguration),
            new PropertyMetadata(null)
        );

    public DragAndDropUIOverride? RejectedUIOverride
    {
        get => (DragAndDropUIOverride?)GetValue(RejectedUIOverrideProperty);
        set => SetValue(RejectedUIOverrideProperty, value);
    }

    public static readonly DependencyProperty RejectedUIOverrideProperty =
        DependencyProperty.Register(
            nameof(RejectedUIOverride),
            typeof(DragAndDropUIOverride),
            typeof(DropConfiguration),
            new PropertyMetadata(null)
        );

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(DropConfiguration),
        new PropertyMetadata(null)
    );
}

/// <summary>Attached-property bridge that applies a DropConfiguration to any WinUI element.</summary>
public static class UIElementEx
{
    public static readonly DependencyProperty DropConfigurationProperty =
        DependencyProperty.RegisterAttached(
            "DropConfiguration",
            typeof(DropConfiguration),
            typeof(UIElementEx),
            new PropertyMetadata(null, OnDropConfigurationChanged)
        );

    public static DropConfiguration? GetDropConfiguration(DependencyObject target) =>
        (DropConfiguration?)target.GetValue(DropConfigurationProperty);

    public static void SetDropConfiguration(DependencyObject target, DropConfiguration? value) =>
        target.SetValue(DropConfigurationProperty, value);

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
        Microsoft.UI.Xaml.DragUIOverride dragUIOverride,
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

public sealed partial class PlayQueueDropCommandAdapter : DependencyObject, ICommand
{
    private EventHandler? _canExecuteChanged;

    public bool OpenPlayPage
    {
        get => (bool)GetValue(OpenPlayPageProperty);
        set => SetValue(OpenPlayPageProperty, value);
    }

    public static readonly DependencyProperty OpenPlayPageProperty = DependencyProperty.Register(
        nameof(OpenPlayPage),
        typeof(bool),
        typeof(PlayQueueDropCommandAdapter),
        new PropertyMetadata(false, OnOpenPlayPageChanged)
    );

    public event EventHandler? CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        // Queue operations are supplied by the playback service when it is connected.
    }

    private static void OnOpenPlayPageChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is PlayQueueDropCommandAdapter command)
        {
            command._canExecuteChanged?.Invoke(command, EventArgs.Empty);
        }
    }
}

/// <summary>Data definition used by GlobalKeyboardAcceleratorService.</summary>
public sealed partial class CommandKeyboardAccelerator : DependencyObject
{
    public VirtualKey Key
    {
        get => (VirtualKey)GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
        nameof(Key),
        typeof(VirtualKey),
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(VirtualKey.None)
    );

    public int KeyCode
    {
        get => (int)GetValue(KeyCodeProperty);
        set => SetValue(KeyCodeProperty, value);
    }

    public static readonly DependencyProperty KeyCodeProperty = DependencyProperty.Register(
        nameof(KeyCode),
        typeof(int),
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(0)
    );

    public VirtualKeyModifiers Modifiers
    {
        get => (VirtualKeyModifiers)GetValue(ModifiersProperty);
        set => SetValue(ModifiersProperty, value);
    }

    public static readonly DependencyProperty ModifiersProperty = DependencyProperty.Register(
        nameof(Modifiers),
        typeof(VirtualKeyModifiers),
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(VirtualKeyModifiers.None)
    );

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(CommandKeyboardAccelerator),
            new PropertyMetadata(null)
        );

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(null)
    );
}

public sealed partial class PlaylistsDropCommandAdapter : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        // The migrated project has no playlist service yet. Keeping this as a
        // no-op command preserves XAML activation and gives callers a valid
        // ICommand until the service is connected.
    }
}

public sealed partial class SemanticZoomOutTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GroupTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        GroupTemplate;

    protected override DataTemplate? SelectTemplateCore(object item) => GroupTemplate;
}

public partial class ModalNotificationTitleTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }
    public DataTemplate? EditAlbumInfoTitleTemplate { get; set; }
    public DataTemplate? EqualizerTitleTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        DefaultTemplate;

    protected override DataTemplate? SelectTemplateCore(object item) => DefaultTemplate;
}

public partial class ModalNotificationTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }
    public DataTemplate? CDRipSettingsTemplate { get; set; }
    public DataTemplate? EditAlbumInfoTemplate { get; set; }
    public DataTemplate? EditMediaInfoTemplate { get; set; }
    public DataTemplate? EqualizerControlTemplate { get; set; }
    public DataTemplate? MediaExtensionAcquisitionUserConsentTemplate { get; set; }
    public DataTemplate? MediaPropertiesTemplate { get; set; }
    public DataTemplate? PlaylistNameDialogTemplate { get; set; }
    public DataTemplate? UrlPickerTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        DefaultTemplate;

    protected override DataTemplate? SelectTemplateCore(object item) => DefaultTemplate;
}

public sealed partial class ValueSlider : Slider { }
