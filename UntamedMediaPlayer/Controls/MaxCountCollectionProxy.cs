using System.Collections;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// 包装一个源集合，只暴露前 MaxCount 项。简化实现。
/// </summary>
public sealed class MaxCountCollectionProxy
    : DependencyObject,
        IEnumerable<object>,
        INotifyCollectionChanged
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(IEnumerable),
        typeof(MaxCountCollectionProxy),
        new PropertyMetadata(null, OnSourceChanged)
    );

    public IEnumerable? Source
    {
        get => (IEnumerable?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register(
        nameof(MaxCount),
        typeof(int),
        typeof(MaxCountCollectionProxy),
        new PropertyMetadata(int.MaxValue, OnMaxCountChanged)
    );

    public int MaxCount
    {
        get => (int)GetValue(MaxCountProperty);
        set => SetValue(MaxCountProperty, value);
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public IEnumerator<object> GetEnumerator()
    {
        if (Source is null)
        {
            return Enumerable.Empty<object>().GetEnumerator();
        }
        return Source.Cast<object>().Take(MaxCount).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MaxCountCollectionProxy proxy)
        {
            if (e.OldValue is INotifyCollectionChanged oldCc)
            {
                oldCc.CollectionChanged -= proxy.OnSourceCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newCc)
            {
                newCc.CollectionChanged += proxy.OnSourceCollectionChanged;
            }
            proxy.CollectionChanged?.Invoke(
                proxy,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
            );
        }
    }

    private static void OnMaxCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MaxCountCollectionProxy proxy)
        {
            proxy.CollectionChanged?.Invoke(
                proxy,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
            );
        }
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(
            this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
        );
    }
}
