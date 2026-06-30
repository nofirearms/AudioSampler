using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AudioSampler.Model
{
    public class AudioSample
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Path { get; set; }
        public TimeSpan Duration { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public string SourceApp { get; set; } = string.Empty;

        public double SelectionStart { get; set; } = 0;
        public double SelectionEnd { get; set; } = 1;
        public bool Normalize { get; set; } = false;

    }
}
