using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Model
{
    public class FolderBookmarkListItem
    {
        public IStorageFolder Storage { get; set; }
        public FolderBookmark Bookmark { get; set; }

        public string Path => Storage.Path.LocalPath; //Uri.UnescapeDataString(Storage.Path.AbsolutePath); 
        public string Name => Storage.Name;
    }
}
