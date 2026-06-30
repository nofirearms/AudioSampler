using AudioSampler.Services;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AudioSampler.ViewModels.Modal
{
    public partial class SettingsViewModel : BaseModalViewModel<object>
    {
        private readonly ThemeService _themeService;

        public ThemeVariant[] Themes { get; }

        [ObservableProperty]
        private ThemeVariant _theme;
        partial void OnThemeChanged(ThemeVariant value)
        {
            if (value is null) return;

            var _ = _themeService.Set(value);
        }



        public SettingsViewModel(ThemeService themeService)
        {
            _themeService = themeService;

            Header = "Settings";

            Themes = _themeService.GetThemes();
            _theme = _themeService.GetCurrent();
        }
    }
}
