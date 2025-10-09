using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static LuxDrive.Data.Common.EntityConstants.File;

using FileEntity = LuxDrive.Data.Models.File;

namespace LuxDrive.Data.Configurations
{
    public class FileConfiguration : IEntityTypeConfiguration<FileEntity>
    {
        public void Configure(EntityTypeBuilder<FileEntity> entity)
        {
            entity
                .HasKey(f => f.Id);

            entity
                .Property(f => f.Id)
                .HasComment(IdComment);

            entity
                .Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(NameLength)
                .HasComment(NameComment);

            entity
                .Property(f => f.Extension)
                .IsRequired()
                .HasMaxLength(ExtensionLength)
                .HasComment(ExtensionComment);

            entity
                .Property(f=>f.Size)
                .HasComment(SizeComment);

            entity
                .Property(f=>f.StorageUrl)
                .HasComment(StorageUrlComment);

            entity
                .Property(f=>f.UploadAt)
                .HasComment(UploadAtComment);

            entity
                .Property(f=>f.UserId)
                .HasComment(UserIdComment); 

            entity
                .HasOne(f=>f.User)
                .WithMany(u=>u.Files)
                .HasForeignKey(f => f.UserId);
             
        }
    }
}
