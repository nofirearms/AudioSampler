using AudioSampler.Model;
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
        private readonly DataService _dataService;

        public FileService(DataService dataService)
        {
            _dataService = dataService;
        }

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


        public async Task<string> GetDefaultPathAsync()
        {
            var storageProvider = GetTopLevel().StorageProvider;
            IStorageFolder? targetFolder = null;

            targetFolder = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);

            return targetFolder.Path.AbsolutePath;
        }

        public async Task<FolderBookmark> RequestFolderBookmarkAsync(bool saveFolder = false)
        {
            var storageProvider = GetTopLevel().StorageProvider;

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Export folder",
                AllowMultiple = false
            });

            if(folders.Count > 0)
            {
                var selectedFolder = folders[0];

                var folderBookmark = new FolderBookmark();

                if (selectedFolder.CanBookmark)
                {
                    folderBookmark.Bookmark = await selectedFolder.SaveBookmarkAsync();                   
                }
                if (saveFolder)
                {
                    await _dataService.FolderBooksmarksReposity.CreateAsync(folderBookmark);
                }

                return folderBookmark;


            }
            return null;
        }


        public async Task<IStorageFolder?> GetStorageFolderFromFolderBookmarkAsync(FolderBookmark bookmark)
        {
            var topLevel = GetTopLevel();

            // Если это дефолтная папка по умолчанию
            if (bookmark.Bookmark == "DEFAULT")
            {
                // Получаем доступ к системной папке Downloads через Avalonia StorageProvider
                // WellKnownFolder.Downloads поддерживается в Avalonia для Android
                return await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);
            }
            else
            {
                // Если это кастомный букмарк из базы
                return await topLevel.StorageProvider.OpenFolderBookmarkAsync(bookmark.Bookmark);
            }
            
        }


        public async Task GetFolderFromDatabase()
        {
            string savedBookmark = "";//GetFolderBookmarkFromSettings();

            if (!string.IsNullOrEmpty(savedBookmark))
            {
                var topLevel = GetTopLevel();

                // Возвращаем объект папки по её закладке. ОС вспомнит, что права у нас есть.
                IStorageFolder? restoredFolder = await topLevel.StorageProvider.OpenFolderBookmarkAsync(savedBookmark);

                if (restoredFolder != null)
                {
                    // Папка готова к работе, права активны!
                }
            }
        }
    }
}
