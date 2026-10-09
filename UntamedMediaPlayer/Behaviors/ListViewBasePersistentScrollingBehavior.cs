using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using Windows.Foundation.Collections;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Restores the list's vertical offset after its item collection changes.
/// </summary>
internal sealed class ListViewBasePersistentScrollingBehavior : Behavior<ListViewBase>
{
    private ScrollViewer? _scrollViewer;
    private double _verticalOffset;
    private bool _isRestoring;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.Unloaded += OnUnloaded;
        AssociatedObject.Items.VectorChanged += OnItemsChanged;
        HookScrollViewer();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.Unloaded -= OnUnloaded;
        AssociatedObject.Items.VectorChanged -= OnItemsChanged;
        _scrollViewer?.ViewChanged -= OnViewChanged;
        _scrollViewer = null;
        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        HookScrollViewer();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (_scrollViewer is not null)
        {
            _verticalOffset = _scrollViewer.VerticalOffset;
        }
    }

    private void HookScrollViewer()
    {
        ScrollViewer? scrollViewer = AssociatedObject.FindDescendant<ScrollViewer>();
        if (ReferenceEquals(_scrollViewer, scrollViewer))
        {
            return;
        }

        if (_scrollViewer is not null)
        {
            _verticalOffset = _scrollViewer.VerticalOffset;
            _scrollViewer.ViewChanged -= OnViewChanged;
        }

        _scrollViewer = scrollViewer;
        if (_scrollViewer is not null)
        {
            _verticalOffset = _scrollViewer.VerticalOffset;
            _scrollViewer.ViewChanged += OnViewChanged;
        }
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        if (!_isRestoring && sender is ScrollViewer scrollViewer)
        {
            _verticalOffset = scrollViewer.VerticalOffset;
        }
    }

    private void OnItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        AssociatedObject.DispatcherQueue.TryEnqueue(() =>
        {
            HookScrollViewer();
            if (_scrollViewer is null)
            {
                return;
            }

            _isRestoring = true;
            _scrollViewer.ChangeView(null, _verticalOffset, null, true);
            _isRestoring = false;
        });
    }
}
