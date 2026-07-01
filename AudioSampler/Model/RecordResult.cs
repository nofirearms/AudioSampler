using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Model
{
    public class RecordResult
    {
        public string FilePath { get; }
        public TimeSpan Duration { get; }
        public string Name { get; }
        public long Size { get; }
        public RecordResult(string filePath, TimeSpan duration, string name, long size)
        {
            FilePath = filePath;
            Duration = duration;
            Name = name;
            Size = size;
        }

        public RecordResult(string filePath)
        {
            FilePath = filePath;
        }
    }
}
