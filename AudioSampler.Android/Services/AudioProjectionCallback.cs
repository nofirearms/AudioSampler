using Android.Content;
using Android.Media.Projection;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Android.Services
{
    public class AudioProjectionCallback : MediaProjection.Callback
    {
        public override void OnStop()
        {
            base.OnStop();

            // Сюда мы попадаем АВТОМАТИЧЕСКИ, как только sharing остановился!
            var context = global::Android.App.Application.Context;

            // 1. Принудительно шлем сообщение в твой класс записи, чтобы сохранить последний WAV, если шла запись
            WeakReferenceMessenger.Default.Send(new Messages.ToggleRecordMessage( Model.RecordingAction.Cancel));

            // 2. Гасим сервис плавающей панели (она исчезнет с экрана)
            var buttonServiceIntent = new Intent(context, typeof(FloatingButtonService));
            context.StopService(buttonServiceIntent);
        }
    }
}
