using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Activation;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player;
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx? MainWindow
    {
        get; private set;
    }

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) => //注册服务信息
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<PlayListsViewModel>();
            services.AddTransient<PlayQueueViewModel>();
            services.AddTransient<MusicLibraryViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<RootPlayBarViewModel>();
            services.AddTransient<LyricViewModel>();
            services.AddTransient<NoMusicViewModel>();
            services.AddTransient<HaveMusicViewModel>();
            services.AddTransient<LocalSongsViewModel>();
            services.AddSingleton<LocalAlbumsViewModel>();
            services.AddSingleton<LocalArtistsViewModel>();
            services.AddTransient<AlbumDetailViewModel>();
            services.AddTransient<ArtistDetailViewModel>();
            services.AddTransient<OnlineSongsViewModel>();
            services.AddTransient<DesktopLyricViewModel>();

            // Configuration
            services.AddOptions<LocalSettingsOptions>()
                    .Configure<IConfiguration>((settings, configuration) =>
                    {
                        var section = configuration.GetSection(nameof(LocalSettingsOptions));
                        // 手动绑定其他属性
                    });
        }).
        Build();//生成容器
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow = new MainWindow();
        await GetService<IActivationService>().ActivateAsync(args);
    }
}
