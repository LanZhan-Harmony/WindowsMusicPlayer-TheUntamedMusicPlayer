using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Activation;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Core.Contracts.Services;
using The_Untamed_Music_Player.Core.Services;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
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
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
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
        ConfigureServices((context, services) =>//注册服务信息
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
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<播放列表ViewModel>();
            services.AddTransient<播放列表Page>();
            services.AddTransient<播放队列ViewModel>();
            services.AddTransient<播放队列Page>();
            services.AddTransient<音乐库ViewModel>();
            services.AddTransient<音乐库Page>();
            services.AddTransient<主页Page>();
            services.AddTransient<主页ViewModel>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<RootPlayBarView>();
            services.AddTransient<RootPlayBarViewModel>();
            services.AddTransient<歌词Page>();
            services.AddTransient<歌词ViewModel>();
            services.AddTransient<无音乐Page>();
            services.AddTransient<无音乐ViewModel>();
            services.AddTransient<有音乐Page>();
            services.AddTransient<有音乐ViewModel>();
            services.AddTransient<歌曲Page>();
            services.AddTransient<歌曲ViewModel>();
            services.AddTransient<专辑Page>();
            services.AddTransient<专辑ViewModel>();
            services.AddTransient<专辑详情Page>();
            services.AddTransient<专辑详情ViewModel>();
            services.AddTransient<艺术家Page>();
            services.AddTransient<艺术家ViewModel>();
            services.AddTransient<艺术家详情Page>();
            services.AddTransient<艺术家详情ViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();//生成容器
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow = new MainWindow(GetService<ILocalSettingsService>());
        await GetService<IActivationService>().ActivateAsync(args);
    }
}
