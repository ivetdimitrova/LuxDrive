using LuxDrive.Data.Models;
using LuxDrive.ViewModels.Friends;


namespace LuxDrive.Services.Interfaces
{
    public interface IFriendService
    {
        Task AcceptRequestAsync(Guid requestId);
        Task RejectRequestAsync(Guid requestId);
        Task<ApplicationUser?> FindUserByEmailAsync(string email);
        Task<IEnumerable<FriendViewModel>> GetFriendsAsync(Guid userId);
        Task RemoveFriendAsync(Guid userId, Guid friendId);
    }
}
