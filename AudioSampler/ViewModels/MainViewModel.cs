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

        //------------------------ MODAL -------------------------------------
        public IReadOnlyList<IModal> ActiveModals => _modalService.ActiveModals;
        public bool HasActiveModals => _modalService.ActiveModals.Any();
        //--------------------------------------------------------------------

        private readonly SourceCache<AudioSample, Guid> _audioSamplesCache = new(c => c.Id);
        //public IObservable<IChangeSet<AudioSampleSummaryViewModel, Guid>> ConnectCards() => _audioSamplesCache.Connect();

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

        public MainViewModel(IScreenCaptureService captureService, DataService dataService, ModalService modalService)
        {
            _captureService = captureService;
            _dataService = dataService;
            _modalService = modalService;

            ((INotifyCollectionChanged)_modalService.ActiveModals).CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(ActiveModals));
                OnPropertyChanged(nameof(HasActiveModals));
            };

            captureService.RecordFinished += (value) =>
            {
                var sample = new AudioSample
                {
                    Duration = value.Duration,
                    FileSizeBytes = value.Size,
                    Name = value.Name,
                    Path = value.FilePath
                };
                AddAudioSample(sample);
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

        private async Task AddAudioSample(AudioSample audioSample)
        {
            _audioSamplesCache.AddOrUpdate(audioSample);
            await _dataService.AudioSamplesRepository.CreateOrUpdateAsync(audioSample);
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
        private async void OpenSettings()
        {
            //_captureService.EnterMiniMode();
            await _modalService.OpenSettingsModal();
        }


        [RelayCommand]
        private async void OpenAudioSampleDetail(AudioSampleSummaryViewModel audioSampleVM)
        {
            if (audioSampleVM is not AudioSampleSummaryViewModel) return;

            var result = await _modalService.OpenAudioSampleDetailModal(audioSampleVM.AudioSample);
            if (result.Success)
            {
                _audioSamplesCache.AddOrUpdate(result.Data);
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