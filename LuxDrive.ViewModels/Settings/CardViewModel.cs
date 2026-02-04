using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.Settings
{
    public class CardViewModel
    {
        public Guid Id { get; set; }
        public string CardNumber { get; set; }
        public string CardLast4 { get; set; }
        public string CardType { get; set; }
    }
}
