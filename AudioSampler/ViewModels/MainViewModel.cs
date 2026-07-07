using AudioSampler.Model;
using AudioSampler.Services;
using AudioSampler.ViewModels;
using AudioSampler.ViewModels.Modal;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AudioSampler.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IScreenCaptureService _captureService;
        private readonly DataService _dataService;
        private readonly ModalService _modalService;
        private readonly AudioEngine _audioEngine;
        private readonly FileService _fileService;


        private readonly SourceCache<AudioSample, Guid> _audioSamplesCache = new(c => c.Id);

        [ObservableProperty]
        private ReadOnlyObservableCollection<AudioSampleSummaryViewModel> _audioSamples;


        [ObservableProperty]
        private bool _sharingState = false;

        public MainViewModel()
        {

            if (Design.IsDesignMode)
            {
                var textChanged = this.WhenPropertyChanged(x => x.NameTextFilter)
                    .Select(_ => CreateFilter());

                // 1. Захватываем UI-поток (здесь он еще доступен)
                var uiContext = SynchronizationContext.Current;

                _audioSamplesCache.Connect()
                    .Transform(a => new AudioSampleSummaryViewModel(a))
                    .Filter(textChanged)
                    .Sort(SortExpressionComparer<AudioSampleSummaryViewModel>.Descending(s => s.DateCreated))
                    .ObserveOn(uiContext)
                    .Bind(out _audioSamples)
                    .Subscribe();


                _audioSamplesCache.AddOrUpdate(new AudioSample { DateCreated = DateTime.Now, Duration = TimeSpan.FromMilliseconds(1200), Name = "RecordTEST", Path = "sdf", SourceApp = "Youtube" });
                _audioSamplesCache.AddOrUpdate(new AudioSample { DateCreated = DateTime.Now, Duration = TimeSpan.FromMilliseconds(3600), Name = "Record132123123123", Path = "sdf", SourceApp = "Browser" });
            }
        }

        public MainViewModel(IScreenCaptureService captureService, DataService dataService, ModalService modalService, AudioEngine audioEngine, FileService fileService)
        {
            _captureService = captureService;
            _dataService = dataService;
            _modalService = modalService;
            _audioEngine = audioEngine;
            _fileService = fileService;


            captureService.RecordFinished += async(value) =>
            {
                //анфриз ui
                await Task.Delay(30);
                await CreateAudioSample(value.FilePath);
            };

            captureService.SharingStateChanged += (state) =>
            {
                SharingState = state;
            };

            var textChanged = this.WhenPropertyChanged(x => x.NameTextFilter)
                .Select(_ => CreateFilter());

            // 1. Захватываем UI-поток (здесь он еще доступен)
            var uiContext = SynchronizationContext.Current;

            _audioSamplesCache.Connect()
                .Transform(a => new AudioSampleSummaryViewModel(a))
                .Filter(textChanged)
                .Sort(SortExpressionComparer<AudioSampleSummaryViewModel>.Descending(s => s.DateCreated))
                .ObserveOn(uiContext) 
                .Bind(out _audioSamples) 
                .Subscribe();

            LoadData();
        }

        private void LoadData()
        {
            var samples = _dataService.AudioSamplesRepository.GetAll();
            _audioSamplesCache.AddOrUpdate(samples);
        }

        private async Task CreateAudioSample(string path)
        {
            var sample = await _audioEngine.CreateAudioSampleAsync(path);
            _audioSamplesCache.AddOrUpdate(sample);
            await _dataService.AudioSamplesRepository.CreateOrUpdateAsync(sample);
        }

        //не используется
        [RelayCommand]
        private void StartSharing()
        {
            _captureService.StartSharing();
        }
        //не используется
        [RelayCommand]
        private void StopSharing()
        {
            _captureService.StopSharing();
        }

        [RelayCommand]
        private void StartCapture()
        {
            _captureService.StartScreenCapture();
        }

        [RelayCommand]
        private void StopCapture()
        {
            _captureService.StopScreenCapture();
        }

        [RelayCommand]
        public void StartStopPlayback(AudioSampleSummaryViewModel audioSampleVM)
        {
            if (audioSampleVM.IsPlaying) 
            {
                _audioEngine.Stop();
            }
            else
            {
                _audioEngine.Play(audioSampleVM);
            }


        }


        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task OpenSettings()
        {
            await _modalService.OpenSettingsModal();
        }


        [RelayCommand]
        private async void OpenAudioSampleDetail(AudioSampleSummaryViewModel audioSampleVM)
        {
            if (audioSampleVM is not AudioSampleSummaryViewModel) return;

            if (audioSampleVM.IsPlaying)
            {
                _audioEngine.Stop();
            }

            var result = await _modalService.OpenAudioSampleDetailModal(audioSampleVM.AudioSample);
            if (!result.Success) return;
            if (result.ButtonTag == "Edit")
            {
                _audioSamplesCache.AddOrUpdate(result.Data);
            }
            else if(result.ButtonTag == "Remove")
            {
                _audioSamplesCache.Remove(result.Data);
                await _dataService.AudioSamplesRepository.RemoveAsync(result.Data);
                _audioEngine.StreamFree();
                _fileService.DeleteFileByPath(result.Data.Path);

            }
        }

        [ObservableProperty]
        private string _nameTextFilter;
        private Func<AudioSampleSummaryViewModel, bool> CreateFilter()
        {
            return item =>
            {

                var text_pass = string.IsNullOrEmpty(NameTextFilter) ||
                                 item.Name.Contains(NameTextFilter, StringComparison.OrdinalIgnoreCase) ||
                                 item.SourceApp.Contains(NameTextFilter, StringComparison.OrdinalIgnoreCase) ;

                return text_pass; 
            };
        }

    }
}