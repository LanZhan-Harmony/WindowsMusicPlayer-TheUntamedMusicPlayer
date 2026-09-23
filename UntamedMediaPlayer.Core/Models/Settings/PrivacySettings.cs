using CommunityToolkit.Mvvm.ComponentModel;

namespace UntamedMediaPlayer.Core.Models.Settings;

public sealed partial class PrivacySettings : ObservableObject
{
    [ObservableProperty]
    public partial bool RememberRecentMedias { get; set; }
}
