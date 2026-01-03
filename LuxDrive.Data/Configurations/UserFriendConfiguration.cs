using LuxDrive.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxDrive.Data.Configurations
{
    public class UserFriendConfiguration : IEntityTypeConfiguration<UserFriend>
    {
        public void Configure(EntityTypeBuilder<UserFriend> entity)
        {
            entity
                .HasKey(x => new { x.UserId, x.FriendId });

            entity
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.Friend)
                .WithMany()
                .HasForeignKey(x => x.FriendId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
