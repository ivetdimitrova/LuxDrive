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
        Task<IEnumerable<UserSentRequestVIewModel>> GetSentRequestAsync(Guid userId);
        Task<IEnumerable<ReceivedRequestViewModel>> GetReceivedRequestAsync(Guid userId);
        Task SendRequestAsync(Guid senderId, string receiverEmail);

        Task AcceptRequestAsync(Guid requestId);


    }
}
