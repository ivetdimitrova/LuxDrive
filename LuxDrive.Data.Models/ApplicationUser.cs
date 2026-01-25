using Microsoft.AspNetCore.Identity;

namespace LuxDrive.Data.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? ProfileImagePath { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public virtual ICollection<File> Files { get; set; }
         = new HashSet<File>();
    }
}
