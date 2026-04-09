using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int SenderId { get; set; }
        public AppUser? Sender { get; set; }

        public int? ReceiverId { get; set; }
        public AppUser? Receiver { get; set; }

        public int? ChatGroupId { get; set; }
        public ChatGroup? ChatGroup { get; set; }

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}