using AudioSampler.Database;
using AudioSampler.Extensions;
using AudioSampler.Model;
using AudioSampler.Services;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace AudioSampler.ViewModels.Modal
{
    public partial class AudioSampleDetailViewModel : BaseModalViewModel<AudioSample>
    {
        private readonly AudioService _audioService;
        private readonly ModalService _modalService;
        private readonly DataService _dataService;
        private readonly FileService _fileService;
        private readonly NotificationService _notificationService;

        private AudioSample _audioSample;

        [ObservableProperty]
        private float[] _audioGraphPoints;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private double _startPercent = 0d;
        [ObservableProperty]
        private double _endPercent = 1d;

        [ObservableProperty]
        private double _playbackPositionPercent;

        private int _stream;

        private DispatcherTimer _playbackTimer;

        [ObservableProperty]
        private bool _isPlaying = false;

        [ObservableProperty]
        private bool _normalized = false;

        [ObservableProperty]
        private TimeSpan _totalTime;

        [ObservableProperty]
        private TimeSpan _selectionTime;

        [ObservableProperty]
        private TimeSpan _currentTime;


        public AudioSampleDetailViewModel(
            AudioService audioService, 
            ModalService modalService, 
            DataService dataService, 
            FileService fileService, 
            NotificationService notificationService,
            AudioSample audioSample)
        {
            _audioService = audioService;
            _modalService = modalService;
            _dataService = dataService;
            _fileService = fileService;
            _notificationService = notificationService;

            _audioSample = audioSample;

            Header = "Edit";

            _stream = _audioService.CreatePlaybackStream(_audioSample.Path);

            Name = _audioSample.Name;
            AudioGraphPoints = _audioSample.WaveformPoints;
            StartPercent = _audioSample.SelectionStart;
            EndPercent = _audioSample.SelectionEnd;

            _playbackTimer = new DispatcherTimer();
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(100);
            _playbackTimer.Tick += OnPlaybackTimer;

            TotalTime = TimeSpan.FromSeconds(_audioService.GetLengthSeconds(_stream));

            if (_audioSample.Normalize) Normalize(true);
        }


        [RelayCommand]
        public void Play()
        {
            _audioService.Play(_stream, _audioSample.Duration.TotalSeconds * StartPercent, _audioSample.Duration.TotalSeconds * EndPercent);
            _playbackTimer.Start();
            IsPlaying = true;
        }

        [RelayCommand]
        public void Stop()
        {
            _audioService.Stop(_stream);
            _playbackTimer.Stop();
            IsPlaying = false;

            CurrentTime = TimeSpan.FromSeconds(_startPercent * _audioSample.Duration.TotalSeconds);
            PlaybackPositionPercent = _startPercent;
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task Normalize(bool normalize)
        {
            if (normalize)
            {
                Normalized = true;
                _audioService.Normalize(_stream, _audioSample.MaxPeak);
                var gain = 1d / _audioSample.MaxPeak;
                var points = AudioGraphPoints.Select(p => p * (float)gain).ToArray();
                AudioGraphPoints = points;
            }
            else
            {
                Normalized = false;
                _audioService.Normalize(_stream, 1);
                AudioGraphPoints = AudioGraphPoints.Select(p => p * (float)_audioSample.MaxPeak).ToArray();
            }

            
        }

        private void OnPlaybackTimer(object? sender, EventArgs e)
        {
            var positionSeconds = _audioService.CheckPlaybackPosition(_stream);
            PlaybackPositionPercent = positionSeconds / _audioSample.Duration.TotalSeconds;
            CurrentTime = TimeSpan.FromSeconds(positionSeconds);
            if(PlaybackPositionPercent >= EndPercent)
            {
                Stop();
            }
        }


        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task Edit()
        {
            _audioSample.Name = Name;
            _audioSample.SelectionStart = StartPercent;
            _audioSample.SelectionEnd = EndPercent;
            _audioSample.Normalize = Normalized;

            await _dataService.AudioSamplesRepository.CreateOrUpdateAsync(_audioSample);

            Close(true, _audioSample, "Edit");
        }


        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task Export()
        {
            //Собираем настройки для модального окна
            try
            {
                var setting = _dataService.SettingsRepository.Get(SettingKey.FolderBookmark);

                //var folderBookmark = _dataService.FolderBooksmarksReposity.GetAll().FirstOrDefault(b => b.Bookmark == value);

                var folderBookmark = setting is null ? null : new FolderBookmark(setting.Value);

                var folder = await _fileService.GetStorageFolderFromFolderBookmarkAsync(folderBookmark);

                if(folder != null)
                {
                    var folderExists = await folder?.ExistsAsync();
                    if (!folderExists) folder = null;
                }


                //Открываем модальное окно с найстройками экспорта
                var result = await _modalService.OpenExportModal(new ExportSettings() { Name = Name, Folder = folder, FolderBookmark = folderBookmark });

                if (result.Success)
                {
                    //переименовываем файл если такой существует
                    var file = await result.Data.Folder.GetFileAsync($"{result.Data.Name}.{result.Data.Format}");
                    if (file != null)
                    {
                        result.Data.Name = $"{result.Data.Name} [{DateTime.Now:yyyy-MM-dd HHmmss}]";
                        file.Dispose();
                    }

                    var renderResult = await _audioService.RenderToFileAsync(
                        _audioSample.Path, 
                        result.Data.Folder, 
                        result.Data.Name,
                        result.Data.Trim ? StartPercent * _audioSample.Duration.TotalSeconds : 0,
                        result.Data.Trim ? EndPercent * _audioSample.Duration.TotalSeconds : _audioSample.Duration.TotalSeconds,
                        result.Data.Normalize,
                        result.Data.Format);

                    if (renderResult.Success) 
                    {
                        _notificationService.Show(Material.Icons.MaterialIconKind.ContentSaveMove, result.Data.Name, result.Data.FullFilePath, Avalonia.Controls.Notifications.NotificationType.Success);
                    }
                    else
                    {
                        _notificationService.Show(Material.Icons.MaterialIconKind.Error, "Error", renderResult.ErrorMessage, Avalonia.Controls.Notifications.NotificationType.Error);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
            
            
        }

        [RelayCommand]
        public async Task Remove()
        {
            var mbResult = await _modalService.OpenMessageBoxModal("Delete File", $"Are you sure you want to delete {_audioSample.Name}?", ["No", "Yes"]);
            if(mbResult.ButtonTag == "Yes")
            {
                Stop();
                Close(true, _audioSample, "Remove");
            }
        }

        protected override void Cancel()
        {
            if (IsPlaying) Stop();

            base.Cancel();
        }
    }
}
