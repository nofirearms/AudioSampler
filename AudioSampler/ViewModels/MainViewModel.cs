using AudioSampler.Model;
using AudioSampler.Services;
using AudioSampler.ViewModels;
using AudioSampler.ViewModels.Modal;
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
using System.Threading;

namespace AudioSampler.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IScreenCaptureService _captureService;
        private readonly IFloatingWidgetService _floatingWidget;
        private readonly DataService _dataService;
        private readonly ModalService _modalService;

        //------------------------ MODAL -------------------------------------
        public IReadOnlyList<BaseModalViewModel> ActiveModals => _modalService.ActiveModals;
        public bool HasActiveModals => _modalService.ActiveModals.Any();
        //--------------------------------------------------------------------

        private readonly SourceCache<AudioSample, Guid> _audioSamplesCache = new(c => c.Id);
        //public IObservable<IChangeSet<AudioSampleSummaryViewModel, Guid>> ConnectCards() => _audioSamplesCache.Connect();

        [ObservableProperty]
        private ReadOnlyObservableCollection<AudioSampleSummaryViewModel> _audioSamples;


        public MainViewModel()
        {

        }

        public MainViewModel(IScreenCaptureService captureService, IFloatingWidgetService floatingWidget, DataService dataService, ModalService modalService)
        {
            _captureService = captureService;
            _floatingWidget = floatingWidget;
            _dataService = dataService;
            _modalService = modalService;

            ((INotifyCollectionChanged)_modalService.ActiveModals).CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasActiveModals));
                OnPropertyChanged(nameof(ActiveModals));
            };

            captureService.RecordFinished += (value) =>
            {

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

        [RelayCommand]
        private void ChooseCaptureApplitcation()
        {
            _captureService.ChooseApplicationGivePermission();
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
        private void OpenMiniMode()
        {
            //_captureService.EnterMiniMode();
            _floatingWidget.MinimizeToFloatingButton();
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