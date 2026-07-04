using AudioSampler.Services;
using Avalonia.Styling;
using Avalonia.Xaml.Interactions.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.ViewModels.Modal
{
    public partial class SettingsViewModel : BaseModalViewModel<object>
    {
        private readonly ThemeService _themeService;
        private readonly FileService _fileService;

        public ThemeVariant[] Themes { get; }

        [ObservableProperty]
        private ThemeVariant _theme;
        partial void OnThemeChanged(ThemeVariant value)
        {
            if (value is null) return;

            var _ = _themeService.Set(value);
        }


        public SettingsViewModel(ThemeService themeService, FileService fileService)
        {
            _themeService = themeService;
            _fileService = fileService;

            Header = "Settings";

            Themes = _themeService.GetThemes();
            _theme = _themeService.GetCurrent();
        }

        [RelayCommand]
        public async Task Support()
        {
            try
            {
                var uri = new Uri("https://yoomoney.ru/fundraise/1ILP8IPBRUI.260627");

                var topLevel = _fileService.GetTopLevel();
                await topLevel.Launcher.LaunchUriAsync(uri);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }
    }
}
