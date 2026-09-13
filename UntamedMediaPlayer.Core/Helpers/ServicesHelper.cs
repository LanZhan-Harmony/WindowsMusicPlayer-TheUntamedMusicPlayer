using Microsoft.Extensions.DependencyInjection;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Core.Services;
using UntamedMediaPlayer.Core.ViewModels;

namespace UntamedMediaPlayer.Core.Helpers;

public class ServicesHelper
{
    public static void ConfigureCoreServices(IServiceCollection services)
    {
        services
            .AddTransient<SettingsViewModel>()
            .AddTransient<PlayQueueViewModel>()
            .AddTransient<PlaylistViewModel>()
            .AddSingleton<ISettingsService, SettingsService>();
    }
}
