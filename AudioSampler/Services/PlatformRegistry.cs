using AudioSampler.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Services
{
    /// <summary>
    /// Класс костыль, чтоб регать сервис с андройда, так как OnCreate в MainActivity срабатывает позже чем регистрация сервисов.
    /// </summary>
    public class LazyScreenCaptureServiceWrapper : IScreenCaptureService
    {
        private IScreenCaptureService? _realService;

        private static LazyScreenCaptureServiceWrapper _instance;
        public static LazyScreenCaptureServiceWrapper Instance => _instance ??= new LazyScreenCaptureServiceWrapper();

        public LazyScreenCaptureServiceWrapper()
        {

        }

        public void RegisterRealService(IScreenCaptureService service)
        {
            _realService = service;
            _realService.RecordFinished += (value) => RecordFinished?.Invoke(value);
            _realService.SharingStateChanged += (value) => SharingStateChanged?.Invoke(value);
        }

        public void StartScreenCapture() => _realService?.StartScreenCapture();
        public void StopScreenCapture() => _realService?.StopScreenCapture();
        public void StartSharing() => _realService?.StartSharing();
        public void StopSharing() => _realService?.StopSharing();

        public event Action<RecordResult> RecordFinished;
        public event Action<bool> SharingStateChanged;
    }
}
