using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// 描述一个响应式列表断点：当容器宽度达到 MinContainerWidth 时使用 Columns 列。
/// </summary>
internal sealed class ResponsiveListBreakpoint : DependencyObject
{
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    private static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns),
        typeof(int),
        typeof(ResponsiveListBreakpoint),
        new PropertyMetadata(1)
    );

    public double MinContainerWidth
    {
        get => (double)GetValue(MinContainerWidthProperty);
        set => SetValue(MinContainerWidthProperty, value);
    }

    private static readonly DependencyProperty MinContainerWidthProperty =
        DependencyProperty.Register(
            nameof(MinContainerWidth),
            typeof(double),
            typeof(ResponsiveListBreakpoint),
            new PropertyMetadata(0d)
        );
}

/// <summary>
/// 响应式断点集合，根据容器宽度解析当前列数。
/// </summary>
internal sealed partial class ResponsiveListBreakpointCollection
    : Collection<ResponsiveListBreakpoint>
{
    public int GetColumnsForWidth(double width)
    {
        int result = 1;
        foreach (ResponsiveListBreakpoint? breakpoint in this.OrderBy(b => b.MinContainerWidth))
        {
            if (width >= breakpoint.MinContainerWidth)
            {
                result = breakpoint.Columns;
            }
        }
        return result;
    }
}
