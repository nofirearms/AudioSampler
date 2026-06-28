using AudioSampler.Database;
using AudioSampler.Model;
using AudioSampler.Services;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;


namespace AudioSampler.ViewModels.Modal
{
    public partial class AudioSampleDetailViewModel : BaseModalViewModel<AudioSample>
    {
        private readonly AudioService _audioService;
        private readonly ModalService _modalService;
        private readonly SettingsRepository _settings;
        private readonly FileService _fileService;
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
        private double _maxPeak;
        private DispatcherTimer _playbackTimer;

        [ObservableProperty]
        private bool _isPlaying = false;

        private bool _normalized = false;
        public AudioSampleDetailViewModel(
            AudioService audioService, 
            ModalService modalService, 
            SettingsRepository settings, 
            FileService fileService, 
            AudioSample audioSample)
        {
            _audioService = audioService;
            _modalService = modalService;
            _settings = settings;
            _fileService = fileService;
            _audioSample = audioSample;

            Header = "Edit";

            LoadData();
        }

        private async void LoadData()
        {
            Name = _audioSample.Name;
            AudioGraphPoints = await _audioService.RenderWaveformAsync(_audioSample.Path, 100);
            _stream = _audioService.CreatePlaybackStream(_audioSample.Path);
            _maxPeak = _audioService.GetMaxPeak(AudioGraphPoints);

            _playbackTimer = new DispatcherTimer();
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(100);
            _playbackTimer.Tick += OnPlaybackTimer;
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
        }

        [RelayCommand]
        public async void Normalize(bool normalize)
        {
            if (normalize)
            {
                _normalized = true;
                _audioService.Normalize(_stream, _maxPeak);
                var gain = 1d / _maxPeak;
                var points = AudioGraphPoints.Select(p => p * (float)gain).ToArray();
                AudioGraphPoints = points;
            }
            else
            {
                _normalized = false;
                _audioService.Normalize(_stream, 1);
                AudioGraphPoints = AudioGraphPoints.Select(p => p * (float)_maxPeak).ToArray();
            }

            
        }

        private void OnPlaybackTimer(object? sender, EventArgs e)
        {
            var positionSeconds = _audioService.CheckPlaybackPosition(_stream, _audioSample.Duration.TotalSeconds / EndPercent);
            PlaybackPositionPercent = positionSeconds / _audioSample.Duration.TotalSeconds;
            if(PlaybackPositionPercent >= EndPercent)
            {
                Stop();
            }
        }

        [RelayCommand]
        public async void Export()
        {
            //var path = _settings.Get("ExportPath") is null ? 
            var folder = await _fileService.GetPathAsync();

            var result = await _modalService.OpenExportModal(new ExportSettings() { Name = Name, Path = folder, Normalize = _normalized });

            if (result.Success)
            {

                await _audioService.RenderToFileAsync(_audioSample.Path, 
                    Path.Combine(
                        result.Data.Path, 
                        $"{result.Data.Name}.{result.Data.Format}"),
                        result.Data.Trim ? StartPercent * _audioSample.Duration.TotalSeconds : 0,
                        result.Data.Trim ? EndPercent * _audioSample.Duration.TotalSeconds : _audioSample.Duration.TotalSeconds, 
                        result.Data.Normalize, 
                        result.Data.Format);
            }
            
        }
    }
}
