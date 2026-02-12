using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.Friends
{
    public class ShareWithFriendViewModel
    {
        public Guid FileId { get; set; }
        public Guid ReceiverId { get; set; }
        public IEnumerable<FriendViewModel> Friends { get; set; }

    }
}
