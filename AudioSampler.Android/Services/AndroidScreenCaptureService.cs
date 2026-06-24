using Android.App;
using Android.Content;
using Android.Media.Projection;
using Android.OS;
using AudioSampler.Messages;
using AudioSampler.Model;
using AudioSampler.Services;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;


namespace AudioSampler.Android.Services
{
    public class AndroidScreenCaptureService : IScreenCaptureService
    {
        private readonly MainActivity _activity;
        private const int CaptureRequestCode = 1001;

        public event Action<RecordResult> RecordFinished;
        public event Action<bool> SharingStateChanged;

        public AndroidScreenCaptureService()
        {
            _activity = MainActivity.Instance;

            WeakReferenceMessenger.Default.Register<RecordFinishedMessage>(this, (r, m) =>
            {
                RecordFinished?.Invoke(m.Value);
            });

            WeakReferenceMessenger.Default.Register<SharingStateChangedMessage>(this, (r, m) =>
            {
                SharingStateChanged?.Invoke(m.IsActive);
            });
        }

        public void StartScreenCapture()
        {
            var projectionManager = (MediaProjectionManager?)_activity.GetSystemService(Context.MediaProjectionService);
            if (projectionManager != null)
            {
                var intent = projectionManager.CreateScreenCaptureIntent();
                _activity.StartActivityForResult(intent, CaptureRequestCode);
            }
        }

        public void StopScreenCapture()
        {
            var serviceIntent = new Intent(_activity, typeof(CaptureService));
            serviceIntent.SetAction("ACTION_STOP_CAPTURE");
            _activity.StartForegroundService(serviceIntent);
        }


        public void StartSharing()
        {
            _activity.StartSharing();
        }

        public void StopSharing()
        {
            _activity.StopSharing();
        }

        //public void EnterMiniMode()
        //{
        //    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        //    {
        //        // Создаем параметры с пропорциями 1 к 1
        //        var pipParams = new global::Android.App.PictureInPictureParams.Builder()
        //            .SetAspectRatio(new global::Android.Util.Rational(1, 1))
        //            .Build();

        //        // Системный метод Android. Если прав нет — он сам вернет false и ничего не сделает
        //        _activity.EnterPictureInPictureMode(pipParams);
        //    }
        //}

    }
}
