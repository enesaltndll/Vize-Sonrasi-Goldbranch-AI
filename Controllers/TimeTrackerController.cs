using GoldBranchAI.Data;
using GoldBranchAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoldBranchAI.Controllers
{
    [Authorize]
    public class TimeTrackerController : Controller
    {
        private readonly AppDbContext _context;

        public TimeTrackerController(AppDbContext context)
        {
            _context = context;
        }

        // Sayfa ilk yüklendiğinde o günkü toplam süreyi getirir (Sayaç sıfırdan başlamasın diye)
        [HttpGet]
        public IActionResult GetTodayTime()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return Json(new { minutes = 0 });

            var today = DateTime.Today; // Saat 00:00'ı temsil eder
            var log = _context.DailyTimeLogs.FirstOrDefault(l => l.AppUserId == user.Id && l.LogDate == today);

            return Json(new { minutes = log?.TotalMinutes ?? 0 });
        }

        // Her 60 saniyede bir JS tarafından sessizce tetiklenir (Veritabanına 1 dakika ekler)
        [HttpPost]
        public IActionResult PingTime()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return Json(new { success = false });

            var today = DateTime.Today;
            var log = _context.DailyTimeLogs.FirstOrDefault(l => l.AppUserId == user.Id && l.LogDate == today);

            if (log == null)
            {
                // O gün ilk defa giriş yapıyorsa yeni kayıt aç
                log = new DailyTimeLog { AppUserId = user.Id, LogDate = today, TotalMinutes = 1 };
                _context.DailyTimeLogs.Add(log);
            }
            else
            {
                // Kayıt varsa üstüne 1 dakika ekle
                log.TotalMinutes += 1;
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }
    }
}