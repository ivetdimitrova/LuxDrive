using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.File
{
    public class IndexViewModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string StorageUrl { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public int Size { get; set; }

        public string? SenderName { get; set; } 
        public DateTime UploadedAt { get; set; }
    }
}
