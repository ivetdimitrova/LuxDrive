using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuxDrive.Data.Models
{
    public class SharedFile
    {
        public int Id { get; set; }
        public Guid FileId { get; set; }
        public virtual File File { get; set; } = null!;

        public Guid SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public virtual ApplicationUser Sender { get; set; } = null!;

        public Guid ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public virtual ApplicationUser Receiver { get; set; } = null!;

        public DateTime SharedOn { get; set; }
    }
}