using static LuxDrive.Data.Common.EntityConstants.SharedFile;

using LuxDrive.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxDrive.Data.Configurations
{
    public class SharedFileConfiguration : IEntityTypeConfiguration<SharedFile>
    {
        public void Configure(EntityTypeBuilder<SharedFile> entity)
        {
            entity
               .Property(f => f.Id)
               .HasComment(IdComment);

            entity
               .Property(f => f.FileId)
               .HasComment(FileIdComment);

            entity
               .Property(f => f.SenderId)
               .HasComment(SenderIdComment);

            entity
               .Property(f => f.ReceiverId)
               .HasComment(ReceiverIdComment);

            entity
               .Property(f => f.SharedOn)
               .HasComment(SharedOnComment);
           
            entity.HasOne(sf => sf.Sender)
                   .WithMany()
                   .HasForeignKey(sf => sf.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sf => sf.Receiver)
                   .WithMany()
                   .HasForeignKey(sf => sf.ReceiverId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}