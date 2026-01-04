using LuxDrive.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxDrive.Data.Configurations
{
    public class SharedFileConfiguration : IEntityTypeConfiguration<SharedFile>
    {
        public void Configure(EntityTypeBuilder<SharedFile> builder)
        {
            builder.HasOne(sf => sf.Sender)
                   .WithMany()
                   .HasForeignKey(sf => sf.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sf => sf.Receiver)
                   .WithMany()
                   .HasForeignKey(sf => sf.ReceiverId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}