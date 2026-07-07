using AudioSampler.Model;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
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

        public TopLevel GetTopLevel()
        {
            var lifetime = App.Current.ApplicationLifetime;

            if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            if (lifetime is IActivityApplicationLifetime mobile && mobile != null)
            {
                return TopLevel.GetTopLevel(App.Current.AndroidRootView);
            }
                

            throw new InvalidOperationException("Не удалось найти TopLevel. Приложение еще не инициализировано?");
        }

        public async Task<Stream> OpenWriteFolderFileAsync(string fileName, string? customFolderUri = null)
        {
            var storageProvider = GetTopLevel().StorageProvider;
            IStorageFolder? targetFolder = null;

            if (!string.IsNullOrEmpty(customFolderUri))
            {
                try
                {
                    targetFolder = await storageProvider.TryGetFolderFromPathAsync(new Uri(customFolderUri));
                }
                catch {  }
            }

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
                    await _dataService.FolderBookmarksReposity.CreateAsync(folderBookmark);
                }

                return folderBookmark;


            }
            return null;
        }


        public async Task<IStorageFolder?> GetStorageFolderFromFolderBookmarkAsync(FolderBookmark bookmark)
        {
            if (bookmark is null) return null;

            var topLevel = GetTopLevel();

            //не используется
            if (bookmark.Bookmark == "DEFAULT")
            {
                return await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            }
            else
            {
                return await topLevel.StorageProvider.OpenFolderBookmarkAsync(bookmark.Bookmark);
            }
            
        }


        public async Task GetFolderFromDatabase()
        {
            string savedBookmark = "";//GetFolderBookmarkFromSettings();

            if (!string.IsNullOrEmpty(savedBookmark))
            {
                var topLevel = GetTopLevel();

                IStorageFolder? restoredFolder = await topLevel.StorageProvider.OpenFolderBookmarkAsync(savedBookmark);

                if (restoredFolder != null)
                {

                }
            }
        }

        public void DeleteFileByPath(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
