using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Media;
using Android.Media.Projection;
using Android.OS;
using AudioSampler.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Android.Resource;
using AudioSampler.Model;

namespace AudioSampler.Android.Services
{
    [Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaProjection)]
    public class CaptureService : Service
    {
        private MediaProjectionManager? _projectionManager;
        private MediaProjection? _mediaProjection;
        private AudioRecord? _recorder;
        private bool _isRecording = false;

        private List<byte[]> _audioChunks = new List<byte[]>();


        public CaptureService()
        {
            // Подписываемся на сообщения из любой точки программы
            WeakReferenceMessenger.Default.Register<ToggleRecordMessage>(this, (r, m) =>
            {
                // m.Value содержит true (если надо писать) или false (если стоп)
                if (m.Value == RecordingAction.Start)
                {
                    Task.Run(() => StartRecording());
                }
                else if (m.Value == RecordingAction.Stop) 
                {
                    Task.Run(async() => await StopRecordingAsync());
                }
                else if(m.Value == RecordingAction.Pause)
                {
                    PauseRecording();
                }
                else if(m.Value == RecordingAction.Cancel)
                {
                    CancelRecording();
                }
                else if(m.Value == RecordingAction.Resume)
                {
                    Task.Run(() => StartRecording());
                }
            });


            // Регистрируем слушателя команды от крестика плавающей панели
            WeakReferenceMessenger.Default.Register<HardStopSharingMessage>(this, (r, m) =>
            {
                StopCaptureSession();
            });
        }



        public override IBinder? OnBind(Intent? intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();
            var channelId = "capture_channel";
            var manager = (NotificationManager)GetSystemService(NotificationService);
            var channel = new NotificationChannel(channelId, "Audio Capture", NotificationImportance.Low);
            manager.CreateNotificationChannel(channel);
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (intent?.Action == "ACTION_START_CAPTURE")
            {
                int resultCode = intent.GetIntExtra("RESULT_CODE", 0);
                var data = (Intent?)intent.GetParcelableExtra("DATA", Java.Lang.Class.FromType(typeof(Intent)));

                if (data != null)
                {
                    // 1. СТРОГО СНАЧАЛА включаем Foreground режим службы с уведомлением
                    StartMyForegroundNotification();

                    // 2. ТЕПЕРЬ Android видит, что мы в Foreground, и разрешает создать проекцию без вылетов!
                    _projectionManager = (MediaProjectionManager?)GetSystemService(Context.MediaProjectionService);
                    if (_projectionManager != null)
                    {
                        _mediaProjection = _projectionManager.GetMediaProjection(resultCode, data);

                        if (_mediaProjection != null)
                        {
                            // Регистрируем наш системный колбэк остановки
                            _mediaProjection.RegisterCallback(new AudioProjectionCallback(), new Handler(Looper.MainLooper));

                            // 3. И вот теперь, когда всё успешно завелось, запускаем плавающую панель!
                            Intent panelIntent = new Intent(this, typeof(FloatingButtonService));
                            StartService(panelIntent);
                        }
                    }
                }
            }

            return StartCommandResult.Sticky;
        }


        private void StartMyForegroundNotification()
        {
            var notification = new Notification.Builder(this, "capture_channel")
                .SetContentTitle("Audio Capture")
                .SetContentText("Audio Capture")
                .SetSmallIcon(Drawable.IcMediaPlay)
                .Build();

            StartForeground(1, notification);
        }


        private void StartRecording()
        {
            _isRecording = true;

            var config = new AudioPlaybackCaptureConfiguration.Builder(_mediaProjection!)
                .AddMatchingUsage(AudioUsageKind.Media)
                .Build();

            var sampleRate = 44100;
            short channels = 2;
            short bitsPerSample = 16;

            var audioFormat = new AudioFormat.Builder()
                .SetEncoding(Encoding.Pcm16bit)
                .SetSampleRate(sampleRate)
                .SetChannelMask(ChannelOut.Stereo)
                .Build();

            var bufferSize = AudioRecord.GetMinBufferSize(sampleRate, ChannelIn.Stereo, Encoding.Pcm16bit);

            _recorder = new AudioRecord.Builder()
                .SetAudioFormat(audioFormat)
                .SetBufferSizeInBytes(bufferSize * 4)
                .SetAudioPlaybackCaptureConfig(config)
                .Build();

            _recorder.StartRecording();

            var buffer = new byte[4096];

            long totalBytes = 0;

            while (_isRecording)
            {
                int read = _recorder.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    var chunk = new byte[read];
                    System.Array.Copy(buffer, 0, chunk, 0, read);
                    _audioChunks.Add(chunk);

                }
            }
        }

        public async Task StopRecordingAsync()
        {

            _isRecording = false;

            if(_recorder != null)
            {
                _recorder.Stop();
                _recorder.Release();

                _recorder = null;

                await Task.Run(() => RenderAudio());
            }
            _audioChunks.Clear();
        }

        public void PauseRecording()
        {
            _isRecording = false;

            if(_recorder != null)
            {
                _recorder.Stop();
                _recorder.Release();

                _recorder = null;
            }
        }

        public void CancelRecording()
        {
            _isRecording = false;

            if (_recorder != null)
            {
                _recorder.Stop();
                _recorder.Release();

                _recorder = null;

            }

            _audioChunks.Clear();
        }

        public byte[] GetSoundData(List<byte[]> chunks)
        {
            // Объединяем все чанки в один массив
            int totalSize = chunks.Sum(chunk => chunk.Length);
            byte[] result = new byte[totalSize];

            int offset = 0;
            foreach (byte[] chunk in chunks)
            {
                Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }
            return result;
        }

        private void RenderAudio(int sampleRate = 44100, short channels = 2, short bitsPerSample = 16)
        {
            var now = DateTime.Now;
            var name = $"Recording{now:ddMMyyyy_hhmmss}.wav";
            var path = Path.Combine(global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads)!.AbsolutePath, name);

            var pcmData = GetSoundData(_audioChunks);

            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fileStream))
            {
                var totalDataLength = pcmData.Length;
                int totalAudioLen = totalDataLength + 36;

                int byteRate = sampleRate * channels * bitsPerSample / 8;
                short blockAlign = (short)(channels * bitsPerSample / 8);

                // 2. Пишем RIFF заголовок (всего 44 байта)
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); // Чанк RIFF
                writer.Write(totalAudioLen);                               // Размер всего файла минус 8 байт
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE")); // Формат WAVE

                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); // Подчанк параметров звука
                writer.Write(16);                                          // Размер этого подчанка (16 для PCM)
                writer.Write((short)1);                                    // Аудио формат (1 - PCM без сжатия)
                writer.Write(channels);                                    // Количество каналов
                writer.Write(sampleRate);                                  // Частота дискретизации
                writer.Write(byteRate);                                    // Количество байт в секунду
                writer.Write(blockAlign);                                  // Выравнивание блока данных
                writer.Write(bitsPerSample);                               //

                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); // Подчанк самих данных
                writer.Write(totalDataLength);                                // Размер только аудио данных

                // 3. Дописываем твой сырой массив байт после заголовка
                writer.Write(pcmData);
            }

        }


        private void StopCaptureSession()
        {
            CancelRecording();

            // 3. САМОЕ ГЛАВНОЕ: глушим системный шеринг (убирает значок из шторки)
            if (_mediaProjection != null)
            {
                _mediaProjection.Stop();
                _mediaProjection = null;
            }

            // 4. Выключаем режим Foreground и полностью выгружаем сервис из памяти
            StopForeground(StopForegroundFlags.Remove);

            StopSelf();
        }
    }
}