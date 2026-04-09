using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class ChatGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string GroupName { get; set; } = string.Empty;

        public string? GroupAvatar { get; set; }

        public int CreatedByUserId { get; set; }
        public AppUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<ChatGroupMember> Members { get; set; } = new List<ChatGroupMember>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
