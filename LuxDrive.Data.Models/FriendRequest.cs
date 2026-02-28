using LuxDrive.Data.Models.Enums;

namespace LuxDrive.Data.Models
    {
        public class FriendRequest
        {
            public Guid Id { get; set; }

            public Guid SenderId { get; set; }
            public ApplicationUser Sender { get; set; } = null!;

            public Guid ReceiverId { get; set; }
            public ApplicationUser Receiver { get; set; } = null!;

            public FriendRequestStatus Status { get; set; }

            public DateTime CreatedOn { get; set; }
        }
    }