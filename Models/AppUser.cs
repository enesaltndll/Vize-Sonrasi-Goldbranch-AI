using System.ComponentModel.DataAnnotations;

namespace GoldBranchAI.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        // YENİ: Sistem içi giriş için şifre alanı
        [Required]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Gelistirici";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // YENİ: Oyunlaştırma (Gamification) & Profil Bilgileri
        public int ExperiencePoints { get; set; } = 0;
        public int SnakeHighScore { get; set; } = 0;
        public string PreferredLanguage { get; set; } = "tr";
        public bool NightGuardianEnabled { get; set; } = true;

        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? TelegramChatId { get; set; }

        // AI Sağlayıcı Tercihleri
        public string PreferredAiProvider { get; set; } = "default"; // default, openai, gemini, anthropic, cohere, sambanova
        public string? CustomAiApiKey { get; set; }
        public string? CustomAiModel { get; set; }

        public ICollection<TodoTask> TodoTasks { get; set; } = new List<TodoTask>();
        public ICollection<ChatGroupMember> Memberships { get; set; } = new List<ChatGroupMember>();
        public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

    }
}