using AudioSampler.Model;
using AudioSampler.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AudioSampler.Desktop.Services
{
    public class TempRecordingService : IScreenCaptureService
    {
        private readonly AudioService _audioService;
        private string _outputFile;

        public event Action<RecordResult> RecordFinished;
        public event Action<bool> SharingStateChanged;

        public TempRecordingService()
        {
            _audioService = new AudioService();
        }

        public void StartScreenCapture()
        {
            throw new NotImplementedException();
        }

        public void StartSharing()
        {
            _outputFile = $"Recording{DateTime.Now:hhmmss}.wav";
            _audioService.StartRecordingWav(_outputFile);
            SharingStateChanged?.Invoke(true);
        }

        public void StopScreenCapture()
        {
            throw new NotImplementedException();
        }

        public void StopSharing()
        {
            _audioService.StopRecordingWav();
            SharingStateChanged?.Invoke(false);
            RecordFinished?.Invoke(new RecordResult(_outputFile, TimeSpan.FromSeconds(4), Path.GetFileNameWithoutExtension(_outputFile), 100));
        }
    }
}
