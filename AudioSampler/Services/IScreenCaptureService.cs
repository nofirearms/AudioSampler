using AudioSampler.Model;
using System;

namespace AudioSampler.Services
{
    public interface IScreenCaptureService
    {
        void StartScreenCapture();
        void StopScreenCapture();
        void ChooseApplicationGivePermission();

        event Action<RecordResult> RecordFinished;
    }
}
