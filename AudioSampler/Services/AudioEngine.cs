using AudioSampler.Model;
using AudioSampler.ViewModels;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AudioSampler.Services
{
    public class AudioEngine
    {
        private readonly AudioService _audioService;
        
        private readonly DispatcherTimer _playbackTimer;

        private int _streamHandle;
        private AudioSampleSummaryViewModel _currentAudioSample;

        public AudioEngine(AudioService audioService) 
        {
            _audioService = audioService;

            _playbackTimer = new DispatcherTimer();
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(50);
            _playbackTimer.Tick += OnPlaybackTimer;
        }

        public async Task<AudioSample> CreateAudioSampleAsync(string path)
        {
            var stream = _audioService.CreatePlaybackStream(path);
            var duration = _audioService.GetLengthSeconds(stream);
            var sizeBytes = _audioService.GetLengthBytes(stream);
            var waveform = await _audioService.RenderWaveformAsync(path, 100);
            var maxpeak = _audioService.GetMaxPeak(waveform);
            var audioSample = new AudioSample
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                Duration = TimeSpan.FromSeconds(duration),
                FileSizeBytes = sizeBytes,
                MaxPeak = maxpeak,
                WaveformPoints = waveform,
                Normalize = false,
                SelectionStart = 0,
                SelectionEnd = 1
            };
            return audioSample;
        }

        private void OnPlaybackTimer(object? sender, EventArgs e)
        {
            var positionSeconds = _audioService.CheckPlaybackPosition(_streamHandle);
            var positionPercent = positionSeconds / _currentAudioSample.AudioSample.Duration.TotalSeconds;

            _currentAudioSample?.PlaybackPosition = positionPercent;

            if (positionPercent >= _currentAudioSample.AudioSample.SelectionEnd)
            {
                Stop();
            }
        }



        public void Play(AudioSampleSummaryViewModel audioSampleVM)
        {
            if (audioSampleVM is null) return;


            if(_currentAudioSample != audioSampleVM)
            {
                Stop();
                _currentAudioSample = audioSampleVM;
                _streamHandle = _audioService.CreatePlaybackStream(audioSampleVM.AudioSample.Path);
                if (audioSampleVM.AudioSample.Normalize) _audioService.Normalize(_streamHandle, audioSampleVM.AudioSample.MaxPeak);
            }
            
            audioSampleVM.Play();
            _audioService.Play(_streamHandle, audioSampleVM.AudioSample.SelectionStart * audioSampleVM.AudioSample.Duration.TotalSeconds, audioSampleVM.AudioSample.SelectionEnd * audioSampleVM.AudioSample.Duration.TotalSeconds);
            _playbackTimer.Start();
        }

        public void Stop()
        {
            if (_currentAudioSample == null || _streamHandle == 0)
            {
                return;
            }

            _currentAudioSample.Stop();
            _audioService.Stop(_streamHandle);
            _playbackTimer.Stop();

            _currentAudioSample = null;

        }

        public TimeSpan TotalTime => TimeSpan.FromSeconds(_audioService.GetLengthSeconds(_streamHandle));

        public void StreamFree()
        {
            if (_streamHandle != 0) 
            {
                _audioService.StreamFree(_streamHandle);
            }
            
        }

    }
}
