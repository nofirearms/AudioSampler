using AudioSampler.Model;
using Avalonia.Platform.Storage;
using ManagedBass;
using ManagedBass.Enc;
using ManagedBass.Fx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.Services
{
    public class AudioService
    {
        private WaveFileWriter _wavWriter;

        public AudioService()
        {
            Bass.Init();
        }

        //--------------------------------------------------------------- UTILITY --------------------------------------------------

        /// <summary>
        /// Получаем сэмплы из файла для графика и нормализации, спользуется при воспроизведении
        /// </summary>
        public async Task<float[]> GetSamplesAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                // Открываем файл в режиме Decode, чтобы просто вытащить данные
                int decodeStream = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Float);
                if (decodeStream == 0)
                    throw new Exception($"Не удалось открыть файл для декодирования: {Bass.LastError}");

                try
                {
                    long lengthInBytes = Bass.ChannelGetLength(decodeStream);
                    if (lengthInBytes <= 0) return Array.Empty<float>();

                    // Так как мы указали флаг BassFlags.Float, BASS сам конвертирует 
                    // аудио (даже MP3) в массив float-сэмплов (от -1.0 до 1.0)
                    int floatCount = (int)(lengthInBytes / 4);
                    float[] samples = new float[floatCount];

                    // Читаем все сэмплы из потока в наш массив
                    int bytesRead = Bass.ChannelGetData(decodeStream, samples, (int)lengthInBytes);

                    return samples;
                }
                finally
                {
                    Bass.StreamFree(decodeStream);
                }
            });
        }

        /// <summary>
        /// Получаем сэмплы из уже открытого стрима
        /// </summary>
        /// <param name="streamHandle"></param>
        /// <returns></returns>
        public async Task<float[]> GetSamplesFromStreamAsync(int streamHandle)
        {
            return await Task.Run(() =>
            {
                long lengthInBytes = Bass.ChannelGetLength(streamHandle);
                if (lengthInBytes <= 0) return Array.Empty<float>();

                // Так как мы указали флаг BassFlags.Float, BASS сам конвертирует 
                // аудио (даже MP3) в массив float-сэмплов (от -1.0 до 1.0)
                int floatCount = (int)(lengthInBytes / 4);
                float[] samples = new float[floatCount];

                // Читаем все сэмплы из потока в наш массив
                int bytesRead = Bass.ChannelGetData(streamHandle, samples, (int)lengthInBytes);

                return samples;
            });
        }

        /// <summary>
        /// Рэндерим точки для графа 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="samplesCount"></param>
        /// <returns></returns>
        public async Task<float[]> RenderWaveformAsync(string filePath, int samplesCount)
        {
            return await Task.Run(async () =>
            {
                var samples = await GetSamplesAsync(filePath);

                var samplesPerPoint = samples.Length / samplesCount;

                var output = new float[samplesCount];

                for (int i = 0; i < samplesCount; i++)
                {
                    var position = i * samplesPerPoint;
                    output[i] = samples.Skip(position).Take(samplesPerPoint).Max();
                }

                return output;
            });
        }

        /// <summary>
        /// Получаем ID стрима
        /// </summary>
        /// <returns>ID стрима</returns>
        public int CreatePlaybackStream(string path)
        {
            return Bass.CreateStream(path, 0, 0, BassFlags.Default);
        }

        /// <summary>
        /// Определить длину стрима в секундах
        /// </summary>
        public double GetLengthSeconds(int stream) 
        {
            // 1. Получаем длину в байтах
            long lengthInBytes = Bass.ChannelGetLength(stream);

            // 2. Переводим байты в секунды
            double lengthInSeconds = Bass.ChannelBytes2Seconds(stream, lengthInBytes);

            return lengthInSeconds; 
        }

        public long GetLengthBytes(int stream)
        {
            return Bass.ChannelGetLength(stream);
        }

        /// <summary>
        /// Максимальная амплитуда для нормализации
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        public double GetMaxPeak(float[] samples)
        {
            return samples.Max(Math.Abs);
        }

        /// <summary>
        /// Нормализации, испльзуется только для воспроизведения аудио
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="maxPeak"></param>
        public void Normalize(int stream, double maxPeak)
        {
            // 2. Считаем коэффициент (на сколько всё умножить, чтоб дотянуть до 1.0)
            double gain = 1.0d / maxPeak;

            // 3. Крутим громкость для плеера на лету
            Bass.ChannelSetAttribute(stream, ChannelAttribute.Volume, gain);

        }

        /// <summary>
        /// Нормализация всего стрима для экспорта
        /// </summary>
        public async Task NormalizeAsync(int stream)
        {
            var samples = await GetSamplesFromStreamAsync(stream);
            var maxPeak = GetMaxPeak(samples);

            if (maxPeak <= 0) return; // Защита от тишины
            var gain = 1.0f / (float)maxPeak;


            var volumeParam = new VolumeParameters
            {
                fTarget = gain,                    // Наш коэффициент нормализации
                lChannel = FXChannelFlags.All,     // Применить ко всем каналам (стерео/моно)
                fCurrent = gain,                   // Текущая громкость (ставим такую же, чтоб без плавного перехода)
                fTime = 0f                         // Время перехода (0 - мгновенно)
            };
            var fxHandle = Bass.ChannelSetFX(stream, EffectType.Volume, 0);
            Bass.FXSetParameters(fxHandle, volumeParam);
        }


        /// <summary>
        /// Нормализация выбранного участка стрима для экспорта
        /// </summary>
        public async Task NormalizeRangeAsync(int decodeStream, long startBytes, long bytesToRead)
        {
            // 1. Прыгаем в начало выделенного участка
            Bass.ChannelSetPosition(decodeStream, startBytes);

            // 2. Выделяем буфер под размер участка (или читаем чанками, если участок огромный)
            // Для примера считаем, что выделенный кусок поместится в память для анализа пиков:
            float[] sampleBuffer = new float[bytesToRead / sizeof(float)];

            // Читаем ровно bytesToRead данных из стрима в наш массив float
            int bytesRead = Bass.ChannelGetData(decodeStream, sampleBuffer, (int)bytesToRead);

            // 3. Считаем пик только по прочитанному буферу
            float maxPeak = 0f;
            for (int i = 0; i < bytesRead / sizeof(float); i++)
            {
                float absValue = Math.Abs(sampleBuffer[i]);
                if (absValue > maxPeak) maxPeak = absValue;
            }

            if (maxPeak <= 0) return; // Тишина
            float gain = 1f / maxPeak;

            // 4. Навешиваем наш рабочий Volume FX
            var volumeEffect = Bass.ChannelSetFX(decodeStream, EffectType.Volume, 0);
            var volumeParams = new VolumeParameters
            {
                fTarget = gain,
                lChannel = FXChannelFlags.All,
                fCurrent = gain,
                fTime = 0f
            };
            Bass.FXSetParameters(volumeEffect, volumeParams);

            // 5. КРИТИЧЕСКИ ВАЖНО: Возвращаем позицию стрима обратно в начало участка!
            // Так как ChannelGetData сдвинул "курсор" стрима к концу участка.
            Bass.ChannelSetPosition(decodeStream, startBytes);
        }

        /// <summary>
        /// Определяем текущее положение воспроизведения в секундах
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="endSeconds"></param>
        /// <returns></returns>
        public double CheckPlaybackPosition(int stream, double endSeconds)
        {
            // Получаем текущую секунду, которая играет прямо сейчас
            long currentBytes = Bass.ChannelGetPosition(stream);
            double currentSeconds = Bass.ChannelBytes2Seconds(stream, currentBytes);

            return currentSeconds;
        }


        /// <summary>
        /// Экспорт аудио
        /// </summary>
        public async Task<Result<IStorageFile>> RenderToFileAsync(string inputPath, IStorageFolder outputFolder, string outputName, double startSeconds, double endSeconds, bool normalize, ExportFormat exportFormat = ExportFormat.wav)
        {
            //if (string.IsNullOrEmpty(outputPath)) throw new Exception("Нет открытого файла для обрезки");

            return await Task.Run(async () => 
            {
                int decodeStream = Bass.CreateStream(inputPath, 0, 0, BassFlags.Decode | BassFlags.Float);
                if (decodeStream == 0) throw new Exception("Ошибка обрезки");


                try
                {
                    long startBytes = Bass.ChannelSeconds2Bytes(decodeStream, startSeconds);
                    long endBytes = Bass.ChannelSeconds2Bytes(decodeStream, endSeconds);
                    long bytesToRead = endBytes - startBytes;

                    //временная папка для энкода, из неё потом копируем в нужную
                    var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "cache");
                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                    }

                    string tempOutputPath = Path.Combine(cacheDir, outputName);


                    if (normalize)
                    {
                        await NormalizeRangeAsync(decodeStream, startBytes, bytesToRead);
                    }
                    else
                    {
                        Bass.ChannelSetPosition(decodeStream, startBytes);
                    }

                    if (exportFormat == ExportFormat.mp3)
                    {

                        int encoder = BassEnc_Mp3.Start(decodeStream, "-b 320", EncodeFlags.Default, tempOutputPath);
                        if (encoder == 0)
                        {
                            var errorCode = Bass.LastError;
                            throw new Exception($"BASS Error: {errorCode}");
                        }

                        byte[] buffer = new byte[20480];
                        long totalBytesRead = 0;

                        // Для MP3 нам нужно прогонять данные через Bass.ChannelGetData,
                        // чтобы они улетали в нативный энкодер libbassenc_mp3.so
                        while (totalBytesRead < bytesToRead)
                        {
                            int toReadNow = (int)Math.Min(buffer.Length, bytesToRead - totalBytesRead);
                            int read = Bass.ChannelGetData(decodeStream, buffer, toReadNow);
                            if (read <= 0) break;

                            totalBytesRead += read;
                        }

                        // Обязательно тушим энкодер после цикла, чтобы файл корректно закрылся
                        BassEnc.EncodeStop(encoder);

                    }
                    else if (exportFormat == ExportFormat.wav)
                    {
                        // === ЛОГИКА ДЛЯ WAV ===
                        Bass.ChannelGetInfo(decodeStream, out ChannelInfo info);

                        int bitsPerSample = info.Flags.HasFlag(BassFlags.Float) ? 32 : 16;
                        var format = new WaveFormat(info.Frequency, bitsPerSample, info.Channels);


                        var encoder = BassEnc.EncodeStart(decodeStream, tempOutputPath, EncodeFlags.PCM | EncodeFlags.ConvertFloatTo16BitInt, null);
                        if (encoder == 0) return Result<IStorageFile>.Fail("Encoder handler is 0");

                        byte[] buf = new byte[20480];
                        long totalBytesRead1 = 0;

                        while (totalBytesRead1 < bytesToRead)
                        {
                            int toReadNow = (int)Math.Min(buf.Length, bytesToRead - totalBytesRead1);
                            int read = Bass.ChannelGetData(decodeStream, buf, toReadNow);
                            if (read <= 0) break;

                            totalBytesRead1 += read;
                        }

                        BassEnc.EncodeStop(encoder);

                        //using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        //using (var waveWriter = new WaveFileWriter(fs, format))
                        //{
                        //    byte[] buf = new byte[20480];
                        //    long totalBytesRead1 = 0;

                        //    while (totalBytesRead1 < bytesToRead)
                        //    {
                        //        int toReadNow = (int)Math.Min(buf.Length, bytesToRead - totalBytesRead1);
                        //        int read = Bass.ChannelGetData(decodeStream, buf, toReadNow);
                        //        if (read <= 0) break;

                        //        waveWriter.Write(buf, read);
                        //        totalBytesRead1 += read;
                        //    }
                        //} // Здесь waveWriter закроется сам и допишет размеры
                    }

                    //ПОСЛЕ ЭНКОДА КОПИРУЕМ В ТАРГЕТНУЮ ПАПКУ
                    // Создаем файл там, куда пользователь разрешил доступ
                    IStorageFile? userFile = await outputFolder.CreateFileAsync($"{outputName}.{exportFormat.ToString()}");

                    if (userFile != null)
                    {
                        // Перекачиваем байты через стандартные стримы C#
                        using (var sourceStream = File.OpenRead(tempOutputPath))
                        using (var destStream = await userFile.OpenWriteAsync())
                        {
                            await sourceStream.CopyToAsync(destStream);
                        }

                        // Подчищаем за собой кэш, чтобы не забивать память телефона
                        File.Delete(tempOutputPath);

                    }
                    else
                    {
                        
                    }

                    return Result<IStorageFile>.Ok(userFile);
                }
                catch (Exception ex)
                {
                    return Result<IStorageFile>.Fail(ex.Message);
                }
                finally
                {
                    Bass.StreamFree(decodeStream);
                }
            });
        }




        //--------------------------------------------------------- PLAYBACK ------------------------------------------------------

        #region PLAYBACK
        /// <summary>
        /// 2. ВОСПРОИЗВЕДЕНИЕ С РАЗНЫХ МЕСТ
        /// </summary>
        public void Play(int stream, double startSeconds, double endSeconds)
        {
            Stop(stream);

            if (stream == 0) throw new Exception($"Не удалось создать поток воспроизведения: {Bass.LastError}");


            // Перемотка на нужное место (переводим секунды в байты)
            long startBytes = Bass.ChannelSeconds2Bytes(stream, startSeconds);
            Bass.ChannelSetPosition(stream, startBytes);

            long endBytes = Bass.ChannelSeconds2Bytes(stream, endSeconds);

            //Bass.ChannelSetSync(stream, SyncFlags.Position, endBytes, (handle, channel, data, user) =>
            //{
            //    Bass.ChannelStop(channel); // Мягко останавливаем
            //                               // Тут можно оповестить UI, что воспроизведение закончено
            //});

            // Запуск
            Bass.ChannelPlay(stream);
        }


        public void Pause(int stream)
        {
            if (stream != 0) Bass.ChannelPause(stream);
        }

        public void Stop(int stream)
        {
            Bass.ChannelStop(stream);
        }

        #endregion

        //-------------------------------------------------------- RECORDING ---------------------------------------------------------------

        #region RECORDING

        /// <summary>
        /// НАЧАТЬ ЗАПИСЬ В WAV
        /// </summary>
        /// <param name="outputPath">Путь к файлу, например: Path.Combine(AppContext.BaseDirectory, "record.wav")</param>
        public int StartRecordingWav(string outputPath)
        {
            // На случай, если забыли вызвать Bass.Init() в Program.cs / MainActivity.cs
            Bass.Init();

            // 1. Инициализируем микрофон по умолчанию (-1)
            if (!Bass.RecordInit(-1))
                throw new Exception($"Не удалось включить микрофон: {Bass.LastError}");

            // 2. Создаем формат: 44100 Гц, 16 бит, Стерео (стандартное CD-качество)
            var format = new WaveFormat(44100, 16, 2);

            // 3. Открываем файл на запись и создаем WaveFileWriter
            var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            _wavWriter = new WaveFileWriter(fileStream, format);

            // 4. Запускаем захват звука с микрофона.
            // Передаем частоту (44100), каналы (2) и наш метод-коллбэк RecordCallback
            var recordStream = Bass.RecordStart(44100, 2, BassFlags.Default, RecordCallback, IntPtr.Zero);

            if (recordStream == 0)
            {
                _wavWriter.Dispose();
                _wavWriter = null;
                return 0;
            }

            return recordStream;
        }

        /// <summary>
        /// КОЛЛБЭК: Сюда BASS каждые несколько миллисекунд скидывает сырые PCM-байты из микрофона
        /// </summary>
        private bool RecordCallback(int handle, IntPtr buffer, int length, IntPtr user)
        {
            // Если запись активна и прилетели валидные данные
            if (_wavWriter != null && buffer != IntPtr.Zero && length > 0)
            {
                // Выделяем C# массив под размер прилетевших байт
                byte[] managedBuffer = new byte[length];

                // Копируем байты из нативной памяти (IntPtr) в наш массив (byte[])
                Marshal.Copy(buffer, managedBuffer, 0, length);

                // Записываем PCM-данные в WAV
                _wavWriter.Write(managedBuffer, length);
            }

            // Возвращаем true, чтобы BASS продолжал слушать микрофон. 
            // Если вернуть false, запись внутри BASS остановится.
            return true;
        }

        /// <summary>
        /// ОСТАНОВИТЬ ЗАПИСЬ И СОХРАНИТЬ ФАЙЛ
        /// </summary>
        public void StopRecordingWav(int recordStream)
        {
            // 1. Останавливаем нативный поток захвата звука
            if (recordStream != 0)
            {
                Bass.ChannelStop(recordStream);
                recordStream = 0;
            }

            // 2. Закрываем наш WaveFileWriter
            if (_wavWriter != null)
            {
                // Метод Dispose автоматически запишет правильный размер данных 
                // в 44-байтный WAV-заголовок (в начало файла) и закроет FileStream
                _wavWriter.Dispose();
                _wavWriter = null;
            }

            // Освобождаем микрофон
            Bass.RecordFree();
        }

        #endregion

        //------------------------------------------------------------------------------------------------------------------------------------

        public void Dispose(int stream)
        {
            Stop(stream);
            Bass.StreamFree(stream);
            Bass.Free();
        }
    }
}
