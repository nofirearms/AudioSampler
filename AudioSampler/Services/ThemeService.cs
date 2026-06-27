using AudioSampler.Database;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.Services
{
    public class ThemeService
    {
        private readonly SettingsRepository _settings;

        public static readonly ThemeVariant OrangeTheme = new ThemeVariant("Orange", ThemeVariant.Dark);
        public static readonly ThemeVariant BlueTheme = new ThemeVariant("Blue", ThemeVariant.Dark);

        public ThemeService(SettingsRepository settingsRepository)
        {
            _settings = settingsRepository; 
        }

        public async Task Change(ThemeVariant theme)
        {
            App.Current.RequestedThemeVariant = theme; 

            await _settings.ChangeValue("Theme", theme.Key.ToString());
        }

        public async Task LoadFromSettings()
        {
            var theme = _settings.Get("Theme");

            if(theme is null)
            {
                await Change(OrangeTheme);
            }
            else
            {
                var themeVariant = GetFromKey(theme.Value);
                await Change(themeVariant);

            }
        }

        public ThemeVariant GetFromKey(string key)
        {
            var field = typeof(ThemeService).GetField($"{key}Theme", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if(field != null)
            {
                var value = field.GetValue(null);
                if(value is ThemeVariant variant)
                {
                    return variant;
                }
            }

            return OrangeTheme;
        }

        public ThemeVariant GetCurrent() => App.Current.ActualThemeVariant;
    }

}
