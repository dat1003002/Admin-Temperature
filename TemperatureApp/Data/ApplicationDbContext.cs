using Microsoft.EntityFrameworkCore;
using TemperatureApp.Models;

namespace TemperatureApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BTLH3> BTLH3 { get; set; }
        public DbSet<BTLH4> BTLH4 { get; set; }
        public DbSet<BTLH5> BTLH5 { get; set; }
        public DbSet<MS_1> MS_1 { get; set; }
        public DbSet<MS_2> MS_2 { get; set; }
        public DbSet<MS_3> MS_3 { get; set; }
        public DbSet<MS_4> MS_4 { get; set; }
        public DbSet<MS_5> MS_5 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BTLH3>().ToTable("BTLH#3");
            modelBuilder.Entity<BTLH4>().ToTable("BTLH#4"); // Ánh xạ BTLH4
            modelBuilder.Entity<BTLH5>().ToTable("BTLH#5"); // Ánh xạ BTLH5
        }
    }
}