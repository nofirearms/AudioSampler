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
        public long DurationMs { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime DateCreated { get; set; }
        public string? SourceApp { get; set; }

    }
}
