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
        public DbSet<MS_1> MS_1 { get; set; }
        public DbSet<MS_2> MS_2 { get; set; }
        public DbSet<MS_3> MS_3 { get; set; }
    }
}
