using System.ComponentModel.DataAnnotations;

namespace LuxDrive.ViewModels.Pricing
{
    public class CheckoutViewModel
    {
        public string PlanName { get; set; }

        [Required(ErrorMessage = "Card name is required")]
        [RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "Name must contain only letters")]
        public string CardName { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        [RegularExpression(@"^(\d{4}\s){3}\d{4}$|^(\d{16})$", ErrorMessage = "Invalid card format (16 digits)")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Format must be MM/YY")]
        public string Expiry { get; set; }

        [Required(ErrorMessage = "CVC is required")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVC must be 3 digits")]
        public string CVC { get; set; }

        /* Изчислимо свойство (Computed Property): Използва модерния C# 'switch expression' 
           за динамично определяне на цената според името на плана. */
        public string PriceDisplay => PlanName?.ToLower() switch
        {
            var p when p.Contains("pro") => "14.99",
            var p when p.Contains("premium") => "29.99",
            var p when p.Contains("enterprise") => "Custom",
            _ => "4.99"
        };
    }
}