using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.Friends
{
    public class UserSentRequestVIewModel
    {
        public Guid Id { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
    }
}
