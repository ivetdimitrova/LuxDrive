using LuxDrive.Data.Models;
using LuxDrive.ViewModels.Friends;


namespace LuxDrive.Services.Interfaces
{
    public interface IFriendService
    {
        Task SendRequestAsync(Guid senderId, Guid receiverId);
        Task AcceptRequestAsync(Guid requestId);
        Task RejectRequestAsync(Guid requestId);
        Task<IEnumerable<RequestViewModel>> GetPendingRequestsAsync(Guid userId);
        Task<ApplicationUser?> FindUserByEmailAsync(string email);
        Task<IEnumerable<FriendViewModel>> GetFriendsAsync(Guid userId);
        Task RemoveFriendAsync(Guid userId, Guid friendId);
        Task<IEnumerable<object>> GetSentPendingRequestsAsync(Guid userId);
    }
}
