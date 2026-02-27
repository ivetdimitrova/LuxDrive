using LuxDrive.Data.Models;
using LuxDrive.ViewModels.Friends;


namespace LuxDrive.Services.Interfaces
{
    public interface IFriendService
    {
       
       
        Task<ApplicationUser?> FindUserByEmailAsync(string email);
        Task<IEnumerable<FriendViewModel>> GetFriendsAsync(Guid userId);
        Task RemoveFriendAsync(string userId, Guid friendId);
    }
}
