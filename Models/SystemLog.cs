using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty; // Örn: "GÜVENLİK", "GÖREV", "SİSTEM"
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}