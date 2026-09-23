using CommunityToolkit.Mvvm.DependencyInjection;
using UntamedMediaPlayer.Core.Contracts.Services;

namespace UntamedMediaPlayer.Core.Helpers;

internal static class LocalizationExtensions
{
    private static readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    extension(string resourceKey)
    {
        internal string GetLocalized()
        {
            return _localizationService.GetString(resourceKey);
        }

        internal string GetLocalized(params object[] args)
        {
            return _localizationService.GetString(resourceKey, args);
        }
    }
}
