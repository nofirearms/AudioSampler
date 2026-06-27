using AudioSampler.Database;
using AudioSampler.Model;
using AudioSampler.Services;
using AudioSampler.ViewModels;
using AudioSampler.Views;
using Avalonia;
using Avalonia.Controls; // Для PageNavigationHost
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ManagedBass;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;


namespace AudioSampler
{
    public partial class App : Application
    {

        public override void Initialize()
        {
            //this.EnableHotReload();
            AvaloniaXamlLoader.Load(this);
        }


        public App()
        {
            Services = ConfigureServices();
        }

        public static App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddTransient<IScreenCaptureService>(provider => LazyScreenCaptureServiceWrapper.Instance);

            services.AddDbContext<AppDbContext>();

            services.AddSingleton<AudioSamplesRepository>();
            services.AddSingleton<SettingsRepository>();
            services.AddSingleton<DataService>();
            services.AddSingleton<ModalService>();
            services.AddTransient<AudioService>();
            services.AddSingleton<ThemeService>();

            services.AddSingleton<ViewModelFactory>();

            services.AddSingleton<MainViewModel>();


            return services.BuildServiceProvider();
        }


        public override async void OnFrameworkInitializationCompleted()
        {
            var themeService = Services.GetRequiredService<ThemeService>();
            await themeService.LoadFromSettings();

            var mainViewModel = Design.IsDesignMode ? new MainViewModel() : Services.GetRequiredService<MainViewModel>();
            

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel,
                    Height = 800,
                    Width = 500
                };
                
            }
            else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
            {
                singleViewFactoryApplicationLifetime.MainViewFactory = () => new PageNavigationHost()
                {
                    Page = new MainView { DataContext = mainViewModel }
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new PageNavigationHost()
                {
                    Page = new MainView { DataContext = mainViewModel }
                };
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}