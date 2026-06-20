using AudioSampler.Services;
using AudioSampler.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace AudioSampler.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IScreenCaptureService _captureService;
        private readonly IFloatingWidgetService _floatingWidget;

        public MainViewModel(IScreenCaptureService captureService, IFloatingWidgetService floatingWidget)
        {
            _captureService = captureService;
            _floatingWidget = floatingWidget;
        }

        [RelayCommand]
        private void ChooseCaptureApplitcation()
        {
            _captureService.ChooseApplicationGivePermission();
        }

        [RelayCommand]
        private void StartCapture()
        {
            _captureService.StartScreenCapture();
        }

        [RelayCommand]
        private void StopCapture()
        {
            _captureService.StopScreenCapture();
        }

        [RelayCommand]
        private void OpenMiniMode()
        {
            //_captureService.EnterMiniMode();
            _floatingWidget.MinimizeToFloatingButton();
        }
    }
}