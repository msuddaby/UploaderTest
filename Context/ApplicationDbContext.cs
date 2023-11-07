using Microsoft.EntityFrameworkCore;
using UploaderTest.Models;

namespace UploaderTest.Context
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        public DbSet<Media> Media { get; set; } = null!;
    }
}
