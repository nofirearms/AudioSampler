using Android.App;
using Android.Service.Notification;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Android.Services
{
    [Service(Label = "AudioSampler Controller", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE", Exported = true)]
    [IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
    public class MediaSessionListener : NotificationListenerService
    {
        // Класс оставляем пустым, он нужен только как маркер для Android
    }
}
