using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public partial class AcrylicVisualWithBoundedCornerRadius : AcrylicVisual
{
    public AcrylicVisualWithBoundedCornerRadius(FrameworkElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        // 创建绑定到源元素的 CornerRadius
        var cornerRadiusBinding = new Binding
        {
            Source = element,
            Path = new PropertyPath(nameof(CornerRadius)),
        };
        SetBinding(CornerRadiusProperty, cornerRadiusBinding);
    }
}
