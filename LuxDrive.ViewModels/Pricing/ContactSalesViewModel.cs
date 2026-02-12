using System.ComponentModel.DataAnnotations;

namespace LuxDrive.ViewModels.Pricing
{
    public class ContactSalesViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]+$", ErrorMessage = "Name can only contain letters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }

        [Required(ErrorMessage = "Please enter your message.")]
        [StringLength(2000, MinimumLength = 10)]
        public string Message { get; set; }
    }
}