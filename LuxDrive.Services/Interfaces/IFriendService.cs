using LuxDrive.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.Services.Interfaces
{
    public interface IFriendService
    {
        Task SendRequestAsync(Guid senderId, Guid receiverId);
        Task AcceptRequestAsync(int requestId);
        Task RejectRequestAsync(int requestId);
        Task<IEnumerable<object>> GetPendingRequestsAsync(Guid userId);
        Task<ApplicationUser?> FindUserByEmailAsync(string email);
        Task<IEnumerable<object>> GetFriendsAsync(Guid userId);
        Task RemoveFriendAsync(Guid userId, Guid friendId);
        Task<IEnumerable<object>> GetSentPendingRequestsAsync(Guid userId);
    }
}
