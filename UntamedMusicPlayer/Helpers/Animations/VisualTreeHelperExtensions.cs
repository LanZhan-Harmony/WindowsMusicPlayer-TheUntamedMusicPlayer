using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace UntamedMusicPlayer.Helpers.Animations;

public static class VisualTreeHelperExtensions
{
    /// <summary>
    /// Gets the implementation root of the Control.
    /// </summary>
    /// <param name="dependencyObject">The DependencyObject.</param>
    /// <returns>Returns the implementation root or null.</returns>
    public static FrameworkElement? GetImplementationRoot(DependencyObject dependencyObject)
    {
        return VisualTreeHelper.GetChildrenCount(dependencyObject) == 1
            ? VisualTreeHelper.GetChild(dependencyObject, 0) as FrameworkElement
            : null;
    }

    extension(Control control)
    {
        public VisualStateGroup? GetVisualStateGroup(string groupName)
        {
            if (
                GetImplementationRoot(control) is FrameworkElement f
                && VisualStateManager.GetVisualStateGroups(f) is IList<VisualStateGroup> groups
            )
            {
                return groups.FirstOrDefault(g => g.Name == groupName);
            }
            return null;
        }
    }

    extension(FrameworkElement dob)
    {
        /// <summary>
        /// Gets the bounding rectangle of a given element
        /// relative to a given other element or visual root
        /// if relativeTo is null or not specified.
        /// </summary>
        /// <param name="dob">The starting element.</param>
        /// <param name="relativeTo">The relative to element.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Element not in visual tree.</exception>
        public Rect GetBoundingRect(FrameworkElement relativeTo)
        {
            if (dob == relativeTo)
            {
                return new Rect(0, 0, relativeTo.ActualWidth, relativeTo.ActualHeight);
            }

            var pos = dob.TransformToVisual(relativeTo).TransformPoint(new Point());
            var pos2 = dob.TransformToVisual(relativeTo)
                .TransformPoint(new Point(dob.ActualWidth, dob.ActualHeight));

            return new Rect(pos, pos2);
        }
    }

    /// <summary>
    /// Gets the first descendant that is of the given type.
    /// </summary>
    /// <remarks>
    /// Returns null if not found.
    /// </remarks>
    /// <typeparam name="T">Type of descendant to look for.</typeparam>
    /// <param name="start">The start object.</param>
    /// <returns></returns>
    public static T? GetFirstDescendantOfType<T>(this FrameworkElement start, string name)
        where T : FrameworkElement
    {
        return start.FindDescendants().OfType<T>().FirstOrDefault(e => e.Name == name);
    }

    /// <summary>
    /// Gets the first descendant that is of the given type.
    /// </summary>
    /// <remarks>
    /// Returns null if not found.
    /// </remarks>
    /// <typeparam name="T">Type of descendant to look for.</typeparam>
    /// <param name="start">The start object.</param>
    /// <returns></returns>
    public static T? GetFirstDescendantOfType<T>(
        this DependencyObject start,
        Func<T, bool>? predicate = null
    )
        where T : DependencyObject
    {
        if (predicate is null)
        {
            return start.FindDescendant<T>();
        }
        else
        {
            return start.FindDescendants().OfType<T>().FirstOrDefault(predicate);
        }
    }

    public static IEnumerable<T> GetFirstLevelDescendantsOfType<T>(
        this FrameworkElement start,
        Predicate<T>? predicate = null
    )
    {
        var queue = new Queue<FrameworkElement>();
        var count = VisualTreeHelper.GetChildrenCount(start);

        for (var i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(start, i) is FrameworkElement child)
            {
                if (child is T c && (predicate == null || predicate(c)))
                {
                    yield return c;
                    continue;
                }
                else
                {
                    queue.Enqueue(child);
                }
            }
        }

        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();
            var count2 = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < count2; i++)
            {
                if (VisualTreeHelper.GetChild(parent, i) is FrameworkElement child)
                {
                    if (child is T c && (predicate == null || predicate(c)))
                    {
                        yield return c;
                        continue;
                    }
                    else
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }
    }

    public static bool ContainsFocus(this UIElement element)
    {
        if (element == null)
        {
            return false;
        }

        if (!(FocusManager.GetFocusedElement() is UIElement focused))
        {
            return false;
        }

        if (focused == element)
        {
            return true;
        }

        return focused.FindAscendants().Any(a => a == element);
    }
}
