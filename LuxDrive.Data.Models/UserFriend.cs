namespace LuxDrive.Data.Models
{
    public class UserFriend
    {
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public Guid FriendId { get; set; }
        public ApplicationUser Friend { get; set; } = null!;
    }
}
