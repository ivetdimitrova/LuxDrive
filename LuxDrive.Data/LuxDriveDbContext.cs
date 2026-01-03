using LuxDrive.Data.Configurations;
using LuxDrive.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using FileEntity = LuxDrive.Data.Models.File;

namespace LuxDrive.Data
{
    public class LuxDriveDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public LuxDriveDbContext(DbContextOptions<LuxDriveDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<FileEntity> Files { get; set; } = null!;

        public DbSet<FriendRequest> FriendRequests { get; set; } = null!;
        public DbSet<UserFriend> UserFriends { get; set; } = null!;
        public DbSet<SharedFile> SharedFiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new ApplicationUserConfiguration());
            builder.ApplyConfiguration(new FileConfiguration());
            builder.ApplyConfiguration(new UserFriendConfiguration());


            
        }
    }
}
