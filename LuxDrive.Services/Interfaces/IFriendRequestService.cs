using LuxDrive.ViewModels.Friends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.Services.Interfaces
{
    public interface IFriendRequestService
    {
        Task<IEnumerable<UserSentRequestViewModel>?> GetSentRequestAsync(string userId);
        Task<IEnumerable<ReceivedRequestViewModel>?> GetReceivedRequestAsync(string userId);
        Task SendRequestAsync(string senderId, string receiverEmail);

        Task AcceptRequestAsync(Guid requestId);

        Task RejectRequestAsync(Guid requestId);
    }
}
