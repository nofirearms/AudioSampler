using AudioSampler.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.Database
{
    public class FolderBookmarksRepository
    {
        private readonly AppDbContext _context;

        public FolderBookmarksRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<FolderBookmark> GetAll() => _context.ExportFolders.ToList();

        public async Task CreateAsync(FolderBookmark bookmark)
        {
            await _context.ExportFolders.AddAsync(bookmark);
            await SaveDatabase();
        }

        public async Task RemoveAsync(FolderBookmark bookmark)
        {
            var entity = _context.ExportFolders.FirstOrDefault(x => x.Bookmark == bookmark.Bookmark);
            if(entity != null) 
            {
                _context.Remove(entity);
                await SaveDatabase();
            }
        }

        private async Task SaveDatabase() => await _context.SaveChangesAsync();
    }
}
