using Android;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media.Projection;
using Android.OS;
using Android.Runtime;
using Android.Util;
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
        private WindowManagerLayoutParams _params;

        private float _initialX;
        private float _initialY;
        private float _initialTouchX;
        private float _initialTouchY;

        private View? _panelView;
        private Button? _btnRecord;
        private Button? _btnStopRecord;
        private Button? _btnRewind;
        private Button? _btnClosePanel;


        private bool _isRecording = false; // Флаг текущего состояния


        private bool _isDestroyed = false;
        private bool _isDragging = false;
        
        private const float  DRAG_THRESHOLD = 5;
        public override IBinder OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();

            _windowManager = GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

            // 1. Загружаем наш XML лейаут
            var inflater = LayoutInflater.From(this);
            _panelView = inflater.Inflate(Resource.Layout.floating_panel, null);

            // 2. Находим кнопки по их ID из XML
            _btnRecord = _panelView.FindViewById<Button>(Resource.Id.btn_record);
            _btnStopRecord = _panelView.FindViewById<Button>(Resource.Id.btn_stop_record);
            _btnRewind = _panelView.FindViewById<Button>(Resource.Id.btn_rewind);
            _btnClosePanel = _panelView.FindViewById<Button>(Resource.Id.btn_close_panel);


            // 3. Вешаем перетаскивание (Drag & Drop) на ВСЮ панель целиком
            _panelView.SetOnTouchListener(this); // Не забудь добавить View.IOnTouchListener к классу CaptureService

            // 4. Настраиваем клики
            //_btnRecordToggle.Click += (s, e) => OnRecordToggleClick();
            //_btnRewind.Click += (s, e) => OnRewindClick();
            //_btnClosePanel.Click += (s, e) => OnClosePanelClick();

            // 5. Задаем начальный визуальный стиль кнопке записи (круг/квадрат)
            _isRecording = false;

            UpdateButtonsVisibility(false);

            // Настройки окна WindowManager (теперь WrapContent, панель сама примет нужный размер)
            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            _params = new WindowManagerLayoutParams(
                WindowManagerLayoutParams.WrapContent,
                WindowManagerLayoutParams.WrapContent,
                layoutType,
                WindowManagerFlags.NotFocusable | WindowManagerFlags.AllowLockWhileScreenOn,
                Format.Translucent
            );

            _params.Gravity = GravityFlags.Top | GravityFlags.Left;
            _params.X = 100;
            _params.Y = 100;

            // Добавляем готовую красивую панель на экран поверх всех приложений
            _windowManager.AddView(_panelView, _params);
        }

        // И обрабатываем все в OnTouch
        public bool OnTouch(View v, MotionEvent e)
        {
            if (_panelView == null || _isDestroyed)
                return false;

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    _initialX = _params.X - e.RawX;
                    _initialY = _params.Y - e.RawY;
                    _initialTouchX = e.RawX;
                    _initialTouchY = e.RawY;
                    _isDragging = false;
                    return true;

                case MotionEventActions.Move:
                    float deltaX = e.RawX - _initialTouchX;
                    float deltaY = e.RawY - _initialTouchY;

                    if (Math.Abs(deltaX) > DRAG_THRESHOLD || Math.Abs(deltaY) > DRAG_THRESHOLD)
                    {
                        _isDragging = true;
                    }

                    if (_isDragging)
                    {
                        _params.X = (int)(e.RawX + _initialX);
                        _params.Y = (int)(e.RawY + _initialY);
                        _windowManager.UpdateViewLayout(_panelView, _params);
                    }
                    return true;

                case MotionEventActions.Up:
                    if (!_isDragging)
                    {
                        // Определяем, по какой кнопке кликнули
                        int[] panelLocation = new int[2];
                        _panelView.GetLocationOnScreen(panelLocation);
                        float x = e.RawX - panelLocation[0];
                        float y = e.RawY - panelLocation[1];

                        // Проверяем только видимые кнопки!
                        if (_btnRecord.Visibility == ViewStates.Visible && IsInsideView(_btnRecord, x, y))
                        {
                            OnRecordToggleClick(true);
                        }
                        else if (_btnStopRecord.Visibility == ViewStates.Visible && IsInsideView(_btnStopRecord, x, y))
                        {
                            OnRecordToggleClick(false);
                        }
                        else if (_btnRewind.Visibility == ViewStates.Visible && IsInsideView(_btnRewind, x, y))
                        {
                            // OnRewindClick();
                        }
                        else if (_btnClosePanel.Visibility == ViewStates.Visible && IsInsideView(_btnClosePanel, x, y))
                        {
                            HardStopAndCloseEverything();
                        }
                    }
                    return true;
            }
            return false;
        }

        private bool IsInsideView(View view, float x, float y)
        {
            if (view == null) return false;

            int[] viewLocation = new int[2];
            view.GetLocationOnScreen(viewLocation);

            int[] panelLocation = new int[2];
            _panelView.GetLocationOnScreen(panelLocation);

            float viewX = viewLocation[0] - panelLocation[0];
            float viewY = viewLocation[1] - panelLocation[1];

            return x >= viewX && x <= viewX + view.Width &&
                   y >= viewY && y <= viewY + view.Height;
        }

        private void OnRecordToggleClick(bool isRecording)
        {

            _isRecording = isRecording;

            UpdateButtonsVisibility(_isRecording);

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
            if (_panelView != null) _windowManager.RemoveView(_panelView);
        }


        private void HardStopAndCloseEverything()
        {
            // 1. Если в этот момент шла запись — принудительно выключаем цикл
            if (_isRecording)
            {
                _isRecording = false;
            }


            // Отправляем команду на жесткую остановку шеринга в CaptureService
            WeakReferenceMessenger.Default.Send(new HardStopSharingMessage());

            // И сразу удаляем саму плавающую панель с экрана, раз пользователь её закрыл
            if (_windowManager != null && _panelView != null)
            {
                _windowManager.RemoveView(_panelView);
                _panelView = null;
            }

            // 5. Полностью выключаем сам Foreground-сервис и убираем уведомление из шторки
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
        }


        private void UpdateButtonsVisibility(bool isRecording)
        {
            if (isRecording)
            {
                _btnRecord.Visibility = ViewStates.Gone;
                _btnStopRecord.Visibility = ViewStates.Visible;
            }
            else
            {
                _btnRecord.Visibility = ViewStates.Visible;
                _btnStopRecord.Visibility = ViewStates.Gone;
            }
        }


    }
}
