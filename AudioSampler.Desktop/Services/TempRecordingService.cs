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
        private int _recordStream;

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
            _recordStream = _audioService.StartRecordingWav(_outputFile);
            SharingStateChanged?.Invoke(true);
        }

        public void StopScreenCapture()
        {
            throw new NotImplementedException();
        }

        public void StopSharing()
        {
            _audioService.StopRecordingWav(_recordStream);
            SharingStateChanged?.Invoke(false);

            var stream = _audioService.CreatePlaybackStream(_outputFile);
            var duration = _audioService.GetLengthSeconds(stream);

            RecordFinished?.Invoke(new RecordResult(_outputFile, TimeSpan.FromSeconds(duration), Path.GetFileNameWithoutExtension(_outputFile), 100));


        }
    }
}
