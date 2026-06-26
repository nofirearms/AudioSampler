using ManagedBass;
using ManagedBass.Enc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.Services
{
    public class AudioService
    {
        private string _currentFile;
        private int _playStream;
        private WaveFileWriter _wavWriter;
        private int _recordStream;

        public AudioService()
        {
            Bass.Init();
        }

        /// <summary>
        /// 1. ОТКРЫТЬ АУДИО И ПОЛУЧИТЬ СЭМПЛЫ ДЛЯ ГРАФИКА
        /// </summary>
        public async Task<float[]> LoadAudioAndGetSamplesAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                _currentFile = filePath;
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
        /// 2. ВОСПРОИЗВЕДЕНИЕ С РАЗНЫХ МЕСТ
        /// </summary>
        public void Play(double startSeconds)
        {
            if (string.IsNullOrEmpty(_currentFile)) return;

            // Если что-то уже играет, освобождаем поток
            Stop();

            // Создаем обычный поток для воспроизведения
            _playStream = Bass.CreateStream(_currentFile);
            if (_playStream == 0)
                throw new Exception($"Не удалось создать поток воспроизведения: {Bass.LastError}");

            // Перемотка на нужное место (переводим секунды в байты)
            long positionInBytes = Bass.ChannelSeconds2Bytes(_playStream, startSeconds);
            Bass.ChannelSetPosition(_playStream, positionInBytes);

            // Запуск
            Bass.ChannelPlay(_playStream);
        }

        public void Pause()
        {
            if (_playStream != 0) Bass.ChannelPause(_playStream);
        }

        public void Stop()
        {
            if (_playStream != 0)
            {
                Bass.ChannelStop(_playStream);
                Bass.StreamFree(_playStream);
                _playStream = 0;
            }
        }

        /// <summary>
        /// 3. НОРМАЛИЗАЦИЯ (Чистый C# над массивом float)
        /// Находит самый громкий пик и подтягивает весь массив так, чтобы пик стал равен 1.0 (0 дБ)
        /// </summary>
        public float[] Normalize(float[] samples)
        {
            if (samples == null || samples.Length == 0) return samples;

            // 1. Ищем максимальный пик амплитуды
            float maxPeak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float absValue = Math.Abs(samples[i]);
                if (absValue > maxPeak) maxPeak = absValue;
            }

            // Если файл пустой или уже нормализован
            if (maxPeak == 0f || Math.Abs(maxPeak - 1.0f) < 0.001f) return samples;

            // 2. Вычисляем коэффициент нормализации
            float gain = 1.0f / maxPeak;

            // 3. Умножаем каждый сэмпл на коэффициент
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain;
            }

            return samples;
        }

        /// <summary>
        /// 4. ОБРЕЗКА И ЭКСПОРТ (Из текущего открытого файла)
        /// </summary>
        public async Task TrimAndSaveAsync(string outputPath, double startSeconds, double endSeconds, bool toMp3 = true)
        {
            if (string.IsNullOrEmpty(_currentFile)) throw new Exception("Нет открытого файла для обрезки");

            await Task.Run(() =>
            {
                int decodeStream = Bass.CreateStream(_currentFile, 0, 0, BassFlags.Decode);
                if (decodeStream == 0) throw new Exception("Ошибка обрезки");

                try
                {
                    long startBytes = Bass.ChannelSeconds2Bytes(decodeStream, startSeconds);
                    long endBytes = Bass.ChannelSeconds2Bytes(decodeStream, endSeconds);
                    long bytesToRead = endBytes - startBytes;

                    Bass.ChannelSetPosition(decodeStream, startBytes);

                    if (toMp3)
                    {
                        // === ЛОГИКА ДЛЯ MP3 ===
                        int encoder = BassEnc_Mp3.Start(decodeStream, "-b 192", EncodeFlags.Default, outputPath);
                        if (encoder == 0) throw new Exception("Не удалось запустить кодировщик MP3");

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
                    else
                    {
                        // === ЛОГИКА ДЛЯ WAV ===
                        Bass.ChannelGetInfo(decodeStream, out ChannelInfo info);

                        int bitsPerSample = info.Flags.HasFlag(BassFlags.Float) ? 32 : 16;
                        var format = new WaveFormat(info.Frequency, bitsPerSample, info.Channels);

                        using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        using (var waveWriter = new WaveFileWriter(fs, format))
                        {
                            byte[] buf = new byte[20480];
                            long totalBytesRead1 = 0;

                            while (totalBytesRead1 < bytesToRead)
                            {
                                int toReadNow = (int)Math.Min(buf.Length, bytesToRead - totalBytesRead1);
                                int read = Bass.ChannelGetData(decodeStream, buf, toReadNow);
                                if (read <= 0) break;

                                waveWriter.Write(buf, read);
                                totalBytesRead1 += read;
                            }
                        } // Здесь waveWriter закроется сам и допишет размеры
                    }
                }
                finally
                {
                    Bass.StreamFree(decodeStream);
                }
            });
        }


        /// <summary>
        /// НАЧАТЬ ЗАПИСЬ В WAV
        /// </summary>
        /// <param name="outputPath">Путь к файлу, например: Path.Combine(AppContext.BaseDirectory, "record.wav")</param>
        public void StartRecordingWav(string outputPath)
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
            _recordStream = Bass.RecordStart(44100, 2, BassFlags.Default, RecordCallback, IntPtr.Zero);

            if (_recordStream == 0)
            {
                _wavWriter.Dispose();
                _wavWriter = null;
                throw new Exception($"Не удалось запустить запись: {Bass.LastError}");
            }
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
        public void StopRecordingWav()
        {
            // 1. Останавливаем нативный поток захвата звука
            if (_recordStream != 0)
            {
                Bass.ChannelStop(_recordStream);
                _recordStream = 0;
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


        public void Dispose()
        {
            Stop();
            Bass.Free();
        }
    }
}
