using GoldBranchAI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoldBranchAI.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetMyNotifications()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false });
            }

            var notifications = _context.SystemNotifications
                .Where(n => n.AppUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new {
                    id = n.Id,
                    message = n.Message,
                    link = n.Link,
                    time = n.CreatedAt.ToString("HH:mm")
                })
                .ToList();

            return Json(new { success = true, count = notifications.Count, data = notifications });
        }

        [HttpPost]
        public IActionResult MarkAsRead()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { success = false });

            var notifications = _context.SystemNotifications.Where(n => n.AppUserId == userId && !n.IsRead).ToList();
            foreach (var n in notifications) n.IsRead = true;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        // --- YENİ BİLDİRİM MERKEZİ EKRANI İÇİN ENDPOINT'LER (Faz 4) ---
        
        public IActionResult Index()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login", "Auth");

            var allNotifications = _context.SystemNotifications
                .Where(n => n.AppUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(allNotifications);
        }

        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                var unread = _context.SystemNotifications.Where(n => n.AppUserId == userId && !n.IsRead).ToList();
                foreach (var item in unread) item.IsRead = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteNotification(int id)
        {
            var notif = _context.SystemNotifications.Find(id);
            if (notif != null)
            {
                _context.SystemNotifications.Remove(notif);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
