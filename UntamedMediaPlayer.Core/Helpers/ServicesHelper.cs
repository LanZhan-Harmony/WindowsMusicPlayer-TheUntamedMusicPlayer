using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Core.Services;
using UntamedMediaPlayer.Core.ViewModels;

namespace UntamedMediaPlayer.Core.Helpers;

public static class ServicesHelper
{
    public static void ConfigureCoreServices(IServiceCollection services)
    {
        services
            .AddSingleton<LoggingService>()
            .AddSingleton<ILoggingService>(sp => sp.GetRequiredService<LoggingService>())
            .AddSingleton<ILoggerFactory>(sp => sp.GetRequiredService<LoggingService>().LoggerFactory)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddTransient<SettingsViewModel>()
            .AddTransient<PlayQueueViewModel>()
            .AddTransient<PlaylistViewModel>()
            .AddSingleton<ISettingsService, SettingsService>();
    }
}
