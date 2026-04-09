using System;
using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class SystemNotification
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
