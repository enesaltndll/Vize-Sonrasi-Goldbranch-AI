using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class ChatGroupMember
    {
        [Key]
        public int Id { get; set; }

        public int ChatGroupId { get; set; }
        public ChatGroup? ChatGroup { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
}
