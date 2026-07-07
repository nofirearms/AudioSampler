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
using Avalonia.Rendering;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;

namespace AudioSampler.Android
{
    [Activity(
        Label = "Audio Sampler",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@mipmap/ic_launcher",
        MainLauncher = true,
        SupportsPictureInPicture = true, // Включает PiP, не спользуется
        CanDisplayOnRemoteDevices = true, // Позволяет ОС управлять окном
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : AvaloniaMainActivity
    {
        private const int CaptureRequestCode = 1001;

        private AndroidScreenCaptureService _androidScreenCaptureService;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //костыль чтобы системные кнопки были белыми
            Window.SetNavigationBarColor(Color.Black);


            // Создаем реальный сервис, передавая ему контекст этой Activity
            _androidScreenCaptureService = new AndroidScreenCaptureService(this);
            LazyScreenCaptureServiceWrapper.Instance.RegisterRealService(_androidScreenCaptureService);

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


        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopSharing();

            //костыль, AudioProjectionCallback при destroy срабатывает бозже чем отписка в _androidScreenCaptureService?.Dispose();
            //из-за этого сообщение не доставляется
            //TODO ПЕРЕДЕЛАТЬ СИСТЕМУ СООБЩЕНИЙ ПОЛСНОСТЬЮ 
            WeakReferenceMessenger.Default.Send(new SharingStateChangedMessage(false));

            _androidScreenCaptureService?.Dispose();
            _androidScreenCaptureService = null;

            
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

                Toast.MakeText(this, "Enable notification access for the rewind button", ToastLength.Long)?.Show();
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
