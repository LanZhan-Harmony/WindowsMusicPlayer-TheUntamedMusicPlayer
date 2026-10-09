using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls.CustomDataTemplateSelectors;

/// <summary>
/// 根据媒体类型选择显示模板。简化实现：默认返回 DefaultTemplate。
/// </summary>
internal partial class MediaDisplayPropertiesTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }

    public DataTemplate? MusicTemplate { get; set; }

    public DataTemplate? VideoTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return DefaultTemplate;
    }
}

/// <summary>
/// 编辑媒体信息时使用的模板选择器。
/// </summary>
internal sealed partial class EditableMediaDisplayPropertiesTemplateSelector
    : MediaDisplayPropertiesTemplateSelector { }
