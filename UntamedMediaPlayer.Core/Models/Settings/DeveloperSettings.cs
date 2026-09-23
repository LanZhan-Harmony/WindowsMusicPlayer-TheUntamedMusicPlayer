using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace UntamedMediaPlayer.Core.Models.Settings;

public sealed partial class DeveloperSettings : ObservableObject
{
    [ObservableProperty]
    public partial LogLevel LogMinimalLevel { get; set; } = LogLevel.Warning;

    [ObservableProperty]
    public partial int LogRetentionDays { get; set; } = 7;
}
