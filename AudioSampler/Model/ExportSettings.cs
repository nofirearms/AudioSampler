using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AudioSampler.Model
{
    public class ExportSettings
    {

        public string Name { get; set; }
        public IStorageFolder Folder { get; set; }
        public ExportFormat Format { get; set; }
        public bool Trim { get; set; }
        public bool Normalize { get; set; }
        public FolderBookmark FolderBookmark { get; set; }

        public string FullFilePath => Path.Combine(Folder.Path.LocalPath, $"{Name}.{Format.ToString()}");

    }


}
