using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.Friends
{
    public class RequestViewModel
    {
        public Guid Id { get; set; }

        public string SenderName { get; set; } = string.Empty;
    }
}
