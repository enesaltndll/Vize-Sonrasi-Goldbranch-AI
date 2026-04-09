using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class UserBadge
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Required]
        public string BadgeName { get; set; } = string.Empty; // Örn: İlk Kan, Deadline Avcısı

        [Required]
        public string IconUrl { get; set; } = string.Empty;

        public string? Description { get; set; }
        
        public DateTime EarnedAt { get; set; } = DateTime.Now;
    }
}
