using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class DailyTimeLog
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Required]
        public DateTime LogDate { get; set; } // Hangi gün çalışıldı? (Örn: 31 Mart 2026)

        public int TotalMinutes { get; set; } = 0; // O günkü toplam çalışma süresi
    }
}