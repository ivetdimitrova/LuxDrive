using LuxDrive.Data.Configurations;
using LuxDrive.Data.Models;
using LuxDrive.Data.Models.Enums;
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

            builder.Entity<UserFriend>()
                .HasKey(x => new { x.UserId, x.FriendId });

            builder.Entity<UserFriend>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserFriend>()
                .HasOne(x => x.Friend)
                .WithMany()
                .HasForeignKey(x => x.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendRequest>()
                .HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendRequest>()
                .HasOne(x => x.Receiver)
                .WithMany()
                .HasForeignKey(x => x.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
