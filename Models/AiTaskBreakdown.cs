using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class AiTaskBreakdown
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ProjectDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gemini API'den dönen ham JSON yanıtı
        /// </summary>
        public string GeneratedJson { get; set; } = string.Empty;

        /// <summary>
        /// Bu breakdown'dan kaç alt görev üretildi
        /// </summary>
        public int SubTaskCount { get; set; }

        /// <summary>
        /// Alt görevler TodoTask tablosuna aktarıldı mı?
        /// </summary>
        public bool IsApplied { get; set; } = false;

        public int CreatedByUserId { get; set; }
        public AppUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
