using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class TaskFilterView
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Required]
        [MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        // Stored filter state (client uses this as canonical payload)
        [Required]
        [MaxLength(1200)]
        public string StateJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

