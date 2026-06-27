using AudioSampler.Model;
using AudioSampler.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;


namespace AudioSampler.ViewModels.Modal
{
    public partial class AudioSampleDetailViewModel : BaseModalViewModel<AudioSample>
    {
        private readonly AudioService _audioService;
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
        public AudioSampleDetailViewModel(AudioService audioService, AudioSample audioSample)
        {
            _audioService = audioService;
            _audioSample = audioSample;

            Header = "Edit Sample";

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
                _audioService.Normalize(_stream, _maxPeak);
                var gain = 1d / _maxPeak;
                var points = AudioGraphPoints.Select(p => p * (float)gain).ToArray();
                AudioGraphPoints = points;
            }
            else
            {
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
    }
}
