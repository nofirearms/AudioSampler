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

            var dbFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            // Железно проверяем и создаем директорию, если её нет
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            var dbPath = Path.Combine(dbFolder, "data.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
