using AudioSampler.Database;
using AudioSampler.Model;
using AudioSampler.Services;
using AudioSampler.ViewModels;
using AudioSampler.Views;
using Avalonia;
using Avalonia.Controls; // Для PageNavigationHost
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;


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

        // Сюда мы лениво подложим реальный андроидный сервис, когда он создастся
        public static Action<IScreenCaptureService>? OnScreenCaptureServiceReady { get; set; }

        // Сюда мы лениво подложим реальный андроидный сервис, когда он создастся
        public static Action<IFloatingWidgetService>? OnFloatingWidgetServiceReady { get; set; }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Регистрируем ленивую прослойку: 
            // Когда ViewModel попросит IScreenCaptureService, контейнер будет ждать, 
            // пока Android не запишет туда себя.
            services.AddSingleton<LazyScreenCaptureServiceWrapper>();
            services.AddSingleton<IScreenCaptureService>(provider => provider.GetRequiredService<LazyScreenCaptureServiceWrapper>());

            services.AddSingleton<LazyFloatingWidgetServiceWrapper>();
            services.AddSingleton<IFloatingWidgetService>(provider => provider.GetRequiredService<LazyFloatingWidgetServiceWrapper>());


            services.AddDbContext<AppDbContext>();

            services.AddSingleton<AudioSamplesRepository>();
            services.AddSingleton<SettingsRepository>();
            services.AddSingleton<DataService>();
            services.AddSingleton<ModalService>();


            services.AddSingleton<MainViewModel>();


            return services.BuildServiceProvider();
        }


        public override void OnFrameworkInitializationCompleted()
        {


            var mainViewModel = Design.IsDesignMode ? new MainViewModel() : Services.GetRequiredService<MainViewModel>();
            

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
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

        // Вспомогательный класс-прослойка для ленивой загрузки
        public class LazyScreenCaptureServiceWrapper : IScreenCaptureService
        {
            private IScreenCaptureService? _realService;

            public LazyScreenCaptureServiceWrapper()
            {
                // Подписываемся на событие готовности платформы
                App.OnScreenCaptureServiceReady = service =>
                {
                    _realService = service;
                    _realService.RecordFinished += (value) => RecordFinished?.Invoke(value);
                    _realService.SharingStateChanged += (value) => SharingStateChanged?.Invoke(value);
                };

                
            }

            public void StartScreenCapture() => _realService?.StartScreenCapture();
            public void StopScreenCapture() => _realService?.StopScreenCapture();
            public void StartSharing() => _realService?.StartSharing();
            public void StopSharing() => _realService?.StopSharing();

            public event Action<RecordResult> RecordFinished;
            public event Action<bool> SharingStateChanged;
        }


        // Вспомогательный класс-прослойка для ленивой загрузки
        public class LazyFloatingWidgetServiceWrapper : IFloatingWidgetService
        {

            private IFloatingWidgetService? _floatingService;

            public LazyFloatingWidgetServiceWrapper()
            {
                // Подписываемся на событие готовности платформы
                App.OnFloatingWidgetServiceReady = service => _floatingService = service;
            }
            public void MinimizeToFloatingButton() => _floatingService?.MinimizeToFloatingButton();
        }
    }
}