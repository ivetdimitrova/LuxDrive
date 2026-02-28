using static LuxDrive.Data.Common.EntityConstants.FriendRequest;

using LuxDrive.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace LuxDrive.Data.Configurations
{
    public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
    {
        public void Configure(EntityTypeBuilder<FriendRequest> entity)
        {
            entity
                .Property(f => f.Id)
                .HasComment(IdComment);

            entity
                .Property(f => f.SenderId)
                .HasComment(SenderIdComment);

            entity
                .Property(f => f.ReceiverId)
                .HasComment(ReceiverIdComment);

            entity
                .Property(f => f.Status)
                .HasComment(StatusComment);

            entity
                .Property(f => f.CreatedOn)
                .HasComment(CreatedOnComment);

            entity
                .HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.Receiver)
                .WithMany()
                .HasForeignKey(x => x.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
