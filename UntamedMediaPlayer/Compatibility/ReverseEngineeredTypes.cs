using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

public sealed partial class PlaylistsDropCommandAdapter : ICommand
{
    public event EventHandler? CanExecuteChanged;

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
