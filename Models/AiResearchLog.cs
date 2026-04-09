using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class AiResearchLog
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Required]
        public string RequestPrompt { get; set; } = string.Empty;

        [Required]
        public string ResponseMarkdown { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
