using Android.Content;
using AudioSampler.Services;
using AudioSampler.Android.Services;

namespace AudioSampler.Android.Services
{
    public class AndroidFloatingWidgetService : IFloatingWidgetService
    {
        public void MinimizeToFloatingButton()
        {
            var activity = MainActivity.Instance;
            if (activity == null) return;

            // 1. Проверяем разрешение (если есть — запускаем)
            if (global::Android.Provider.Settings.CanDrawOverlays(activity))
            {
                // Запускаем твой FloatingButtonService
                var intent = new Intent(activity, typeof(FloatingButtonService));
                activity.StartService(intent);

                // Имитируем нажатие кнопки "Домой", чтобы свернуть саму Avalonia
                var homeIntent = new Intent(Intent.ActionMain);
                homeIntent.AddCategory(Intent.CategoryHome);
                homeIntent.SetFlags(ActivityFlags.NewTask);
                activity.StartActivity(homeIntent);
            }
            else
            {
                // Если разрешения нет, перенаправляем в настройки (вызываем метод из MainActivity)
                activity.CheckOverlayPermission();
            }
        }
    }
}
