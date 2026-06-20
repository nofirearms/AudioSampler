using Android.App;
using Android.Content;
using Android.Media.Projection;
using Android.OS;
using AudioSampler.Messages;
using AudioSampler.Services;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;


namespace AudioSampler.Android.Services
{
    public class AndroidScreenCaptureService : IScreenCaptureService
    {
        private readonly Activity _activity;
        private const int CaptureRequestCode = 1001;

        public AndroidScreenCaptureService()
        {
            _activity = MainActivity.Instance;
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


        public void ChooseApplicationGivePermission()
        {
            var projectionManager = (MediaProjectionManager?)_activity.GetSystemService(Context.MediaProjectionService);
            if (projectionManager != null)
            {
                var intent = projectionManager.CreateScreenCaptureIntent();
                _activity.StartActivityForResult(intent, CaptureRequestCode);
            }
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


        public void HandleActivityResult(int requestCode, int resultCode, Intent? data)
        {
            if (requestCode == CaptureRequestCode && resultCode == (int)Result.Ok && data != null)
            {
                var serviceIntent = new Intent(_activity, typeof(CaptureService));
                serviceIntent.SetAction("ACTION_START_CAPTURE");
                serviceIntent.PutExtra("RESULT_CODE", resultCode);
                serviceIntent.PutExtra("DATA", data);

                _activity.StartForegroundService(serviceIntent);
            }
        }






    }
}
