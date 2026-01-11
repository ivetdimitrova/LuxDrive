using LuxDrive.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace LuxDrive.Models
{
    public class UserSettingsViewModel
    {
        public string Username { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First name is required.")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]+$", ErrorMessage = "Only letters, spaces and dashes are allowed.")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Last name is required.")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]+$", ErrorMessage = "Only letters, spaces and dashes are allowed.")]
        public string LastName { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\+?[0-9\s]+$", ErrorMessage = "Phone must contain only digits and +.")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }

        public List<PaymentCard> SavedCards { get; set; } = new List<PaymentCard>();

        public string NewCardNumber { get; set; }
        public string NewCardExpiry { get; set; }
        public string NewCardCvc { get; set; }
    }
}