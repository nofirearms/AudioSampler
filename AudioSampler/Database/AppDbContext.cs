using AudioSampler.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AudioSampler.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<AudioSample> AudioSamples { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<FolderBookmark> ExportFolders { get; set; }

        public AppDbContext()
        {
            Database.EnsureCreated();
        }

        public AppDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            //string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolder = Path.Combine(appDataRoot, "AudioSampler");
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            var dbPath = Path.Combine(dbFolder, "data.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
