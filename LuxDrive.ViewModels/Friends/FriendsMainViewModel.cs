using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.Friends
{
    public class FriendsMainViewModel
    {
        public IEnumerable<FriendViewModel>? Friends { get; set; } 
        public IEnumerable<UserSentRequestViewModel>? SentRequests { get; set; }
        public IEnumerable<ReceivedRequestViewModel>? ReceivedRequests { get; set; }
    }
}
