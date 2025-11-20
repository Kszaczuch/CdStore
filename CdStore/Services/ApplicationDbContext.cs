using CdStore.Models;
using Microsoft.EntityFrameworkCore;

namespace CdStore.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Album> Albumy { get; set; } = null!;
    }
}
