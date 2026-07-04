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
using AudioSampler.Messages;
using AudioSampler.Services;
using Avalonia;
using Avalonia.Android;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;

namespace AudioSampler.Android
{
    [Activity(
        Label = "Audio Sampler",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@mipmap/icon",
        MainLauncher = true,
        SupportsPictureInPicture = true, // Включает PiP
        CanDisplayOnRemoteDevices = true, // Позволяет ОС управлять окном
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity
    {
        private const int CaptureRequestCode = 1001;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //костыль чтобы системные кнопки были белыми
            Window.SetNavigationBarColor(Color.Black);


            // Создаем реальный сервис, передавая ему контекст этой Activity
            LazyScreenCaptureServiceWrapper.Instance.RegisterRealService(new AndroidScreenCaptureService(this));

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


        public void StopSharing()
        {
            WeakReferenceMessenger.Default.Send(new HardStopSharingMessage());
        }

        public void StartSharing()
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
