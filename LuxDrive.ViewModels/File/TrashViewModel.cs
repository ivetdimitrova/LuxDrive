using System;
using System.Collections.Generic;
using System.Linq;

namespace LuxDrive.ViewModels.File
{
    public class TrashViewModel
    {
        public IEnumerable<TrashItemViewModel> Files { get; set; } = new List<TrashItemViewModel>();
    }

    public class TrashItemViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public DateTime? DeletedOn { get; set; }

        public int DaysLeft
        {
            get
            {
                var days = DeletedOn.HasValue
                    ? 30 - (DateTime.UtcNow - DeletedOn.Value).Days
                    : 30;
                return Math.Max(0, days);
            }
        }

        public string DisplayExtension => Extension?.ToUpper() ?? "";
    }
}