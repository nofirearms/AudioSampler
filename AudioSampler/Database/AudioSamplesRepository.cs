

using AudioSampler.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AudioSampler.Database
{
    public class AudioSamplesRepository
    {
        private readonly AppDbContext _context;

        public AudioSamplesRepository(AppDbContext context)
        {
            _context = context;
        }

        public AudioSample Get(Guid id) => _context.AudioSamples.FirstOrDefault(s => s.Id == id);
        public IEnumerable<AudioSample> GetAll() => _context.AudioSamples.ToList();
        public async Task CreateOrUpdateAsync(AudioSample audioSample)
        {
            try
            {
                //var entity = _context.AudioSamples.FindAsync(audioSample.Id);
                await _context.AudioSamples.AddAsync(audioSample);

                await SaveDatabase();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        } 

        public async Task RemoveAsync(AudioSample audioSample)
        {
            _context.AudioSamples.Remove(audioSample);

            await SaveDatabase();
        }

        public async Task SaveDatabase() => await _context.SaveChangesAsync();
    }
}
