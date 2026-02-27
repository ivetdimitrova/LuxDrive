using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.Friends
{
    public class UserSentRequestViewModel
    {
        public Guid Id { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }
}
