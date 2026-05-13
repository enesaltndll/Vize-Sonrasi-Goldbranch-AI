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
        public DateTime? CompletedAt { get; set; }

        public ICollection<TaskComment>? Comments { get; set; }

        // --- ADVANCED AURA LOGIC & SMART SORTING (Hafta 4) ---
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public double UrgencyScore
        {
            get 
            {
                if (IsCompleted) return -1000; // Tamamlananlar en düşük öncelikte

                double hoursLeft = (DueDate - DateTime.Now).TotalHours;
                double remainingEffort = EstimatedTimeHours - (SpentTimeMinutes / 60.0);
                if (remainingEffort < 0) remainingEffort = 0; // Efor aşıldıysa

                // Eğer zaman tükendiyse (Gecikmiş görev), aciliyet sonsuza doğru katlanır
                if (hoursLeft <= 0) return 10000 + (remainingEffort * 100) + Math.Abs(hoursLeft * 10);

                // Risk Çarpanı (Velocity Ratio): Kalan iş, kalan zamandan fazlaysa risk katlanarak artar
                double riskRatio = remainingEffort / hoursLeft;
                
                // Bağımlılık Cezası (Dependency Penalty): Eğer bloke durumdaysa stres puanı artar
                double dependencyPenalty = (DependsOnTask != null && !DependsOnTask.IsCompleted) ? 1.5 : 1.0;

                // Enterprise Formül: (Risk * 1000) * Ceza + (Efor Yükü) + (Zaman Baskısı)
                double score = (riskRatio * 1000) * dependencyPenalty + (remainingEffort * 50) + (1000 / (hoursLeft + 1));
                
                return Math.Round(score, 2);
            }
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string AuraColor 
        {
            get 
            {
                if (IsCompleted) return "secondary";
                
                double score = UrgencyScore;

                if (score > 800) return "danger";    // Çok Acil / Yüksek Risk (Kırmızı)
                if (score > 300) return "warning";   // Yaklaşan Tehlike / Odak Gerektiriyor (Turuncu)
                if (score > 100) return "info";      // Aktif Çalışma (Mavi)
                return "success";                    // Güvende / Zaman Bol (Yeşil)
            }
        }
    }
}