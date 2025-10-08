using LuxDrive.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static LuxDrive.Data.Common.EntityConstants.ApplicationUser;


namespace LuxDrive.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> entity)
        {
          entity
                .HasKey(au => au.Id);

            entity
                .Property(au => au.FirstName)
                .IsRequired()
                .HasMaxLength(FirstNameLength)
                .HasComment(FirstNameComment);

            entity
               .Property(au => au.LastName)
               .IsRequired()
               .HasMaxLength(LastNameLength)
               .HasComment(LastNameComment);

        }
    }
}
