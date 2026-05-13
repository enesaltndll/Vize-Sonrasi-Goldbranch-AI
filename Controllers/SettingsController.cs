using GoldBranchAI.Data;
using GoldBranchAI.Models;
using GoldBranchAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoldBranchAI.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BillingService _billingService;
        private readonly EmailService _emailService;

        public SettingsController(AppDbContext context, BillingService billingService, EmailService emailService)
        {
            _context = context;
            _billingService = billingService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users
                .Include(u => u.TodoTasks)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return NotFound();

            var userState = _billingService.GetUserState(user.Id, user.Email);
            ViewBag.Plan = _billingService.GetEffectivePlan(userState);
            ViewBag.CanUseAnimatedAvatar = userState.PlanKey == "pro" || userState.PlanKey == "business";

            // Heatmap Data (Last 120 days)
            var startDate = DateTime.Today.AddDays(-119);
            var activityData = user.TodoTasks
                .Where(t => t.IsCompleted && t.CompletedAt >= startDate)
                .GroupBy(t => t.CompletedAt.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionary(k => k.Date, v => v.Count);
            
            ViewBag.ActivityData = activityData;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> GetAIZReport()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users
                .Include(u => u.TodoTasks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Json(new { success = false, error = "User not found" });

            var todayCompleted = user.TodoTasks
                .Where(t => t.IsCompleted && t.CompletedAt.HasValue && t.CompletedAt.Value.Date == DateTime.Today)
                .ToList();

            if (!todayCompleted.Any())
            {
                return Json(new { success = true, report = "Bugün henüz tamamlanmış bir görev görünmüyor. Biraz aksiyon almaya ne dersin?" });
            }

            // AI Simulation (In a real app, this would call Gemini/OpenAI)
            var taskList = string.Join(", ", todayCompleted.Select(t => t.Title));
            var report = $"Bugün harika bir iş çıkardın! Tamamlananlar: {taskList}. " +
                         $"Verimlilik katsayınız %{new Random().Next(85, 99)}. " +
                         $"Yarın için odaklanman gereken ana konu: Proje stabilizasyonu ve testler.";

            return Json(new { success = true, report });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string fullName, string bio, string avatarUrl, string telegramChatId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return NotFound();

            var userState = _billingService.GetUserState(user.Id, user.Email);
            bool canUseAnimated = userState.PlanKey == "pro" || userState.PlanKey == "business";

            // Animated Avatar Check (Discord Nitro style)
            if (!string.IsNullOrEmpty(avatarUrl) && avatarUrl.ToLower().EndsWith(".gif") && !canUseAnimated)
            {
                TempData["Error"] = "Hareketli profil resimleri (GIF) sadece Gold veya Diamond üyeler içindir!";
                return RedirectToAction("Index");
            }

            user.FullName = fullName;
            user.Bio = bio;
            user.AvatarUrl = avatarUrl;
            user.TelegramChatId = telegramChatId;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profiliniz başarıyla güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestTelegram()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null || string.IsNullOrEmpty(user.TelegramChatId))
            {
                return Json(new { success = false, message = "Telegram Chat ID bulunamadı!" });
            }

            var telegramService = HttpContext.RequestServices.GetRequiredService<TelegramService>();
            var message = $"🚀 <b>GoldBranch AI Bağlantısı Başarılı!</b>\n\nMerhaba {user.FullName}, bu bir test bildirimidir. Artık tüm sistem uyarılarını buradan alabilirsin.";
            
            var result = await telegramService.SendMessageAsync(user.TelegramChatId, message);

            if (result)
                return Json(new { success = true, message = "Test mesajı başarıyla gönderildi!" });
            else
                return Json(new { success = false, message = "Hata! Bot Token geçersiz olabilir veya botu henüz başlatmamış olabilirsiniz." });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return NotFound();

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Yeni şifreler eşleşmiyor!";
                return RedirectToAction("Index");
            }

            // Verify current password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.Password);
            if (!isPasswordValid)
            {
                TempData["Error"] = "Mevcut şifreniz hatalı!";
                return RedirectToAction("Index");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Notify user via Email (Security best practice)
            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #fbbf24; text-align: center;'>Güvenlik Uyarısı: Şifre Değişikliği 🔐</h2>
                    <p style='color: #555; font-size: 16px;'>Merhaba <strong>{user.FullName}</strong>,</p>
                    <p style='color: #555; font-size: 16px;'>Hesabına ait şifre başarıyla değiştirildi. Bu işlemi sen yapmadıysan lütfen hemen destek ekibimizle iletişime geç.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='text-align: center; color: #999; font-size: 12px;'>Bu mesaj güvenlik amacıyla otomatik olarak gönderilmiştir.</p>
                </div>";

            _ = _emailService.SendEmailAsync(user.Email, "GoldBranch AI - Şifreniz Değiştirildi", emailBody);

            TempData["Success"] = "Şifreniz başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
    }
}
