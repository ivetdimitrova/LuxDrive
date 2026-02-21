using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LuxDrive.ViewModels.Friends
{
    public class ShareWithFriendViewModel
    {
        [Required]
        public Guid FileId { get; set; }

        [Required(ErrorMessage = "Please select a friend to share with.")]
        public Guid ReceiverId { get; set; }

        public IEnumerable<FriendViewModel> Friends { get; set; }
    }
}