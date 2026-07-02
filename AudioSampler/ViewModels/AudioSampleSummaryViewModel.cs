using AudioSampler.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.ViewModels
{
    public partial class AudioSampleSummaryViewModel : ModdedObservableObject
    {
        private AudioSample _audioSample;

        public AudioSample AudioSample => _audioSample;

        public Guid Id => _audioSample.Id;
        public string Name => _audioSample.Name;
        public string SourceApp => _audioSample.SourceApp;
        public DateTime DateCreated => _audioSample.DateCreated;
        public TimeSpan Duration => _audioSample.Duration;

        public double SelectionStart => _audioSample.SelectionStart;
        public double SelectionEnd => _audioSample.SelectionEnd;


        [ObservableProperty]
        private bool _isPlaying = false;

        [ObservableProperty]
        private double _playbackPosition = 0;


        public AudioSampleSummaryViewModel(AudioSample audioSample) 
        {
            _audioSample = audioSample;
        }

        public void Play()
        {
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            PlaybackPosition = 0;
        }
    }
}
