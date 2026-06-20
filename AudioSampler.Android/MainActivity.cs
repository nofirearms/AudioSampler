using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using AudioSampler.Android.Services;
using Avalonia;
using Avalonia.Android;
using System.Diagnostics;

namespace AudioSampler.Android
{
    [Activity(
        Label = "AudioSampler.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        SupportsPictureInPicture = true, // Включает PiP
        CanDisplayOnRemoteDevices = true, // Позволяет ОС управлять окном
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity
    {
        public static MainActivity Instance { get; private set; }

        private AndroidScreenCaptureService? _androidService;
        private AndroidFloatingWidgetService? _floatingService;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Instance = this;

            // Создаем реальный сервис, передавая ему контекст этой Activity
            _androidService = new AndroidScreenCaptureService();
            App.OnScreenCaptureServiceReady?.Invoke(_androidService);

            _floatingService = new AndroidFloatingWidgetService();
            App.OnFloatingWidgetServiceReady?.Invoke(_floatingService);

            CheckOverlayPermission();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // Напрямую отдаем результат нашему локальному сервису
            _androidService?.HandleActivityResult(requestCode, (int)resultCode, data);
        }

        // Метод срабатывает, когда пользователь нажимает кнопку Home или сворачивает приложение
        //protected override void OnUserLeaveHint()
        //{
        //    base.OnUserLeaveHint();

        //    // Метод срабатывает, когда пользователь нажимает кнопку Home или сворачивает приложение
        //    if (Settings.CanDrawOverlays(this))
        //    {
        //        Intent intent = new Intent(this, typeof(FloatingButtonService));
        //        StartService(intent);
        //    }
        //}

        public void CheckOverlayPermission()
        {
            // 1. Проверяем наличие разрешения (используем полный путь к Android.Provider)
            if (!Settings.CanDrawOverlays(this))
            {
                // 2. Явно создаем Android.Net.Uri (не System.Uri!)
                Uri packageUri = Uri.Parse($"package:{this.PackageName}");

                // 3. Создаем Intent, используя перегрузку с единственным параметром строки действия (Action)
                Intent intent = new Intent(Settings.ActionManageOverlayPermission);

                // 4. Принудительно прикрепляем данные пакета к интенту
                intent.SetData(packageUri);

                // 5. Перенаправляем пользователя в настройки Android
                this.StartActivityForResult(intent, 1234);
            }
        }
    }
}
