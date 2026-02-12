using System.ComponentModel.DataAnnotations;

namespace LuxDrive.Data.Models
{
    public class PaymentCard
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; } 
        public ApplicationUser User { get; set; } = null!;

        public string CardLast4 { get; set; } 
        public string CardType { get; set; } 
    }
}