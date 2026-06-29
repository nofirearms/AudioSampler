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
        private readonly ModalService _modalService;

        [ObservableProperty]
        private IStorageFolder _folder;

        [ObservableProperty]
        private string _fileName;

        public List<ExportFormat> Formats => Enum.GetValues(typeof(ExportFormat)).Cast<ExportFormat>().ToList();

        [ObservableProperty]
        private ExportFormat _format;

        [ObservableProperty]
        private bool _trim;

        [ObservableProperty]
        private bool _normalize;

        private FolderBookmark _bookmark;

        public ExportAudioSampleViewModel(DataService dataService, FileService fileService, ModalService modalService, ExportSettings exportSettings)
        {
            _fileService = fileService;
            _dataService = dataService;
            _modalService = modalService;

            _folder = exportSettings.Folder;
            _fileName = exportSettings.Name; 
            _format = exportSettings.Format;
            _trim = exportSettings.Trim;
            _normalize = exportSettings.Normalize;
            _bookmark = exportSettings.FolderBookmark;

            Header = "Export";

            var _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            
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
        public async void OpenFoldersList()
        {
            var result = await _modalService.OpenFoldersListModal(_bookmark);
            if (result.Success)
            {
                Folder = result.Data.Storage;
                _bookmark = result.Data.Bookmark;
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
        public void Cancel() => base.Cancel();

    }
}
