using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Model
{
    public class ExportSettings
    {

        public string Name { get; set; }
        public string Path {  get; set; }
        public ExportFormat Format { get; set; }
        public bool Trim { get; set; }
        public bool Normalize { get; set; }

    }


}
