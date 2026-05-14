using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoldBranchAI.Models
{
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        public int FriendId { get; set; }
        [ForeignKey("FriendId")]
        public AppUser? Friend { get; set; }

        public DateTime EstablishedAt { get; set; } = DateTime.Now;
        public bool IsPending { get; set; } = false;
    }
}
