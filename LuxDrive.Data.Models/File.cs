namespace LuxDrive.Data.Models
{
    public class File
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Extension { get; set; } = null!;
        public long Size { get; set; }
        public string StorageUrl { get; set;} = null!;
        public DateTime UploadAt { get; set; }

        public Guid UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
