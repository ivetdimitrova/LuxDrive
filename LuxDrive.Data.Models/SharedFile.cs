namespace LuxDrive.Data.Models
{
    public class SharedFile
    {
        public int Id { get; set; }

        public Guid FileId { get; set; }
        public File File { get; set; } = null!;

        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }

        public DateTime SharedOn { get; set; }
    }
}
