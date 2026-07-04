using Android;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.Media.Projection;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Transitions;
using Android.Util;
using Android.Views;
using Android.Widget;
using AudioSampler.Messages;
using AudioSampler.Model;
using AudioSampler.Services;
using CommunityToolkit.Mvvm.Messaging;
using Java.Lang;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Timers;
using MediaController = Android.Media.Session.MediaController;


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
        private FrameLayout? _panelRoot;
        private Button? _btnRecord;
        private Button? _btnStopRecord;
        private Button? _btnPauseRecord;
        private Button? _btnResumeRecord;
        private Button? _btnCancelRecord;
        private Button? _btnRewind;
        private Button? _btnClosePanel;
        private TextView? _tvTimer;
        private TextView? _tvStatus;
        private LinearLayout? _recordingControlsGroup;
        private LinearLayout? _floatingToast;
        private LinearLayout? _llMainGroup;
        private Button? _btnMaximize;

        private RecordingState _recordingState = RecordingState.Initial; 
        public RecordingState RecordingState
        {
            get => _recordingState;
            set
            {
                OnRecordingStateChanged(_recordingState, value);;
                _recordingState = value;               
            }
        }


        private Handler _timerHandler;
        private Runnable _timerRunnable;
        private TimeSpan _recordingTime = TimeSpan.Zero;

        private DataService _dataService;



        public override IBinder OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();

            _windowManager = GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            _dataService = App.Current.Services.GetRequiredService<DataService>();

            // 1. Загружаем наш XML лейаут
            var inflater = LayoutInflater.From(this);
            _panelView = inflater.Inflate(Resource.Layout.floating_panel, null);

            _panelRoot = _panelView.FindViewById<FrameLayout>(Resource.Id.panel_root);
            _btnRecord = _panelView.FindViewById<Button>(Resource.Id.btn_record);
            _btnStopRecord = _panelView.FindViewById<Button>(Resource.Id.btn_stop_record);
            _btnPauseRecord = _panelView.FindViewById<Button>(Resource.Id.btn_pause_record);
            _btnResumeRecord = _panelView.FindViewById<Button>(Resource.Id.btn_resume_record);
            _btnCancelRecord = _panelView.FindViewById<Button>(Resource.Id.btn_cancel_record);
            _btnRewind = _panelView.FindViewById<Button>(Resource.Id.btn_rewind);
            _btnClosePanel = _panelView.FindViewById<Button>(Resource.Id.btn_close_panel);
            _tvTimer = _panelView.FindViewById<TextView>(Resource.Id.tv_timer);
            _recordingControlsGroup = _panelView.FindViewById<LinearLayout>(Resource.Id.recording_controls_group);
            _floatingToast = _panelView.FindViewById<LinearLayout>(Resource.Id.ll_toast);
            _llMainGroup = _panelView.FindViewById<LinearLayout>(Resource.Id.ll_main_group);
            _btnMaximize = _panelView.FindViewById<Button>(Resource.Id.btn_maximize);


            // 3. Вешаем перетаскивание (Drag & Drop) на ВСЮ панель целиком
            _panelView.SetOnTouchListener(this); // Не забудь добавить View.IOnTouchListener к классу CaptureService
            _btnRewind.SetOnTouchListener(this);
            _btnRecord.SetOnTouchListener(this);
            _btnPauseRecord.SetOnTouchListener(this);
            _btnStopRecord.SetOnTouchListener(this);
            _btnCancelRecord.SetOnTouchListener(this);
            _btnClosePanel.SetOnTouchListener(this);
            _btnResumeRecord.SetOnTouchListener(this);
            _btnMaximize.SetOnTouchListener(this);



            // 4. Настраиваем клики
            _btnRecord.Click += (s, e) => OnRecordButtonClick();
            _btnStopRecord.Click += (s, e) => OnStopButtonClick();
            _btnPauseRecord.Click += (s, e) => OnPauseButtonClick();
            _btnCancelRecord.Click += (s, e) => OnCancelButtonClick();
            _btnResumeRecord.Click += (s, e) => OnResumeButtonClick();
            _btnRewind.Click += (s, e) => OnRewindButtonClick();
            _btnClosePanel.Click += (s, e) => OnClosePanelButtonClick();
            _btnMaximize.Click += (s, e) => OnMaximizeButtonClick();


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
            )
            {
                Gravity = GravityFlags.Top | GravityFlags.Left,

                X = _dataService.SettingsRepository.Get(SettingKey.FloatingBarX) is Setting x ? int.Parse(x.Value) : 100,
                Y = _dataService.SettingsRepository.Get(SettingKey.FloatingBarY) is Setting y ? int.Parse(y.Value) : 100,
            };

            // Добавляем готовую красивую панель на экран поверх всех приложений
            _windowManager.AddView(_panelView, _params);

            _timerHandler = new Handler(Looper.MainLooper);
            _timerRunnable = new Runnable(OnTimerTick);

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
                    // Двигаем всю панель, за какой бы элемент её ни зацепили
                    _params.X = (int)(_initialX + (e.RawX - _initialTouchX));
                    _params.Y = (int)(_initialY + (e.RawY - _initialTouchY));
                    _windowManager.UpdateViewLayout(_panelView, _params);
                    return true;

                case MotionEventActions.Up:
                    // ВЫЧИСЛЯЕМ: это был просто клик или перетаскивание?
                    float deltaX = System.Math.Abs(e.RawX - _initialTouchX);
                    float deltaY = System.Math.Abs(e.RawY - _initialTouchY);

                    if (deltaX < 10 && deltaY < 10)
                    {
                        // Пользователь просто кликнул (палец почти не сместился).
                        // Если событие пришло от конкретной кнопки, принудительно вызываем её нативный клик!
                        if (v is Button)
                        {
                            v.PerformClick();
                        }
                    }
                    //перетаскивание
                    else
                    {
                        _dataService.SettingsRepository.ChangeValue(SettingKey.FloatingBarX, _params.X.ToString());
                        _dataService.SettingsRepository.ChangeValue(SettingKey.FloatingBarY, _params.Y.ToString());
                    }
                    
                    return true;
            }
            return false;
        }

        private void OnTimerTick()
        {
            _recordingTime = _recordingTime.Add(TimeSpan.FromMilliseconds(1000));
            _tvTimer.Text = _recordingTime.ToString(@"mm\:ss");

            _timerHandler.PostDelayed(_timerRunnable, 1000);
        }


        private void OnRecordingStateChanged(RecordingState oldValue, RecordingState newValue)
        {

            TransitionManager.BeginDelayedTransition(_panelRoot, new AutoTransition().SetDuration(200));

            if (newValue == RecordingState.Initial)
            {

            }
            else if(newValue == RecordingState.Record)
            {
                _llMainGroup.Visibility = ViewStates.Gone;

                _btnResumeRecord.Visibility = ViewStates.Gone;
                _btnPauseRecord.Visibility = ViewStates.Visible;

                _recordingControlsGroup.Visibility = ViewStates.Visible;
                

                _params.Width = WindowManagerLayoutParams.WrapContent;

                StartTimer();
            }
            else if(newValue == RecordingState.Stop)
            {
                _llMainGroup.Visibility = ViewStates.Visible;

                _btnResumeRecord.Visibility = ViewStates.Gone;
                _btnPauseRecord.Visibility = ViewStates.Visible;

                _recordingControlsGroup.Visibility = ViewStates.Gone;

                StopTimer(true); 
            }
            else if(newValue == RecordingState.Pause)
            {
                _llMainGroup.Visibility = ViewStates.Gone;

                _btnResumeRecord.Visibility = ViewStates.Visible;
                _btnPauseRecord.Visibility = ViewStates.Gone;

                _recordingControlsGroup.Visibility = ViewStates.Visible;

                StopTimer(false);
            }

            _windowManager.UpdateViewLayout(_panelView, _params);
        }


        private void OnClosePanelButtonClick()
        {

        }

        private void OnRewindButtonClick()
        {
            SendSystemRewindCommand();
        }

        private void OnRecordButtonClick()
        {
            RecordingState = RecordingState.Record;

            WeakReferenceMessenger.Default.Send(new ToggleRecordMessage(RecordingAction.Start));
        }

        private void OnPauseButtonClick()
        {
            RecordingState = RecordingState.Pause;

            WeakReferenceMessenger.Default.Send(new ToggleRecordMessage(RecordingAction.Pause));
        }

        private void OnStopButtonClick()
        {
            RecordingState = RecordingState.Stop;

            ShowSimpleToast();

            WeakReferenceMessenger.Default.Send(new ToggleRecordMessage(RecordingAction.Stop));
        }

        private void OnCancelButtonClick()
        {
            RecordingState = RecordingState.Stop;

            WeakReferenceMessenger.Default.Send(new ToggleRecordMessage(RecordingAction.Cancel));
        }

        private void OnMaximizeButtonClick()
        {
            var context = Application.ApplicationContext;

            // 2. Указываем класс запуска (это та самая точка, где стартует твой общий проект Avalonia)
            var intent = new Intent(context, typeof(MainActivity));

            // 3. Эти флаги принудительно достают твой общий проект из фона, НЕ перезапуская его
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ReorderToFront | ActivityFlags.SingleTop);

            // 4. Отрезаем любые анимации, чтобы общее окно появилось мгновенно и без дерганий
            intent.AddFlags(ActivityFlags.NoAnimation);

            // 5. Даем команду системе вытащить основной проект на экран
            context.StartActivity(intent);
        }

        private void OnResumeButtonClick()
        {
            RecordingState = RecordingState.Record;

            WeakReferenceMessenger.Default.Send(new ToggleRecordMessage(RecordingAction.Resume));
        }


        private void StartTimer()
        {

            _timerHandler.PostDelayed(_timerRunnable, 1000);
        }

        private void StopTimer(bool reset = true)
        {
            if (reset)
            {
                _tvTimer.Text = "00:00";
                _recordingTime = TimeSpan.Zero;
            }

            _timerHandler.RemoveCallbacks(_timerRunnable);
        }


        public override void OnDestroy()
        {
            base.OnDestroy();

            if (_panelView != null) _windowManager.RemoveView(_panelView);

            StopSelf();
        }

        public override void OnTaskRemoved(Intent? rootIntent)
        {
            if (_panelView != null) _windowManager.RemoveView(_panelView);

            StopSelf();

            base.OnTaskRemoved(rootIntent);
        }


        private void SendSystemRewindCommand()
        {
            var manager = (MediaSessionManager?)GetSystemService(Context.MediaSessionService);
            if (manager == null) return;

            // Передаем компонент нашего сервиса, у которого есть права слушателя уведомлений
            var component = new ComponentName(this, Java.Lang.Class.FromType(typeof(CaptureService)));

            try
            {
                var controllers = manager.GetActiveSessions(component);
                if (controllers != null && controllers.Count > 0)
                {
                    // Берем самый первый активный плеер (например, запущенный YouTube)
                    MediaController activeController = controllers[0];

                    // Получаем текущую позицию ползунка в миллисекундах
                    long currentPosition = activeController.PlaybackState.Position;

                    // Высчитываем новую позицию (минус 10000 миллисекунд = 10 секунд)
                    long targetPosition = System.Math.Max(0, currentPosition - 5000);

                    // Принудительно двигаем ползунок чужого плеера на нужную секунду!
                    activeController.GetTransportControls().SeekTo(targetPosition);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка перемотки сессии: {ex.Message}");
            }
        }


        private void HardStopAndCloseEverything()
        {
            // 1. Если в этот момент шла запись — принудительно выключаем цикл
            _recordingState = RecordingState.Stop;


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

        private void ShowSimpleToast()
        {
            if (_floatingToast == null) return;

            _floatingToast.Visibility = ViewStates.Visible;

            new Handler(Looper.MainLooper).PostDelayed(() =>
            {
                _floatingToast.Visibility = ViewStates.Gone;
            }, 2000);
        }

    }
}
