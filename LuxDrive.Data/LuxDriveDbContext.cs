using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LuxDrive.Data
{
    public class LuxDriveDbContext : IdentityDbContext
    {
        public LuxDriveDbContext(DbContextOptions<LuxDriveDbContext> options)
          : base(options)
        {
        }
    }
}
