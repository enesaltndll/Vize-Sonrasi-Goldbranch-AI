using GoldBranchAI.Data;
using GoldBranchAI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoldBranchAI.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            int userId = int.Parse(userIdStr);

            // Fetch AppUser with Badges
            var user = _context.Users.Include(u => u.UserBadges).Include(t => t.TodoTasks).FirstOrDefault(u => u.Id == userId);
            
            if (user == null) return NotFound();

            // Calculate completed
            ViewBag.CompletedTasks = user.TodoTasks?.Count(t => t.IsCompleted) ?? 0;
            ViewBag.OverallTasks = user.TodoTasks?.Count ?? 0;
            
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(string avatarUrl)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false });

            int userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
            
            if(user != null && !string.IsNullOrWhiteSpace(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
