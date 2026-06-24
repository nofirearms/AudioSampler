using AudioSampler.Model;
using System;

namespace AudioSampler.Services
{
    public interface IScreenCaptureService
    {
        void StartScreenCapture();
        void StopScreenCapture();
        void StartSharing();
        void StopSharing();

        event Action<RecordResult> RecordFinished;
        event Action<bool> SharingStateChanged;
    }
}
