using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class TaskComment
    {
        [Key]
        public int Id { get; set; }
        
        public int TodoTaskId { get; set; }
        public TodoTask? TodoTask { get; set; }
        
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
        
        [Required]
        public string CommentText { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
