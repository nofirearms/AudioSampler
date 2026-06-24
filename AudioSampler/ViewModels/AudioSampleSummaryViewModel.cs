using AudioSampler.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.ViewModels
{
    public class AudioSampleSummaryViewModel : ModdedObservableObject
    {
        private AudioSample _audioSample;

        public Guid Id => _audioSample.Id;
        public string Name => _audioSample.Name;
        public string SourceApp => _audioSample.SourceApp;
        public DateTime DateCreated => _audioSample.DateCreated;

        public TimeSpan Duration => _audioSample.Duration;

        public AudioSampleSummaryViewModel(AudioSample audioSample) 
        {
            _audioSample = audioSample;
        }
    }
}
