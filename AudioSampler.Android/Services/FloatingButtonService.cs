using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media.Projection;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AudioSampler.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;

namespace AudioSampler.Android.Services
{
    [Service(Enabled = true, Exported = false)]
    public class FloatingButtonService : Service, View.IOnTouchListener
    {
        private IWindowManager _windowManager;
        private Button _floatingButton;
        private WindowManagerLayoutParams _params;
        private float _initialX;
        private float _initialY;
        private float _initialTouchX;
        private float _initialTouchY;


        private bool _isRecording = false; // Флаг текущего состояния
        private GradientDrawable _buttonBackground; // Объект для управления формой и цветом

        public override IBinder OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();

            _windowManager = GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

            // Переводим 60 dp в реальные пиксели экрана устройства
            int buttonSize = (int)(60 * Resources.DisplayMetrics.Density);

            _floatingButton = new Button(this);
            _floatingButton.SetOnTouchListener(this);

            // Устанавливаем начальный круглый вид
            _isRecording = false;
            UpdateButtonVisuals();

            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            // Вместо WrapContent задаем жесткие размеры buttonSize
            _params = new WindowManagerLayoutParams(
                buttonSize,
                buttonSize,
                layoutType,
                WindowManagerFlags.NotFocusable,
                Format.Translucent
            );

            _params.Gravity = GravityFlags.Top | GravityFlags.Left;
            _params.X = 100;
            _params.Y = 100;

            _windowManager.AddView(_floatingButton, _params);
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    _initialX = _params.X;
                    _initialY = _params.Y;
                    _initialTouchX = e.RawX;
                    _initialTouchY = e.RawY;
                    return true;

                case MotionEventActions.Move:
                    _params.X = (int)(_initialX + (e.RawX - _initialTouchX));
                    _params.Y = (int)(_initialY + (e.RawY - _initialTouchY));
                    _windowManager.UpdateViewLayout(_floatingButton, _params);
                    return true;

                case MotionEventActions.Up:
                    if (Math.Abs(e.RawX - _initialTouchX) < 10 && Math.Abs(e.RawY - _initialTouchY) < 10)
                    {
                        OnButtonClick();
                    }
                    return true;
            }
            return false;
        }

        private void OnButtonClick()
        {

            // Переключаем состояние (если был круг — станет квадрат, и наоборот)
            _isRecording = !_isRecording;

            // Мгновенно перерисовываем кнопку на экране
            UpdateButtonVisuals();

            WeakReferenceMessenger.Default.Send(new ToggleRecordMessage(_isRecording));

            // НЕОБЯЗАТЕЛЬНО: Если нужно, чтобы при нажатии на "Стоп" приложение открывалось обратно:
            //if (!_isRecording)
            //{
            //    Intent dialogIntent = new Intent(this, typeof(MainActivity));
            //    dialogIntent.AddFlags(ActivityFlags.NewTask);
            //    StartActivity(dialogIntent);
            //    StopSelf(); // Закрываем плавающую кнопку
            //}
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_floatingButton != null)
            {
                _windowManager.RemoveView(_floatingButton);
            }
        }


        private void UpdateButtonVisuals()
        {
            if (_floatingButton == null) return;

            _buttonBackground = new GradientDrawable();

            if (!_isRecording)
            {
                // СОСТОЯНИЕ 1: Круглая красная кнопка (Старт)
                _buttonBackground.SetShape(ShapeType.Rectangle);
                // Делаем радиус закругления огромным (половина от размера кнопки), чтобы получился идеальный круг
                _buttonBackground.SetCornerRadius(1000);
                _buttonBackground.SetColor(Color.ParseColor("#FF3B30")); // Яркий Apple-Red цвет
                _floatingButton.Text = ""; // Текст убираем, круг говорит сам за себя
            }
            else
            {
                // СОСТОЯНИЕ 2: Квадратная кнопка (Стоп)
                _buttonBackground.SetShape(ShapeType.Rectangle);
                // Маленький радиус для легкого сглаживания углов квадрата
                _buttonBackground.SetCornerRadius(15);
                _buttonBackground.SetColor(Color.ParseColor("#1C1C1E")); // Темный стильный цвет фона
                _floatingButton.Text = "■"; // Символ квадрата "Стоп"
                _floatingButton.SetTextColor(Color.ParseColor("#FF3B30")); // Сам квадрат делаем красным
                _floatingButton.TextSize = 24; // Увеличиваем размер иконки стопа
            }

            // Применяем созданный фон к кнопке
            _floatingButton.Background = _buttonBackground;
        }


    }
}
