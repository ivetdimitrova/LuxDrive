using System.ComponentModel.DataAnnotations;

namespace LuxDrive.ViewModels.Pricing
{
    public class CheckoutViewModel
    {
        public string PlanName { get; set; }

        [Required]
        public string CardName { get; set; }

        [Required]
        [StringLength(19)] 
        public string CardNumber { get; set; }

        [Required]
        [StringLength(5)] 
        public string Expiry { get; set; }

        [Required]
        [StringLength(3)]
        public string CVC { get; set; }

        public string PriceDisplay => PlanName?.ToLower() switch
        {
            var p when p.Contains("pro") => "14.99",
            var p when p.Contains("premium") => "29.99",
            var p when p.Contains("enterprise") => "Custom",
            _ => "4.99"
        };
    }
}