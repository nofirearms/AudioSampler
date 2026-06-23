using AudioSampler.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Services
{
    public class DataService
    {
        public AudioSamplesRepository AudioSamplesRepository { get; private set; }
        public SettingsRepository SettingsRepository { get; private set; }

        public DataService(AudioSamplesRepository audioSamplesRepository, SettingsRepository settingsRepository)
        {
            AudioSamplesRepository = audioSamplesRepository;
            SettingsRepository = settingsRepository;
        }

        
    }
}
