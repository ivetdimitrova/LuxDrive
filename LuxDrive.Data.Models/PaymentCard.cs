using System.ComponentModel.DataAnnotations;

namespace LuxDrive.Data.Models
{
    public class PaymentCard
    {
        public int Id { get; set; }

        public string UserId { get; set; } 

        public string CardLast4 { get; set; } 
        public string CardType { get; set; } 
    }
}