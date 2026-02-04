using System.ComponentModel.DataAnnotations;

namespace LuxDrive.ViewModels.Pricing
{
    public class ContactSalesViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Company { get; set; }

        [Required]
        public string Message { get; set; }
    }
}