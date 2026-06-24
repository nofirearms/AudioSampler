using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media.Projection;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.View;
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
        private const int CaptureRequestCode = 1001;
        public static MainActivity Instance { get; private set; }

        private AndroidScreenCaptureService? _androidService;
        private AndroidFloatingWidgetService? _floatingService;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //WindowCompat.SetDecorFitsSystemWindows(Window, true);

            //костыль чтобы системные кнопки были белыми
            Window.SetNavigationBarColor(Color.Black);

            Instance = this;

            // Создаем реальный сервис, передавая ему контекст этой Activity
            _androidService = new AndroidScreenCaptureService();
            App.OnScreenCaptureServiceReady?.Invoke(_androidService);

            _floatingService = new AndroidFloatingWidgetService();
            App.OnFloatingWidgetServiceReady?.Invoke(_floatingService);

            CheckOverlayPermission();
            CheckNotificationPermission();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == CaptureRequestCode && resultCode == Result.Ok && data != null)
            {
                var serviceIntent = new Intent(this, typeof(CaptureService));
                serviceIntent.SetAction("ACTION_START_CAPTURE");
                serviceIntent.PutExtra("RESULT_CODE", (int)resultCode);
                serviceIntent.PutExtra("DATA", data);

                StartForegroundService(serviceIntent);
            }
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

        //public void HardStopSharing()
        //{
        //    if (_mediaProjection != null)
        //    {
        //        // Этот вызов вырубит трансляцию (значок в шторке пропадет) 
        //        // и автоматически стриггерит AudioProjectionCallback.OnStop()
        //        _mediaProjection.Stop();
        //        _mediaProjection = null;
        //    }
        //}

        public void ChooseApplicationGivePermission()
        {
            var projectionManager = (MediaProjectionManager?)GetSystemService(Context.MediaProjectionService);
            if (projectionManager != null)
            {
                var intent = projectionManager.CreateScreenCaptureIntent();
                StartActivityForResult(intent, CaptureRequestCode);

            }
        }

        public void CheckNotificationPermission()
        {
            var enabledListeners = NotificationManagerCompat.GetEnabledListenerPackages(this);

            // Проверяем, есть ли имя пакета нашего приложения в этом списке
            if (!enabledListeners.Contains(PackageName))
            {
                var intent = new Intent(Settings.ActionNotificationListenerSettings);
                intent.AddFlags(ActivityFlags.NewTask);
                StartActivity(intent);

                // Тут можно вывести Toast-сообщение: "Пожалуйста, включите доступ для AudioSampler"
                Toast.MakeText(this, "Включите доступ к уведомлениям для перемотки", ToastLength.Long)?.Show();
            }
        }

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
