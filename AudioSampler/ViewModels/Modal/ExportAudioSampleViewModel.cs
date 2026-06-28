using AudioSampler.Model;
using AudioSampler.Services;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace AudioSampler.ViewModels.Modal
{
    public partial class ExportAudioSampleViewModel : BaseModalViewModel<ExportSettings>
    {
        private readonly FileService _fileService;
        private readonly DataService _dataService;

        [ObservableProperty]
        private IStorageFolder _folder;

        [ObservableProperty]
        private string _fileName;

        [ObservableProperty]
        private ObservableCollection<FolderBookmarkListItem> _folders = new();

        [ObservableProperty]
        private FolderBookmarkListItem _selectedFolder;

        partial void OnSelectedFolderChanged(FolderBookmarkListItem? oldValue, FolderBookmarkListItem newValue)
        {
            if (newValue is null) return;
            Folder = newValue.Storage;
            _bookmark = newValue.Bookmark;
        }


        public List<ExportFormat> Formats => Enum.GetValues(typeof(ExportFormat)).Cast<ExportFormat>().ToList();

        [ObservableProperty]
        private ExportFormat _format;

        [ObservableProperty]
        private bool _trim;

        [ObservableProperty]
        private bool _normalize;

        private FolderBookmark _bookmark;

        public ExportAudioSampleViewModel(DataService dataService, FileService fileService, ExportSettings exportSettings)
        {
            _fileService = fileService;
            _dataService = dataService;

            _folder = exportSettings.Folder;
            _fileName = exportSettings.Name;
            _format = exportSettings.Format;
            _trim = exportSettings.Trim;
            _normalize = exportSettings.Normalize;
            _bookmark = exportSettings.FolderBookmark;

            LoadData();
        }

        private async Task LoadData()
        {
            var bookmarks = _dataService.FolderBooksmarksReposity.GetAll();
            foreach(var bookmark in bookmarks)
            {
                var storage = await _fileService.GetStorageFolderFromFolderBookmarkAsync(bookmark);
                var item = new FolderBookmarkListItem
                {
                    Storage = storage,
                    Bookmark = bookmark
                };
                Folders.Add(item);

                if(bookmark.Bookmark == _bookmark.Bookmark)
                {
                    SelectedFolder = item;
                }
            }
        }

        [RelayCommand]
        public async void SelectExportFolder()
        {
            var result = await _fileService.RequestFolderBookmarkAsync(false);
            if(result != null)
            {
                _bookmark = result;
                Folder = await _fileService.GetStorageFolderFromFolderBookmarkAsync(_bookmark);
            }
        }

        [RelayCommand]
        public async void AddExportFolder()
        {
            var result = await _fileService.RequestFolderBookmarkAsync(false);
            if (result != null)
            {
                _bookmark = result;
                var storageFolder = await _fileService.GetStorageFolderFromFolderBookmarkAsync(_bookmark);
                await _dataService.FolderBooksmarksReposity.CreateAsync(_bookmark);
                var item = new FolderBookmarkListItem { Bookmark = _bookmark, Storage = storageFolder};
                Folders.Add(item);
                SelectedFolder = item;
            }
        }

        [RelayCommand]
        public void Export()
        {
            var export = new ExportSettings
            {
                Format = Format,
                Name = FileName,
                Normalize = Normalize,
                Folder = Folder,
                FolderBookmark = _bookmark,
                Trim = Trim
            };

            Close(true, export);
        }

        [RelayCommand]
        public async void RemoveFolder()
        {
            if (SelectedFolder is null) return;

            if (SelectedFolder.Bookmark.Bookmark == "DEFAULT") return;

            await _dataService.FolderBooksmarksReposity.RemoveAsync(SelectedFolder.Bookmark);

            Folders.Remove(SelectedFolder);
            
        }
    }

    public class FolderBookmarkListItem
    {
        public IStorageFolder Storage { get; set; }
        public FolderBookmark Bookmark { get; set; }

        public string Path => Storage.Path.LocalPath; //Uri.UnescapeDataString(Storage.Path.AbsolutePath); 
    }
}
