using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.Services
{
    public class FileService
    {


        private TopLevel GetTopLevel()
        {
            var lifetime = App.Current.ApplicationLifetime;

            if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            if (lifetime is ISingleViewApplicationLifetime mobile && mobile.MainView != null)
                return TopLevel.GetTopLevel(mobile.MainView);

            throw new InvalidOperationException("Не удалось найти TopLevel. Приложение еще не инициализировано?");
        }

        public async Task<Stream> OpenWriteFolderFileAsync(string fileName, string? customFolderUri = null)
        {
            var storageProvider = GetTopLevel().StorageProvider;
            IStorageFolder? targetFolder = null;

            // 1. Пробуем открыть кастомную папку по URI, если её передали
            if (!string.IsNullOrEmpty(customFolderUri))
            {
                try
                {
                    targetFolder = await storageProvider.TryGetFolderFromPathAsync(new Uri(customFolderUri));
                }
                catch { /* игнорируем, если папка удалена */ }
            }

            // 2. Если кастомной нет, берем системную "Музыку"
            if (targetFolder == null)
            {
                targetFolder = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);
            }

            if (targetFolder == null)
                throw new DirectoryNotFoundException("Доступ к хранилищу отклонен OS.");

            // 3. Создаем файл и отдаем поток наружу
            var file = await targetFolder.CreateFileAsync(fileName);
            if (file == null)
                throw new IOException("Не удалось создать файл.");


            return await file.OpenWriteAsync();
        }


        public async Task<string> GetPathAsync()
        {
            var storageProvider = GetTopLevel().StorageProvider;
            IStorageFolder? targetFolder = null;

            targetFolder = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);

            return targetFolder.Path.AbsolutePath;
        }

        public async Task<string?> RequestCustomFolderAsync()
        {
            var storageProvider = GetTopLevel().StorageProvider;

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выберите папку для экспорта аудио",
                AllowMultiple = false
            });

            return folders.Count > 0 ? folders[0].Path.ToString() : null;
        }

    }
}
