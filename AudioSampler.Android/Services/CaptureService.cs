using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Media;
using Android.Media.Projection;
using Android.OS;
using Android.Provider;
using AudioSampler.Messages;
using AudioSampler.Model;
using Avalonia.Markup.Xaml.Templates;
using CommunityToolkit.Mvvm.Messaging;
using Java.Nio.FileNio.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Android.Resource;


namespace AudioSampler.Android.Services
{
    [Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaProjection)]
    public class CaptureService : Service
    {
        private MediaProjectionManager? _projectionManager;
        private MediaProjection? _mediaProjection;
        private AudioRecord? _recorder;
        private bool _isRecording = false;
        private TaskCompletionSource<bool> _recordingResultSource;

        public CaptureService()
        {
            // Подписываемся на сообщения из любой точки программы
            WeakReferenceMessenger.Default.Register<ToggleRecordMessage>(this, (r, m) =>
            {
                // m.Value содержит true (если надо писать) или false (если стоп)
                if (m.Value == RecordingAction.Start)
                {
                    StartRecordingAsync();
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
                    StartRecordingAsync();
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


        private async Task StartRecordingAsync()
        {
            if (_isRecording) return;

            _isRecording = true;
            _recordingResultSource = new TaskCompletionSource<bool>();

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
            var bufferSizeInBytes = bufferSize * 4;

            _recorder = new AudioRecord.Builder()
                .SetAudioFormat(audioFormat)
                .SetBufferSizeInBytes(bufferSizeInBytes)
                .SetAudioPlaybackCaptureConfig(config)
                .Build();

            _recorder.StartRecording();

            var folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"Recording{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            var writeTask = Task.Run(() =>
            {
                using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream))
                {
                    // Выделяем 44 байта ПУСТОГО места в самом начале файла под будущий WAV-заголовок
                    byte[] emptyHeader = new byte[44];
                    writer.Write(emptyHeader);

                    byte[] buffer = new byte[bufferSizeInBytes];

                    while (_isRecording)
                    {
                        int read = _recorder.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            // Пишем байты на диск мгновенно порциями, память приложения чиста!
                            writer.Write(buffer, 0, read);
                        }
                    }
                }
            });


            bool recordResult = await _recordingResultSource.Task;

            // Гарантируем, что поток записи успел закрыть файл
            await writeTask;

            if (_recorder != null)
            {
                try
                {
                    _recorder.Stop();
                    _recorder.Release();

                    _recorder = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка остановки: {ex.Message}");
                }
            }


            if (recordResult)
            {
                // Пользователь нажал Стоп — запечатываем WAV-заголовок поверх пустоты
                FinalizeWavFile(file);
            }
            else
            {
                // Пользователь нажал Отмена — просто удаляем временный файл с диска
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

        }

        private void FinalizeWavFile(string file, int sampleRate = 44100, short channels = 2, short bitsPerSample = 16)
        {

            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(fileStream))
            {
                // 1. Высчитываем реальные размеры файла, который записался на диск
                int totalDataLength = (int)fileStream.Length - 44; // Чистый размер звука без заголовка
                int totalAudioLen = (int)fileStream.Length - 8;
                int byteRate = sampleRate * channels * bitsPerSample / 8;
                short blockAlign = (short)(channels * bitsPerSample / 8);

                // 2. Сдвигаем курсор записи в самый ТОП файла (на позицию 0), где лежат пустые 44 байта
                fileStream.Seek(0, SeekOrigin.Begin);

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


                var durationMs = (totalDataLength * 1000) / (sampleRate * channels * (bitsPerSample / 8));
                WeakReferenceMessenger.Default.Send(new RecordFinishedMessage(new RecordResult(file, TimeSpan.FromMilliseconds(durationMs), Path.GetFileNameWithoutExtension(file), fileStream.Length)));
            }

            //CopyFromInnerStorageToPath(file, Path.GetFileName(file));

            // 4. Переименовываем файл из "temp_..." в нормальное красивое имя, если нужно
            //string folderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            //string finalPath = System.IO.Path.Combine(folderPath, $"guitar_sample_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav");

            //System.IO.File.Move(tempPath, finalPath);

        }

        private void CopyFromInnerStorageToPath(string source, string output)
        {
            try
            {
                var values = new ContentValues();
                values.Put(MediaStore.Audio.Media.InterfaceConsts.DisplayName, output);
                values.Put(MediaStore.Audio.Media.InterfaceConsts.MimeType, "audio/wav");
                values.Put(MediaStore.Audio.Media.InterfaceConsts.RelativePath, "Recordings/AudioSampler");

                var uri = ContentResolver.Insert(MediaStore.Audio.Media.GetContentUri("external"), values);
                if (uri == null) return;

                using (var inStream = System.IO.File.OpenRead(source))
                using (var outStream = ContentResolver.OpenOutputStream(uri))
                {
                    inStream.CopyTo(outStream!);
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }


        #region record to list of bytes
        //private void StartRecording()
        //{
        //    _isRecording = true;

        //    var config = new AudioPlaybackCaptureConfiguration.Builder(_mediaProjection!)
        //        .AddMatchingUsage(AudioUsageKind.Media)
        //        .Build();

        //    var sampleRate = 44100;
        //    short channels = 2;
        //    short bitsPerSample = 16;

        //    var audioFormat = new AudioFormat.Builder()
        //        .SetEncoding(Encoding.Pcm16bit)
        //        .SetSampleRate(sampleRate)
        //        .SetChannelMask(ChannelOut.Stereo)
        //        .Build();

        //    var bufferSize = AudioRecord.GetMinBufferSize(sampleRate, ChannelIn.Stereo, Encoding.Pcm16bit);

        //    _recorder = new AudioRecord.Builder()
        //        .SetAudioFormat(audioFormat)
        //        .SetBufferSizeInBytes(bufferSize * 4)
        //        .SetAudioPlaybackCaptureConfig(config)
        //        .Build();

        //    _recorder.StartRecording();

        //    var buffer = new byte[4096];

        //    long totalBytes = 0;

        //    while (_isRecording)
        //    {
        //        int read = _recorder.Read(buffer, 0, buffer.Length);
        //        if (read > 0)
        //        {
        //            var chunk = new byte[read];
        //            System.Array.Copy(buffer, 0, chunk, 0, read);
        //            _audioChunks.Add(chunk);

        //        }
        //    }
        //}

        #endregion

        public async Task StopRecordingAsync()
        {
            _isRecording = false;

            _recordingResultSource.TrySetResult(true);
        }

        public void CancelRecording()
        {
            _isRecording = false;

            _recordingResultSource.TrySetResult(false);
        }


        public void PauseRecording()
        {
            if(_recorder != null)
            {
                _recorder.Stop();
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