using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Model
{
    /// <summary>
    /// Под Export Folder всегда подразумевается Bookmark токен
    /// </summary>
    public class FolderBookmark
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Bookmark { get; set; }

        public FolderBookmark(string bookmark)
        {
            Bookmark = bookmark;
        }

        public FolderBookmark()
        {
            
        }
    }
}
