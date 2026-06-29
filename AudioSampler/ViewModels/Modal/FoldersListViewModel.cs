using AudioSampler.Model;
using AudioSampler.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.ViewModels.Modal
{
    public partial class FoldersListViewModel : BaseModalViewModel<FolderBookmarkListItem>
    {
        private readonly DataService _dataService;
        private readonly FileService _fileService;

        private readonly FolderBookmark _exportFolderBookmark;

        [ObservableProperty]
        private ObservableCollection<FolderBookmarkListItem> _folders = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveFolderCommand))]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private FolderBookmarkListItem _selectedFolder;


        public FoldersListViewModel(DataService dataService, FileService fileService, FolderBookmark exportFolderBookmark)
        {
            _dataService = dataService;
            _fileService = fileService;

            _exportFolderBookmark = exportFolderBookmark;

            var _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var bookmarks = _dataService.FolderBooksmarksReposity.GetAll();
            foreach (var bookmark in bookmarks)
            {
                var storage = await _fileService.GetStorageFolderFromFolderBookmarkAsync(bookmark);
                var item = new FolderBookmarkListItem
                {
                    Storage = storage,
                    Bookmark = bookmark
                };
                Folders.Add(item);

                if (bookmark.Bookmark == _exportFolderBookmark.Bookmark)
                {
                    SelectedFolder = item;
                }
            }
        }

        [RelayCommand]
        public async void AddExportFolder()
        {
            var bookmark = await _fileService.RequestFolderBookmarkAsync(false);
            if (bookmark != null)
            {
                var storageFolder = await _fileService.GetStorageFolderFromFolderBookmarkAsync(bookmark);
                await _dataService.FolderBooksmarksReposity.CreateAsync(bookmark);
                var item = new FolderBookmarkListItem { Bookmark = bookmark, Storage = storageFolder };

                Folders.Add(item);
                SelectedFolder = item;
            }
        }

        [RelayCommand(CanExecute = nameof(CanRemoveFolder))]
        public async void RemoveFolder()
        {
            if (SelectedFolder is null) return;

            if (SelectedFolder.Bookmark.Bookmark == "DEFAULT") return;

            await _dataService.FolderBooksmarksReposity.RemoveAsync(SelectedFolder.Bookmark);

            Folders.Remove(SelectedFolder);
        }
        public bool CanRemoveFolder() => SelectedFolder != null && SelectedFolder.Bookmark.Bookmark != "DEFAULT";


        [RelayCommand(CanExecute = nameof(CanAccept))]
        public async void Accept()
        {
            Close(true, SelectedFolder, "Accept");
        }
        public bool CanAccept() => SelectedFolder != null;

        [RelayCommand]
        public void Cancel() => base.Cancel();
    }
}
