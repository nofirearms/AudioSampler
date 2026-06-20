using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Services
{
    public interface IScreenCaptureService
    {
        void StartScreenCapture();
        void StopScreenCapture();
        void ChooseApplicationGivePermission();
    }
}
