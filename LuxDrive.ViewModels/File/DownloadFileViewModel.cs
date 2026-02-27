using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.ViewModels.File
{
    public class DownloadFileViewModel
    {
        public string Name { get; set; } = null!;
        public string Extension { get; set; } = null!;
        public string StorageUrl { get; set; } = null!;
    }
}
