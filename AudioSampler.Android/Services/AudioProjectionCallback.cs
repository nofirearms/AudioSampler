using Android.Content;
using Android.Media.Projection;
using AudioSampler.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Android.Services
{
    /// <summary>
    /// Callback для ослеживания остановки шаринга экрана и аудио
    /// </summary>
    public class AudioProjectionCallback : MediaProjection.Callback
    {
        public override void OnStop()
        {
            base.OnStop();

            // Сюда мы попадаем АВТОМАТИЧЕСКИ, как только sharing остановился!
            var context = global::Android.App.Application.Context;

            // 1. Принудительно шлем сообщение в твой класс записи, чтобы сохранить последний WAV, если шла запись
            WeakReferenceMessenger.Default.Send(new ToggleRecordMessage( Model.RecordingAction.Cancel));
            //Шлём сообщение об остановке шаринга, отлавливаем в AndroidScreenCaptureService
            WeakReferenceMessenger.Default.Send(new SharingStateChangedMessage(false));

            // 2. Гасим сервис плавающей панели (она исчезнет с экрана)
            var buttonServiceIntent = new Intent(context, typeof(FloatingButtonService));
            context.StopService(buttonServiceIntent);
        }
    }
}
