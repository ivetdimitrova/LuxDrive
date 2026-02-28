using static LuxDrive.Data.Common.EntityConstants.PaymentCard;

using LuxDrive.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxDrive.Data.Configurations
{
    public class PaymentCardConfiguration : IEntityTypeConfiguration<PaymentCard>
    {
        public void Configure(EntityTypeBuilder<PaymentCard> entity)
        {
            entity
             .Property(f => f.Id)
             .HasComment(IdComment);

            entity
             .Property(f => f.UserId)
             .HasComment(UserIdComment);

            entity
             .Property(f => f.CardLast4)
             .HasComment(CardLast4Comment);

            entity
             .Property(f => f.CardType)
             .HasComment(CardTypeComment);
        }
    }
}
