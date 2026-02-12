using LuxDrive.ViewModels.Settings;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LuxDrive.ViewModels.Settings
{
    public class UserSettingsViewModel
    {
        public IFormFile? ProfileImage { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Username { get; set; }

        public bool RemovePhoto { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First name is required.")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]+$", ErrorMessage = "Only letters, spaces and dashes are allowed.")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Last name is required.")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]+$", ErrorMessage = "Only letters, spaces and dashes are allowed.")]
        public string LastName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\+?[0-9\s]+$", ErrorMessage = "Phone must contain only digits and +.")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmNewPassword { get; set; }

        public List<CardViewModel> SavedCards { get; set; } = new List<CardViewModel>();

        [Required(ErrorMessage = "Card name is required")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]+$", ErrorMessage = "Name must contain only letters")]
        public string NewCardName { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        [RegularExpression(@"^(\d{4}\s){3}\d{4}$|^(\d{16})$", ErrorMessage = "Invalid card format (16 digits)")]
        public string NewCardNumber { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Format must be MM/YY")]
        public string NewCardExpiry { get; set; }

        [Required(ErrorMessage = "CVC is required")]
        [RegularExpression(@"^[0-9]{3}$", ErrorMessage = "CVC must be 3 digits")]
        public string NewCardCvc { get; set; }
    }
}