using System;
using System.ComponentModel.DataAnnotations;

namespace LuxDrive.ViewModels.Friends
{
    public class FriendViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }
    }
}