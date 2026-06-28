using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Model
{
    public class Setting
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Key { get; set; }
        public string Value { get; set; }

        public Setting(string key, string value)
        {
            Key = key; Value = value;
        }

        public Setting(SettingKey key, string value)
        {
            Key = key.ToString(); Value = value;
        }
    }
}
