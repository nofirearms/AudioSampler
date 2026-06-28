
using AudioSampler.Model;
using Avalonia.Xaml.Interactions.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AudioSampler.Database
{
    public class SettingsRepository
    {
        private readonly AppDbContext _context;

        public SettingsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task ChangeValue(string key, string value) 
        { 
            var setting = _context.Settings.FirstOrDefault(s => s.Key == key);
            if (setting is null) 
            { 
                setting = new Setting(key, value);
                _context.Add(setting);
            }
            else
            {
                setting.Value = value;
                _context.Update(setting);
            }

            await SaveDatabase();

        }
        public async Task ChangeValue(SettingKey key, string value)
        {
            ChangeValue(Enum.GetName(typeof(SettingKey), key), value);
        }


        public Setting Get(string key) => _context.Settings.FirstOrDefault(s => s.Key == key);

        public Setting Get(SettingKey key) => _context.Settings.FirstOrDefault(s => s.Key == Enum.GetName(typeof(SettingKey), key));

        private async Task SaveDatabase() => await _context.SaveChangesAsync();
    }
}
