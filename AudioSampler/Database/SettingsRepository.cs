
namespace AudioSampler.Database
{
    public class SettingsRepository
    {
        private readonly AppDbContext _context;

        public SettingsRepository(AppDbContext context)
        {
            _context = context;
        }
    }
}
