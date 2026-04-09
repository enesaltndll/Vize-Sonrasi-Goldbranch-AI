using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class TodoTask
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        // YENİ EKLENEN ONAY STATÜSÜ: (Devam Ediyor, Onay Bekliyor, Revize, Tamamlandı)
        public string Status { get; set; } = "Devam Ediyor";

        public int EstimatedTimeHours { get; set; }
        public int SpentTimeMinutes { get; set; } = 0;
        public string? AttachedFilePath { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("DependsOnTask")]
        public int? DependsOnTaskId { get; set; }
        public TodoTask? DependsOnTask { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<TaskComment>? Comments { get; set; }

        // --- AURA LOGIC & SMART SORTING ---
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string AuraColor 
        {
            get 
            {
                if (IsCompleted) return "secondary";
                var hoursLeft = (DueDate - DateTime.Now).TotalHours;
                if (hoursLeft < 24) return "danger"; // Acil (Kırmızı)
                if (hoursLeft < 168) return "warning"; // Yaklaşan (Turuncu - 7 gün)
                return "success"; // Rahat (Yeşil)
            }
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public double UrgencyScore
        {
            get 
            {
                if (IsCompleted) return 999999;
                return (DueDate - DateTime.Now).TotalHours;
            }
        }
    }
}