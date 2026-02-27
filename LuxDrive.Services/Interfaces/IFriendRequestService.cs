using LuxDrive.ViewModels.Friends;

namespace LuxDrive.Services.Interfaces
{
    public interface IFriendRequestService
    {
        Task<IEnumerable<UserSentRequestViewModel>> GetSentRequestsAsync(string userId);
        Task<IEnumerable<ReceivedRequestViewModel>> GetReceivedRequestsAsync(string userId);
        Task SendRequestAsync(string senderId, string receiverEmail);

        Task AcceptRequestAsync(Guid requestId);

        Task RejectRequestAsync(Guid requestId);
    }
}
