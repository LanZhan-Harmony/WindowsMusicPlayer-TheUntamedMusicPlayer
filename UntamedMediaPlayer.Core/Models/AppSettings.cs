using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMediaPlayer.Core.Enums;

namespace UntamedMediaPlayer.Core.Models;

public sealed partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    public partial ThemeMode AppTheme { get; set; } = ThemeMode.System;
}
