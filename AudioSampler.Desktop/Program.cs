using AudioSampler.Desktop.Services;
using AudioSampler.Services;
using Avalonia;
using System;
using System.Runtime.CompilerServices;

namespace AudioSampler.Desktop
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {

            LazyScreenCaptureServiceWrapper.Instance.RegisterRealService(new TempRecordingService());

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}
