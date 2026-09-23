using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMediaPlayer.Core.Enums;

namespace UntamedMediaPlayer.Core.Models.Settings;

public sealed partial class PersonalizationSettings : ObservableObject
{
    /// <summary>
    /// Gets or sets the application theme mode.
    /// </summary>
    [ObservableProperty]
    public partial ThemeMode AppTheme { get; set; } = ThemeMode.System;

    [ObservableProperty]
    public partial MaterialMode AppMaterial { get; set; } = MaterialMode.DesktopAcrylic;
}
